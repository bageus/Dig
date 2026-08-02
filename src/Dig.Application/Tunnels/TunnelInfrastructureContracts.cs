using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Application.Tunnels
{

public interface ITunnelInfrastructureRepository
{
    TunnelInfrastructureState Get();

    void Save(TunnelInfrastructureState state);
}

public sealed class RegisterTunnelSegmentCommand : ICommand<Result>
{
    public RegisterTunnelSegmentCommand(
        EntityId segmentId,
        TunnelSegmentOriginKind originKind,
        CellId originCell,
        IEnumerable<CellId> orderedHorizontalCells,
        long tick)
    {
        if (orderedHorizontalCells is null)
        {
            throw new ArgumentNullException(nameof(orderedHorizontalCells));
        }

        SegmentId = segmentId;
        OriginKind = originKind;
        OriginCell = originCell;
        OrderedHorizontalCells = new ReadOnlyCollection<CellId>(
            orderedHorizontalCells.ToArray());
        Tick = tick;
    }

    public EntityId SegmentId { get; }
    public TunnelSegmentOriginKind OriginKind { get; }
    public CellId OriginCell { get; }
    public IReadOnlyList<CellId> OrderedHorizontalCells { get; }
    public long Tick { get; }
}

public sealed class RegisterCompletedTunnelAnchorCommand : ICommand<Result>
{
    public RegisterCompletedTunnelAnchorCommand(
        EntityId segmentId,
        CellId cell,
        TunnelStructuralAnchorKind kind,
        long tick)
    {
        if (kind != TunnelStructuralAnchorKind.WoodenSupport
            && kind != TunnelStructuralAnchorKind.Door)
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        SegmentId = segmentId;
        Cell = cell;
        Kind = kind;
        Tick = tick;
    }

    public EntityId SegmentId { get; }
    public CellId Cell { get; }
    public TunnelStructuralAnchorKind Kind { get; }
    public long Tick { get; }
}

public sealed class GetTunnelInfrastructureQuery : IQuery<TunnelInfrastructureSnapshot>
{
}

public enum TunnelAutomaticSupportSyncStatus
{
    NoTarget = 0,
    OutOfRange = 1,
    PendingSource = 2,
    Available = 3,
    Retained = 4,
}

public sealed class TunnelAutomaticSupportSyncResult
{
    public TunnelAutomaticSupportSyncResult(
        TunnelAutomaticSupportSyncStatus status,
        EntityId? jobId,
        CellId? targetCell)
    {
        Status = status;
        JobId = jobId;
        TargetCell = targetCell;
    }

    public TunnelAutomaticSupportSyncStatus Status { get; }
    public EntityId? JobId { get; }
    public CellId? TargetCell { get; }
}

public sealed class SynchronizeTunnelAutomaticSupportCommand
    : ICommand<Result<TunnelAutomaticSupportSyncResult>>
{
    public SynchronizeTunnelAutomaticSupportCommand(
        EntityId segmentId,
        EntityId newJobId,
        IEnumerable<CellId> completedBuildingCells,
        IEnumerable<CellId> revealedCells,
        IEnumerable<CellId> reachableCells,
        long tick)
    {
        if (completedBuildingCells is null
            || revealedCells is null
            || reachableCells is null)
        {
            throw new ArgumentNullException(nameof(completedBuildingCells));
        }

        SegmentId = segmentId;
        NewJobId = newJobId;
        CompletedBuildingCells = Copy(completedBuildingCells);
        RevealedCells = Copy(revealedCells);
        ReachableCells = Copy(reachableCells);
        Tick = tick;
    }

    public EntityId SegmentId { get; }
    public EntityId NewJobId { get; }
    public IReadOnlyList<CellId> CompletedBuildingCells { get; }
    public IReadOnlyList<CellId> RevealedCells { get; }
    public IReadOnlyList<CellId> ReachableCells { get; }
    public long Tick { get; }

    private static IReadOnlyList<CellId> Copy(IEnumerable<CellId> cells)
    {
        return new ReadOnlyCollection<CellId>(
            cells.Distinct().OrderBy(cell => cell).ToArray());
    }
}

public static class TunnelInfrastructureApplicationErrors
{
    public static readonly DomainError SegmentNotFound = new DomainError(
        "tunnel.infrastructure.application.segment_not_found",
        "Tunnel segment was not found while synchronizing automatic work.");

    public static readonly DomainError MultipleActiveAutomaticJobs = new DomainError(
        "tunnel.infrastructure.application.multiple_active_jobs",
        "A tunnel segment has more than one non-terminal automatic support job.");
}
}
