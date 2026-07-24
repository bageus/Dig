using System;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Application.World
{

public static class MiningOutputSaveErrors
{
    public static readonly DomainError InvalidSnapshot = new DomainError(
        "mining_output.save.invalid_snapshot",
        "The mining output commit snapshot is malformed or unsupported.");

    public static readonly DomainError IntegrityMismatch = new DomainError(
        "mining_output.save.integrity_mismatch",
        "The mining output commit ledger does not match authoritative Inventory state.");
}

public sealed class RestoredMiningOutputState
{
    internal RestoredMiningOutputState(
        MiningOutputCommitState commits,
        MiningOutputIntegrityReport integrity)
    {
        Commits = commits ?? throw new ArgumentNullException(nameof(commits));
        Integrity = integrity ?? throw new ArgumentNullException(nameof(integrity));
    }

    public MiningOutputCommitState Commits { get; }

    public MiningOutputIntegrityReport Integrity { get; }
}

public sealed class MiningOutputSaveCoordinator
{
    private readonly MiningOutputIntegrityDiagnostics _diagnostics;

    public MiningOutputSaveCoordinator(MiningOutputIntegrityDiagnostics? diagnostics = null)
    {
        _diagnostics = diagnostics ?? new MiningOutputIntegrityDiagnostics();
    }

    public Result<MiningOutputCommitSaveSnapshot> Capture(
        MiningOutputCommitState commits,
        InventoryState inventory)
    {
        if (commits == null)
        {
            throw new ArgumentNullException(nameof(commits));
        }

        if (inventory == null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        MiningOutputIntegrityReport integrity = _diagnostics.Inspect(commits, inventory);
        if (!integrity.IsValid)
        {
            return Result<MiningOutputCommitSaveSnapshot>.Failure(
                MiningOutputSaveErrors.IntegrityMismatch);
        }

        return Result<MiningOutputCommitSaveSnapshot>.Success(
            MiningOutputCommitSaveSnapshot.Capture(commits));
    }

    public Result<RestoredMiningOutputState> Restore(
        MiningOutputCommitSaveSnapshot snapshot,
        InventoryState inventory)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (inventory == null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        try
        {
            MiningOutputCommitState restored = snapshot.Restore();
            MiningOutputIntegrityReport integrity = _diagnostics.Inspect(restored, inventory);
            if (!integrity.IsValid)
            {
                return Result<RestoredMiningOutputState>.Failure(
                    MiningOutputSaveErrors.IntegrityMismatch);
            }

            return Result<RestoredMiningOutputState>.Success(
                new RestoredMiningOutputState(restored, integrity));
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is FormatException
            || exception is OverflowException)
        {
            return Result<RestoredMiningOutputState>.Failure(
                MiningOutputSaveErrors.InvalidSnapshot);
        }
    }
}

}
