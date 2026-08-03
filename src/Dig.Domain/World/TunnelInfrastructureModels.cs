using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.World
{

public enum TunnelSegmentOriginKind
{
    RoomExit = 0,
    VerticalJunction = 1,
}

public enum TunnelStructuralAnchorKind
{
    Origin = 0,
    WoodenSupport = 1,
    Door = 2,
}

public readonly struct TunnelStructuralAnchorSnapshot : IEquatable<TunnelStructuralAnchorSnapshot>
{
    public TunnelStructuralAnchorSnapshot(
        CellId cell,
        TunnelStructuralAnchorKind kind,
        int distanceFromOrigin)
    {
        if (distanceFromOrigin < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distanceFromOrigin));
        }

        Cell = cell;
        Kind = kind;
        DistanceFromOrigin = distanceFromOrigin;
    }

    public CellId Cell { get; }

    public TunnelStructuralAnchorKind Kind { get; }

    public int DistanceFromOrigin { get; }

    public bool Equals(TunnelStructuralAnchorSnapshot other)
    {
        return Cell == other.Cell
            && Kind == other.Kind
            && DistanceFromOrigin == other.DistanceFromOrigin;
    }

    public override bool Equals(object? obj)
    {
        return obj is TunnelStructuralAnchorSnapshot other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Cell, Kind, DistanceFromOrigin);
    }
}

public readonly struct TunnelAutomaticSupportTargetSnapshot
    : IEquatable<TunnelAutomaticSupportTargetSnapshot>
{
    public TunnelAutomaticSupportTargetSnapshot(
        EntityId segmentId,
        CellId anchorCell,
        CellId targetCell,
        int distanceFromAnchor)
    {
        if (segmentId.IsEmpty)
        {
            throw new ArgumentException("Segment id cannot be empty.", nameof(segmentId));
        }

        if (distanceFromAnchor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distanceFromAnchor));
        }

        SegmentId = segmentId;
        AnchorCell = anchorCell;
        TargetCell = targetCell;
        DistanceFromAnchor = distanceFromAnchor;
    }

    public EntityId SegmentId { get; }

    public CellId AnchorCell { get; }

    public CellId TargetCell { get; }

    public int DistanceFromAnchor { get; }

    public bool Equals(TunnelAutomaticSupportTargetSnapshot other)
    {
        return SegmentId == other.SegmentId
            && AnchorCell == other.AnchorCell
            && TargetCell == other.TargetCell
            && DistanceFromAnchor == other.DistanceFromAnchor;
    }

    public override bool Equals(object? obj)
    {
        return obj is TunnelAutomaticSupportTargetSnapshot other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SegmentId, AnchorCell, TargetCell, DistanceFromAnchor);
    }
}

public sealed class HorizontalTunnelSegmentSnapshot
{
    private readonly CellId[] _orderedHorizontalCells;
    private readonly TunnelStructuralAnchorSnapshot[] _structuralAnchors;

    public HorizontalTunnelSegmentSnapshot(
        EntityId segmentId,
        TunnelSegmentOriginKind originKind,
        CellId originCell,
        IEnumerable<CellId> orderedHorizontalCells,
        IEnumerable<TunnelStructuralAnchorSnapshot> structuralAnchors,
        TunnelAutomaticSupportTargetSnapshot? nextAutomaticSupportTarget,
        long version)
    {
        if (segmentId.IsEmpty)
        {
            throw new ArgumentException("Segment id cannot be empty.", nameof(segmentId));
        }

        if (orderedHorizontalCells is null)
        {
            throw new ArgumentNullException(nameof(orderedHorizontalCells));
        }

        if (structuralAnchors is null)
        {
            throw new ArgumentNullException(nameof(structuralAnchors));
        }

        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        SegmentId = segmentId;
        OriginKind = originKind;
        OriginCell = originCell;
        _orderedHorizontalCells = orderedHorizontalCells.ToArray();
        _structuralAnchors = structuralAnchors.ToArray();
        NextAutomaticSupportTarget = nextAutomaticSupportTarget;
        Version = version;
    }

    public EntityId SegmentId { get; }

    public TunnelSegmentOriginKind OriginKind { get; }

    public CellId OriginCell { get; }

