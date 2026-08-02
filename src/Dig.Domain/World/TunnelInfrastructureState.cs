using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

internal sealed class HorizontalTunnelSegmentState
{
    private readonly CellId[] _orderedCells;
    private readonly Dictionary<CellId, int> _cellIndices;
    private readonly Dictionary<CellId, HashSet<TunnelStructuralAnchorKind>> _anchorKinds;
    private int _currentAnchorIndex = -1;

    private HorizontalTunnelSegmentState(
        EntityId segmentId,
        TunnelSegmentOriginKind originKind,
        CellId originCell,
        CellId[] orderedCells)
    {
        SegmentId = segmentId;
        OriginKind = originKind;
        OriginCell = originCell;
        _orderedCells = orderedCells;
        _cellIndices = orderedCells
            .Select((cell, index) => new { cell, index })
            .ToDictionary(value => value.cell, value => value.index);
        _anchorKinds = new Dictionary<CellId, HashSet<TunnelStructuralAnchorKind>>();
    }

    public EntityId SegmentId { get; }

    public TunnelSegmentOriginKind OriginKind { get; }

    public CellId OriginCell { get; }

    public long Version { get; private set; }

    public static Result<HorizontalTunnelSegmentState> Create(
        EntityId segmentId,
        TunnelSegmentOriginKind originKind,
        CellId originCell,
        IEnumerable<CellId> orderedHorizontalCells)
    {
        if (orderedHorizontalCells is null)
        {
            throw new ArgumentNullException(nameof(orderedHorizontalCells));
        }

        CellId[] cells = orderedHorizontalCells.ToArray();
        if (cells.Length == 0)
        {
            return Result<HorizontalTunnelSegmentState>.Failure(
                TunnelInfrastructureErrors.EmptySegment);
        }

        int direction = Math.Sign(cells[0].X - originCell.X);
        if (direction == 0)
        {
            return Result<HorizontalTunnelSegmentState>.Failure(
                TunnelInfrastructureErrors.InvalidHorizontalSegment);
        }

        for (int index = 0; index < cells.Length; index++)
        {
            CellId expected = new CellId(
                checked(originCell.X + (direction * (index + 1))),
                originCell.Y,
                originCell.Z);
            if (cells[index] != expected)
            {
                return Result<HorizontalTunnelSegmentState>.Failure(
                    TunnelInfrastructureErrors.InvalidHorizontalSegment);
            }
        }

        return Result<HorizontalTunnelSegmentState>.Success(
            new HorizontalTunnelSegmentState(segmentId, originKind, originCell, cells));
    }

    public Result<bool> RegisterAnchor(CellId cell, TunnelStructuralAnchorKind kind)
    {
        if (kind is not TunnelStructuralAnchorKind.WoodenSupport
            and not TunnelStructuralAnchorKind.Door)
        {
            return Result<bool>.Failure(TunnelInfrastructureErrors.InvalidAnchorKind);
        }

        if (!_cellIndices.TryGetValue(cell, out int cellIndex))
        {
            return Result<bool>.Failure(TunnelInfrastructureErrors.AnchorOutsideSegment);
        }

        if (_anchorKinds.TryGetValue(cell, out HashSet<TunnelStructuralAnchorKind>? kinds)
            && kinds.Contains(kind))
        {
            return Result<bool>.Success(false);
        }

        int nextTargetIndex = _currentAnchorIndex + TunnelInfrastructureState.AutomaticSupportInterval;
        if (cellIndex > _currentAnchorIndex
            && nextTargetIndex < _orderedCells.Length
            && cellIndex > nextTargetIndex)
        {
            return Result<bool>.Failure(TunnelInfrastructureErrors.AnchorBeyondNextTarget);
        }

        if (kinds is null)
        {
            kinds = new HashSet<TunnelStructuralAnchorKind>();
            _anchorKinds.Add(cell, kinds);
        }

        kinds.Add(kind);
        if (cellIndex > _currentAnchorIndex)
        {
            _currentAnchorIndex = cellIndex;
        }

        Version = checked(Version + 1);
        return Result<bool>.Success(true);
    }

    public CellId? GetNextTargetCell()
    {
        int targetIndex = _currentAnchorIndex + TunnelInfrastructureState.AutomaticSupportInterval;
        return targetIndex < _orderedCells.Length ? _orderedCells[targetIndex] : null;
    }

    public HorizontalTunnelSegmentSnapshot CaptureSnapshot()
    {
        List<TunnelStructuralAnchorSnapshot> anchors = new List<TunnelStructuralAnchorSnapshot>
        {
            new TunnelStructuralAnchorSnapshot(OriginCell, TunnelStructuralAnchorKind.Origin, 0),
        };
        foreach (KeyValuePair<CellId, HashSet<TunnelStructuralAnchorKind>> entry in
            _anchorKinds.OrderBy(value => _cellIndices[value.Key]))
        {
            int distance = _cellIndices[entry.Key] + 1;
            foreach (TunnelStructuralAnchorKind kind in entry.Value.OrderBy(value => value))
            {
                anchors.Add(new TunnelStructuralAnchorSnapshot(entry.Key, kind, distance));
            }
        }

        CellId anchorCell = _currentAnchorIndex < 0
            ? OriginCell
            : _orderedCells[_currentAnchorIndex];
        CellId? targetCell = GetNextTargetCell();
        TunnelAutomaticSupportTargetSnapshot? target = targetCell.HasValue
            ? new TunnelAutomaticSupportTargetSnapshot(
                SegmentId,
                anchorCell,
                targetCell.Value,
                TunnelInfrastructureState.AutomaticSupportInterval)
            : null;

        return new HorizontalTunnelSegmentSnapshot(
            SegmentId,
            OriginKind,
            OriginCell,
            new ReadOnlyCollection<CellId>(_orderedCells),
            anchors,
            target,
            Version);
    }

    public static Result<HorizontalTunnelSegmentState> Restore(
        HorizontalTunnelSegmentSnapshot snapshot)
    {
        Result<HorizontalTunnelSegmentState> created = Create(
            snapshot.SegmentId,
            snapshot.OriginKind,
            snapshot.OriginCell,
            snapshot.OrderedHorizontalCells);
        if (created.IsFailure)
        {
            return created;
        }

        HorizontalTunnelSegmentState state = created.Value;
        TunnelStructuralAnchorSnapshot[] anchors = snapshot.StructuralAnchors.ToArray();
        if (anchors.Length == 0
            || anchors[0].Cell != snapshot.OriginCell
            || anchors[0].Kind != TunnelStructuralAnchorKind.Origin
            || anchors[0].DistanceFromOrigin != 0)
        {
            return Result<HorizontalTunnelSegmentState>.Failure(
                TunnelInfrastructureErrors.InvalidSnapshot);
        }

        foreach (TunnelStructuralAnchorSnapshot anchor in anchors
            .Skip(1)
            .OrderBy(value => value.DistanceFromOrigin)
            .ThenBy(value => value.Kind))
        {
            Result<bool> registered = state.RegisterAnchor(anchor.Cell, anchor.Kind);
            if (registered.IsFailure)
            {
                return Result<HorizontalTunnelSegmentState>.Failure(registered.Error!);
            }
        }

        HorizontalTunnelSegmentSnapshot derived = state.CaptureSnapshot();
        if (derived.NextAutomaticSupportTarget != snapshot.NextAutomaticSupportTarget)
        {
            return Result<HorizontalTunnelSegmentState>.Failure(
                TunnelInfrastructureErrors.InvalidSnapshot);
        }

        state.Version = snapshot.Version;
        return Result<HorizontalTunnelSegmentState>.Success(state);
    }
}
}
