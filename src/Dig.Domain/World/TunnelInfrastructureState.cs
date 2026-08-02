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

    public static readonly DomainError AnchorBeyondNextTarget = new DomainError(
        "tunnel.infrastructure.anchor_beyond_next_target",
        "A new forward anchor cannot skip the current automatic support target.");

    public static readonly DomainError InvalidSnapshot = new DomainError(
        "tunnel.infrastructure.invalid_snapshot",
        "Tunnel infrastructure snapshot does not match its derived anchor chain.");
}

public sealed class TunnelInfrastructureState : AggregateRoot
{
    public const int AutomaticSupportInterval = 10;

    private readonly Dictionary<EntityId, HorizontalTunnelSegmentState> _segments =
        new Dictionary<EntityId, HorizontalTunnelSegmentState>();

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
                initialTarget));
        }

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
                .Select(value => value.CaptureSnapshot()));
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

    private static void ValidateTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }
}
}
