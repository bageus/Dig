using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Application.Tunnels
{

public readonly struct TunnelTopologySegmentKey : IEquatable<TunnelTopologySegmentKey>, IComparable<TunnelTopologySegmentKey>
{
    public TunnelTopologySegmentKey(
        TunnelSegmentOriginKind originKind,
        CellId originCell,
        int direction)
    {
        if (direction is not -1 and not 1)
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        OriginKind = originKind;
        OriginCell = originCell;
        Direction = direction;
    }

    public TunnelSegmentOriginKind OriginKind { get; }
    public CellId OriginCell { get; }
    public int Direction { get; }

    public int CompareTo(TunnelTopologySegmentKey other)
    {
        int origin = OriginCell.CompareTo(other.OriginCell);
        if (origin != 0)
        {
            return origin;
        }

        int kind = OriginKind.CompareTo(other.OriginKind);
        return kind != 0 ? kind : Direction.CompareTo(other.Direction);
    }

    public bool Equals(TunnelTopologySegmentKey other)
    {
        return OriginKind == other.OriginKind
            && OriginCell == other.OriginCell
            && Direction == other.Direction;
    }

    public override bool Equals(object? obj)
    {
        return obj is TunnelTopologySegmentKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(OriginKind, OriginCell, Direction);
    }
}

public sealed class TunnelTopologySegmentProvenance
{
    private readonly CellId[] _orderedHorizontalCells;

    public TunnelTopologySegmentProvenance(
        EntityId segmentId,
        TunnelSegmentOriginKind originKind,
        CellId originCell,
        IEnumerable<CellId> orderedHorizontalCells)
    {
        if (segmentId.IsEmpty)
        {
            throw new ArgumentException("Segment id cannot be empty.", nameof(segmentId));
        }

        if (orderedHorizontalCells is null)
        {
            throw new ArgumentNullException(nameof(orderedHorizontalCells));
        }

        CellId[] cells = orderedHorizontalCells.ToArray();
        if (cells.Length == 0)
        {
            throw new ArgumentException(
                "Completed tunnel provenance must contain at least one horizontal cell.",
                nameof(orderedHorizontalCells));
        }

        int direction = Math.Sign(cells[0].X - originCell.X);
        if (direction == 0)
        {
            throw new ArgumentException(
                "Tunnel provenance must start beside its origin.",
                nameof(orderedHorizontalCells));
        }

        for (int index = 0; index < cells.Length; index++)
        {
            CellId expected = new CellId(
                checked(originCell.X + (direction * (index + 1))),
                originCell.Y,
                originCell.Z);
            if (cells[index] != expected)
            {
                throw new ArgumentException(
                    "Tunnel provenance must be one ordered contiguous horizontal direction.",
                    nameof(orderedHorizontalCells));
            }
        }

        SegmentId = segmentId;
        OriginKind = originKind;
        OriginCell = originCell;
        Direction = direction;
        _orderedHorizontalCells = cells;
    }

    public EntityId SegmentId { get; }
    public TunnelSegmentOriginKind OriginKind { get; }
    public CellId OriginCell { get; }
    public int Direction { get; }
    public TunnelTopologySegmentKey Key => new TunnelTopologySegmentKey(
        OriginKind,
        OriginCell,
        Direction);
    public IReadOnlyList<CellId> OrderedHorizontalCells =>
        new ReadOnlyCollection<CellId>(_orderedHorizontalCells);
}

public sealed class SynchronizeTunnelTopologyCommand
    : ICommand<Result<TunnelTopologySynchronizationResult>>
{
    private readonly TunnelTopologySegmentProvenance[] _segments;

    public SynchronizeTunnelTopologyCommand(
        IEnumerable<TunnelTopologySegmentProvenance> segments,
        long tick)
    {
        if (segments is null)
        {
            throw new ArgumentNullException(nameof(segments));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        _segments = segments
            .OrderBy(value => value.Key)
            .ThenBy(value => value.SegmentId.ToString(), StringComparer.Ordinal)
            .ToArray();
        if (_segments.Select(value => value.Key).Distinct().Count() != _segments.Length)
        {
            throw new ArgumentException(
                "Completed tunnel provenance contains duplicate topology directions.",
                nameof(segments));
        }

        if (_segments.Select(value => value.SegmentId).Distinct().Count() != _segments.Length)
        {
            throw new ArgumentException(
                "Completed tunnel provenance contains duplicate segment ids.",
                nameof(segments));
        }

        Tick = tick;
    }

    public IReadOnlyList<TunnelTopologySegmentProvenance> Segments =>
        new ReadOnlyCollection<TunnelTopologySegmentProvenance>(_segments);
    public long Tick { get; }
}

public sealed class TunnelTopologySynchronizationResult
{
    public TunnelTopologySynchronizationResult(
        int added,
        int updated,
        int removed,
        int retained)
    {
        Added = added;
        Updated = updated;
        Removed = removed;
        Retained = retained;
    }

    public int Added { get; }
    public int Updated { get; }
    public int Removed { get; }
    public int Retained { get; }
}

public static class TunnelTopologySynchronizationErrors
{
    public static readonly DomainError SegmentIdentityMismatch = new DomainError(
        "tunnel.topology.segment_identity_mismatch",
        "Completed excavation provenance changed the stable id of an existing tunnel direction.");

    public static readonly DomainError SegmentIdConflict = new DomainError(
        "tunnel.topology.segment_id_conflict",
        "Completed excavation provenance reused a segment id for another tunnel direction.");
}
}
