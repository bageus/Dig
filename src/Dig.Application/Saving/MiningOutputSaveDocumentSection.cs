using System;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public static class MiningOutputSaveDocumentSectionErrors
{
    public static readonly DomainError CellOutOfBounds = new DomainError(
        "mining_output.save.document.cell_out_of_bounds",
        "The mining output save section references a cell outside authoritative world bounds.");
}

public sealed class MiningOutputSaveDocumentSection
{
    private readonly MiningOutputSaveCoordinator _coordinator;
    private readonly MiningOutputSaveWorldValidator _worldValidator;

    public MiningOutputSaveDocumentSection(
        MiningOutputSaveCoordinator? coordinator = null,
        MiningOutputSaveWorldValidator? worldValidator = null)
    {
        _coordinator = coordinator ?? new MiningOutputSaveCoordinator();
        _worldValidator = worldValidator ?? new MiningOutputSaveWorldValidator();
    }

    public Result<MiningOutputCommitsSaveData> Capture(
        MiningOutputCommitState commits,
        InventoryState inventory,
        WorldSize worldSize)
    {
        if (commits == null)
        {
            throw new ArgumentNullException(nameof(commits));
        }

        if (inventory == null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        Result<MiningOutputCommitSaveSnapshot> captured =
            _coordinator.Capture(commits, inventory);
        if (captured.IsFailure)
        {
            return Result<MiningOutputCommitsSaveData>.Failure(captured.Error);
        }

        MiningOutputSaveWorldValidationReport worldValidation =
            _worldValidator.Validate(captured.Value, worldSize);
        if (!worldValidation.IsValid)
        {
            return Result<MiningOutputCommitsSaveData>.Failure(
                MiningOutputSaveDocumentSectionErrors.CellOutOfBounds);
        }

        return Result<MiningOutputCommitsSaveData>.Success(
            MiningOutputSaveDataAdapter.Encode(captured.Value));
    }

    public Result<RestoredMiningOutputState> Restore(
        MiningOutputCommitsSaveData data,
        InventoryState inventory,
        WorldSize worldSize)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (inventory == null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        try
        {
            MiningOutputCommitSaveSnapshot snapshot =
                MiningOutputSaveDataAdapter.Decode(data);
            MiningOutputSaveWorldValidationReport worldValidation =
                _worldValidator.Validate(snapshot, worldSize);
            if (!worldValidation.IsValid)
            {
                return Result<RestoredMiningOutputState>.Failure(
                    MiningOutputSaveDocumentSectionErrors.CellOutOfBounds);
            }

            return _coordinator.Restore(snapshot, inventory);
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
