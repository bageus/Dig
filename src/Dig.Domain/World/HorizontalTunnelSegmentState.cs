using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.World
{

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

        int nextTargetIndex = _currentAnchorIndex
            + TunnelInfrastructureState.AutomaticSupportInterval;
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
        int targetIndex = _currentAnchorIndex
            + TunnelInfrastructureState.AutomaticSupportInterval;
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
        if (!HasValidOrigin(snapshot, anchors))
        {
            return Result<HorizontalTunnelSegmentState>.Failure(
                TunnelInfrastructureErrors.InvalidSnapshot);
        }

        foreach (TunnelStructuralAnchorSnapshot anchor in anchors
            .Skip(1)
            .OrderBy(value => value.DistanceFromOrigin)
            .ThenBy(value => value.Kind))
        {
            if (!state.HasValidDistance(anchor))
            {
                return Result<HorizontalTunnelSegmentState>.Failure(
                    TunnelInfrastructureErrors.InvalidSnapshot);
            }

            Result<bool> registered = state.RegisterAnchor(anchor.Cell, anchor.Kind);
            if (registered.IsFailure)
            {
                return Result<HorizontalTunnelSegmentState>.Failure(registered.Error!);
            }
        }

        HorizontalTunnelSegmentSnapshot derived = state.CaptureSnapshot();
        if (!derived.StructuralAnchors.SequenceEqual(snapshot.StructuralAnchors)
            || !Nullable.Equals(
                derived.NextAutomaticSupportTarget,
                snapshot.NextAutomaticSupportTarget))
        {
            return Result<HorizontalTunnelSegmentState>.Failure(
                TunnelInfrastructureErrors.InvalidSnapshot);
        }

        state.Version = snapshot.Version;
        return Result<HorizontalTunnelSegmentState>.Success(state);
    }

    private bool HasValidDistance(TunnelStructuralAnchorSnapshot anchor)
    {
        return _cellIndices.TryGetValue(anchor.Cell, out int index)
            && anchor.DistanceFromOrigin == index + 1;
    }

    private static bool HasValidOrigin(
        HorizontalTunnelSegmentSnapshot snapshot,
        TunnelStructuralAnchorSnapshot[] anchors)
    {
        return anchors.Length > 0
            && anchors[0].Cell == snapshot.OriginCell
            && anchors[0].Kind == TunnelStructuralAnchorKind.Origin
            && anchors[0].DistanceFromOrigin == 0;
    }
}
}