    public IReadOnlyList<CellId> OrderedHorizontalCells =>
        new ReadOnlyCollection<CellId>(_orderedHorizontalCells);

    public IReadOnlyList<TunnelStructuralAnchorSnapshot> StructuralAnchors =>
        new ReadOnlyCollection<TunnelStructuralAnchorSnapshot>(_structuralAnchors);

    public TunnelAutomaticSupportTargetSnapshot? NextAutomaticSupportTarget { get; }

    public long Version { get; }
}

public sealed class TunnelInfrastructureSnapshot
{
    private readonly HorizontalTunnelSegmentSnapshot[] _segments;
    private readonly CellId[] _completedJunctionStoneTrimCells;
    private readonly CellId[] _completedStoneFloorTrimCells;
    private readonly TunnelJunctionStoneTrimTargetSnapshot[] _pendingJunctionStoneTrimTargets;

    public TunnelInfrastructureSnapshot(
        long version,
        IEnumerable<HorizontalTunnelSegmentSnapshot> segments,
        IEnumerable<CellId>? completedJunctionStoneTrimCells = null,
        IEnumerable<CellId>? completedStoneFloorTrimCells = null)
    {
        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (segments is null)
        {
            throw new ArgumentNullException(nameof(segments));
        }

        Version = version;
        _segments = segments
            .OrderBy(value => value.SegmentId.ToString(), StringComparer.Ordinal)
            .ToArray();
        _completedJunctionStoneTrimCells = (completedJunctionStoneTrimCells
                ?? Array.Empty<CellId>())
            .Distinct()
            .OrderBy(cell => cell)
            .ToArray();
        _completedStoneFloorTrimCells = (completedStoneFloorTrimCells
                ?? Array.Empty<CellId>())
            .Distinct()
            .OrderBy(cell => cell)
            .ToArray();
        _pendingJunctionStoneTrimTargets =
            TunnelJunctionStoneTrimProjection.DerivePending(
                _segments,
                _completedJunctionStoneTrimCells);
    }

    public long Version { get; }

    public IReadOnlyList<HorizontalTunnelSegmentSnapshot> Segments =>
        new ReadOnlyCollection<HorizontalTunnelSegmentSnapshot>(_segments);

    public IReadOnlyList<CellId> CompletedJunctionStoneTrimCells =>
        new ReadOnlyCollection<CellId>(_completedJunctionStoneTrimCells);

    public IReadOnlyList<CellId> CompletedStoneFloorTrimCells =>
        new ReadOnlyCollection<CellId>(_completedStoneFloorTrimCells);

    public IReadOnlyList<TunnelJunctionStoneTrimTargetSnapshot>
        PendingJunctionStoneTrimTargets =>
            new ReadOnlyCollection<TunnelJunctionStoneTrimTargetSnapshot>(
                _pendingJunctionStoneTrimTargets);
}

public sealed class TunnelSegmentRegistered : IDomainEvent
{
    public TunnelSegmentRegistered(long tick, EntityId segmentId)
    {
        Tick = tick;
        SegmentId = segmentId;
    }

    public long Tick { get; }

    public EntityId SegmentId { get; }
}

public sealed class TunnelStructuralAnchorRegistered : IDomainEvent
{
    public TunnelStructuralAnchorRegistered(
        long tick,
        EntityId segmentId,
        CellId cell,
        TunnelStructuralAnchorKind kind)
    {
        Tick = tick;
        SegmentId = segmentId;
        Cell = cell;
        Kind = kind;
    }

    public long Tick { get; }

    public EntityId SegmentId { get; }

    public CellId Cell { get; }

    public TunnelStructuralAnchorKind Kind { get; }
}

public sealed class TunnelAutomaticSupportTargetChanged : IDomainEvent
{
    public TunnelAutomaticSupportTargetChanged(
        long tick,
        EntityId segmentId,
        CellId? previousTargetCell,
        CellId? nextTargetCell)
    {
        Tick = tick;
        SegmentId = segmentId;
        PreviousTargetCell = previousTargetCell;
        NextTargetCell = nextTargetCell;
    }

    public long Tick { get; }

    public EntityId SegmentId { get; }

    public CellId? PreviousTargetCell { get; }

    public CellId? NextTargetCell { get; }
}
}
