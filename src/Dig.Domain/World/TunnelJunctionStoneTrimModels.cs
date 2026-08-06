using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.World
{

public readonly struct TunnelJunctionStoneTrimTargetSnapshot
    : IEquatable<TunnelJunctionStoneTrimTargetSnapshot>
{
    public TunnelJunctionStoneTrimTargetSnapshot(EntityId ownerSegmentId, CellId cell)
    {
        if (ownerSegmentId.IsEmpty)
        {
            throw new ArgumentException(
                "Junction stone-trim owner segment id cannot be empty.",
                nameof(ownerSegmentId));
        }

        OwnerSegmentId = ownerSegmentId;
        Cell = cell;
    }

    public EntityId OwnerSegmentId { get; }

    public CellId Cell { get; }

    public bool Equals(TunnelJunctionStoneTrimTargetSnapshot other)
    {
        return OwnerSegmentId == other.OwnerSegmentId && Cell == other.Cell;
    }

    public override bool Equals(object? obj)
    {
        return obj is TunnelJunctionStoneTrimTargetSnapshot other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(OwnerSegmentId, Cell);
    }
}

public sealed class TunnelSegmentRemoved : IDomainEvent
{
    public TunnelSegmentRemoved(long tick, EntityId segmentId)
    {
        Tick = tick;
        SegmentId = segmentId;
    }

    public long Tick { get; }

    public EntityId SegmentId { get; }
}

public sealed class TunnelJunctionStoneTrimTargetChanged : IDomainEvent
{
    public TunnelJunctionStoneTrimTargetChanged(
        long tick,
        CellId cell,
        EntityId? previousOwnerSegmentId,
        EntityId? nextOwnerSegmentId)
    {
        Tick = tick;
        Cell = cell;
        PreviousOwnerSegmentId = previousOwnerSegmentId;
        NextOwnerSegmentId = nextOwnerSegmentId;
    }

    public long Tick { get; }

    public CellId Cell { get; }

    public EntityId? PreviousOwnerSegmentId { get; }

    public EntityId? NextOwnerSegmentId { get; }
}

public sealed class TunnelJunctionStoneTrimCompleted : IDomainEvent
{
    public TunnelJunctionStoneTrimCompleted(long tick, CellId cell)
    {
        Tick = tick;
        Cell = cell;
    }

    public long Tick { get; }

    public CellId Cell { get; }
}

public sealed class TunnelJunctionStoneTrimCompletionRemoved : IDomainEvent
{
    public TunnelJunctionStoneTrimCompletionRemoved(long tick, CellId cell)
    {
        Tick = tick;
        Cell = cell;
    }

    public long Tick { get; }

    public CellId Cell { get; }
}

internal static class TunnelJunctionStoneTrimProjection
{
    public static TunnelJunctionStoneTrimTargetSnapshot[] DerivePending(
        IEnumerable<HorizontalTunnelSegmentSnapshot> segments,
        IReadOnlyCollection<CellId> completedCells)
    {
        if (segments is null)
        {
            throw new ArgumentNullException(nameof(segments));
        }

        if (completedCells is null)
        {
            throw new ArgumentNullException(nameof(completedCells));
        }

        HashSet<CellId> completed = completedCells.ToHashSet();
        return segments
            .Where(segment => segment.OriginKind == TunnelSegmentOriginKind.VerticalJunction)
            .GroupBy(segment => segment.OriginCell)
            .Where(group => !completed.Contains(group.Key))
            .Select(group => new TunnelJunctionStoneTrimTargetSnapshot(
                group.OrderBy(
                        segment => segment.SegmentId.ToString(),
                        StringComparer.Ordinal)
                    .First()
                    .SegmentId,
                group.Key))
            .OrderBy(target => target.Cell)
            .ThenBy(
                target => target.OwnerSegmentId.ToString(),
                StringComparer.Ordinal)
            .ToArray();
    }
}
}

namespace Dig.Domain.World
{

public sealed class TunnelStoneFloorTrimCompleted : IDomainEvent
{
    public TunnelStoneFloorTrimCompleted(long tick, EntityId segmentId, CellId cell)
    {
        Tick = tick;
        SegmentId = segmentId;
        Cell = cell;
    }

    public long Tick { get; }
    public EntityId SegmentId { get; }
    public CellId Cell { get; }
}

public sealed class TunnelStoneFloorTrimCompletionRemoved : IDomainEvent
{
    public TunnelStoneFloorTrimCompletionRemoved(long tick, CellId cell)
    {
        Tick = tick;
        Cell = cell;
    }

    public long Tick { get; }
    public CellId Cell { get; }
}

}
