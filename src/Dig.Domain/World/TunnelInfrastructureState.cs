using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.World
{

public static class TunnelInfrastructureErrors
{
    public static readonly DomainError EmptySegmentId = new DomainError(
        "tunnel.infrastructure.empty_segment_id",
        "Tunnel segment id cannot be empty.");

    public static readonly DomainError SegmentAlreadyExists = new DomainError(
        "tunnel.infrastructure.segment_already_exists",
        "Tunnel segment is already registered.");

    public static readonly DomainError SegmentNotFound = new DomainError(
        "tunnel.infrastructure.segment_not_found",
        "Tunnel segment was not found.");

    public static readonly DomainError EmptySegment = new DomainError(
        "tunnel.infrastructure.empty_segment",
        "Tunnel segment must contain at least one horizontal cell.");

    public static readonly DomainError InvalidHorizontalSegment = new DomainError(
        "tunnel.infrastructure.invalid_horizontal_segment",
        "Tunnel segment cells must form one contiguous horizontal line from the origin.");

    public static readonly DomainError InvalidAnchorKind = new DomainError(
        "tunnel.infrastructure.invalid_anchor_kind",
        "Only a completed wooden support or completed door can be registered as an anchor.");

    public static readonly DomainError AnchorOutsideSegment = new DomainError(
        "tunnel.infrastructure.anchor_outside_segment",
        "Structural anchor cell is outside the tunnel segment.");

    public static readonly DomainError JunctionNotFound = new DomainError(
        "tunnel.infrastructure.junction_not_found",
        "A vertical tunnel junction was not found at the requested cell.");

    public static readonly DomainError InvalidSnapshot = new DomainError(
        "tunnel.infrastructure.invalid_snapshot",
        "Tunnel infrastructure snapshot does not match its derived anchor chain.");
}

public sealed class TunnelInfrastructureState : AggregateRoot
{
    public const int AutomaticSupportInterval = 10;

    private readonly Dictionary<EntityId, HorizontalTunnelSegmentState> _segments =
        new Dictionary<EntityId, HorizontalTunnelSegmentState>();
    private readonly HashSet<CellId> _completedJunctionStoneTrimCells =
        new HashSet<CellId>();

    public long Version { get; private set; }

    public Result RegisterSegment(
        EntityId segmentId,
        TunnelSegmentOriginKind originKind,
        CellId originCell,
        IEnumerable<CellId> orderedHorizontalCells,
        long tick)
    {
        ValidateTick(tick);
        if (segmentId.IsEmpty)
        {
            return Result.Failure(TunnelInfrastructureErrors.EmptySegmentId);
        }

        if (_segments.ContainsKey(segmentId))
        {
            return Result.Failure(TunnelInfrastructureErrors.SegmentAlreadyExists);
        }

        Result<HorizontalTunnelSegmentState> created = HorizontalTunnelSegmentState.Create(
            segmentId,
            originKind,
            originCell,
            orderedHorizontalCells);
        if (created.IsFailure)
        {
            return Result.Failure(created.Error!);
        }

        TunnelJunctionStoneTrimTargetSnapshot[] previousJunctionTargets =
            CapturePendingJunctionTargets();
        HorizontalTunnelSegmentState segment = created.Value;
        _segments.Add(segmentId, segment);
        Version = checked(Version + 1);
        Raise(new TunnelSegmentRegistered(tick, segmentId));

        CellId? initialTarget = segment.GetNextTargetCell();
        if (initialTarget.HasValue)
        {
            Raise(new TunnelAutomaticSupportTargetChanged(
                tick,
                segmentId,
                previousTargetCell: null,
                nextTargetCell: initialTarget));
        }

        RaiseJunctionTargetChanges(
            previousJunctionTargets,
            CapturePendingJunctionTargets(),
            tick);
        return Result.Success();
    }

    public Result RemoveSegment(EntityId segmentId, long tick)
    {
        ValidateTick(tick);
        if (!_segments.TryGetValue(segmentId, out HorizontalTunnelSegmentState? segment))
        {
            return Result.Failure(TunnelInfrastructureErrors.SegmentNotFound);
        }

        TunnelJunctionStoneTrimTargetSnapshot[] previousJunctionTargets =
            CapturePendingJunctionTargets();
        CellId originCell = segment.OriginCell;
        TunnelSegmentOriginKind originKind = segment.OriginKind;
        _segments.Remove(segmentId);
        Version = checked(Version + 1);
        Raise(new TunnelSegmentRemoved(tick, segmentId));

        if (originKind == TunnelSegmentOriginKind.VerticalJunction
            && !HasVerticalJunction(originCell)
            && _completedJunctionStoneTrimCells.Remove(originCell))
        {
            Version = checked(Version + 1);
            Raise(new TunnelJunctionStoneTrimCompletionRemoved(tick, originCell));
        }

        RaiseJunctionTargetChanges(
            previousJunctionTargets,
            CapturePendingJunctionTargets(),
            tick);
        return Result.Success();
    }

    public Result RegisterCompletedWoodenSupport(
        EntityId segmentId,
        CellId cell,
        long tick)
    {
        return RegisterAnchor(segmentId, cell, TunnelStructuralAnchorKind.WoodenSupport, tick);
    }

    public Result RegisterCompletedDoor(EntityId segmentId, CellId cell, long tick)
    {
        return RegisterAnchor(segmentId, cell, TunnelStructuralAnchorKind.Door, tick);
    }

    public Result RegisterCompletedJunctionStoneTrim(CellId cell, long tick)
    {
        ValidateTick(tick);
        TunnelJunctionStoneTrimTargetSnapshot[] targets =
            CapturePendingJunctionTargets();
        int targetIndex = Array.FindIndex(targets, target => target.Cell == cell);
        if (targetIndex < 0)
        {
            return _completedJunctionStoneTrimCells.Contains(cell)
                ? Result.Success()
                : Result.Failure(TunnelInfrastructureErrors.JunctionNotFound);
        }

        _completedJunctionStoneTrimCells.Add(cell);
        Version = checked(Version + 1);
        Raise(new TunnelJunctionStoneTrimCompleted(tick, cell));
        Raise(new TunnelJunctionStoneTrimTargetChanged(
            tick,
            cell,
            targets[targetIndex].OwnerSegmentId,
            nextOwnerSegmentId: null));
        return Result.Success();
    }

    public HorizontalTunnelSegmentSnapshot? GetSegment(EntityId segmentId)
    {
        return _segments.TryGetValue(segmentId, out HorizontalTunnelSegmentState? segment)
            ? segment.CaptureSnapshot()
            : null;
    }

    public TunnelInfrastructureSnapshot CaptureSnapshot()
    {
        return new TunnelInfrastructureSnapshot(
            Version,
            _segments.Values
                .OrderBy(value => value.SegmentId.ToString(), StringComparer.Ordinal)
                .Select(value => value.CaptureSnapshot()),
            _completedJunctionStoneTrimCells);
    }

    public static Result<TunnelInfrastructureState> Restore(
        TunnelInfrastructureSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        TunnelInfrastructureState state = new TunnelInfrastructureState();
        foreach (HorizontalTunnelSegmentSnapshot segmentSnapshot in snapshot.Segments)
        {
            if (state._segments.ContainsKey(segmentSnapshot.SegmentId))
            {
                return Result<TunnelInfrastructureState>.Failure(
                    TunnelInfrastructureErrors.InvalidSnapshot);
            }

            Result<HorizontalTunnelSegmentState> restored =
                HorizontalTunnelSegmentState.Restore(segmentSnapshot);
            if (restored.IsFailure)
            {
                return Result<TunnelInfrastructureState>.Failure(restored.Error!);
            }

            state._segments.Add(segmentSnapshot.SegmentId, restored.Value);
        }

        foreach (CellId cell in snapshot.CompletedJunctionStoneTrimCells)
        {
            if (!state.HasVerticalJunction(cell)
                || !state._completedJunctionStoneTrimCells.Add(cell))
            {
                return Result<TunnelInfrastructureState>.Failure(
                    TunnelInfrastructureErrors.InvalidSnapshot);
            }
        }

        TunnelInfrastructureSnapshot derived = state.CaptureSnapshot();
        if (!derived.PendingJunctionStoneTrimTargets.SequenceEqual(
                snapshot.PendingJunctionStoneTrimTargets))
        {
            return Result<TunnelInfrastructureState>.Failure(
                TunnelInfrastructureErrors.InvalidSnapshot);
        }

        state.Version = snapshot.Version;
        return Result<TunnelInfrastructureState>.Success(state);
    }

    private Result RegisterAnchor(
        EntityId segmentId,
        CellId cell,
        TunnelStructuralAnchorKind kind,
        long tick)
    {
        ValidateTick(tick);
        if (!_segments.TryGetValue(segmentId, out HorizontalTunnelSegmentState? segment))
        {
            return Result.Failure(TunnelInfrastructureErrors.SegmentNotFound);
        }

        CellId? previousTarget = segment.GetNextTargetCell();
        Result<bool> registration = segment.RegisterAnchor(cell, kind);
        if (registration.IsFailure)
        {
            return Result.Failure(registration.Error!);
        }

        if (!registration.Value)
        {
            return Result.Success();
        }

        Version = checked(Version + 1);
        Raise(new TunnelStructuralAnchorRegistered(tick, segmentId, cell, kind));

        CellId? nextTarget = segment.GetNextTargetCell();
        if (previousTarget != nextTarget)
        {
            Raise(new TunnelAutomaticSupportTargetChanged(
                tick,
                segmentId,
                previousTarget,
                nextTarget));
        }

        return Result.Success();
    }

    private TunnelJunctionStoneTrimTargetSnapshot[] CapturePendingJunctionTargets()
    {
        return TunnelJunctionStoneTrimProjection.DerivePending(
            _segments.Values.Select(segment => segment.CaptureSnapshot()),
            _completedJunctionStoneTrimCells);
    }

    private bool HasVerticalJunction(CellId cell)
    {
        return _segments.Values.Any(segment =>
            segment.OriginKind == TunnelSegmentOriginKind.VerticalJunction
            && segment.OriginCell == cell);
    }

    private void RaiseJunctionTargetChanges(
        IReadOnlyCollection<TunnelJunctionStoneTrimTargetSnapshot> previous,
        IReadOnlyCollection<TunnelJunctionStoneTrimTargetSnapshot> next,
        long tick)
    {
        Dictionary<CellId, EntityId> previousByCell =
            previous.ToDictionary(target => target.Cell, target => target.OwnerSegmentId);
        Dictionary<CellId, EntityId> nextByCell =
            next.ToDictionary(target => target.Cell, target => target.OwnerSegmentId);
        foreach (CellId cell in previousByCell.Keys
            .Concat(nextByCell.Keys)
            .Distinct()
            .OrderBy(value => value))
        {
            previousByCell.TryGetValue(cell, out EntityId previousOwner);
            nextByCell.TryGetValue(cell, out EntityId nextOwner);
            EntityId? previousValue = previousOwner.IsEmpty ? (EntityId?)null : previousOwner;
            EntityId? nextValue = nextOwner.IsEmpty ? (EntityId?)null : nextOwner;
            if (previousValue != nextValue)
            {
                Raise(new TunnelJunctionStoneTrimTargetChanged(
                    tick,
                    cell,
                    previousValue,
                    nextValue));
            }
        }
    }

    private static void ValidateTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }
}
}
