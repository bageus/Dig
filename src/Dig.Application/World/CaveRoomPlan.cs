using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Application.World
{

public enum CaveRoomPlanFailureReason
{
    None = 0,
    EntranceOutOfBounds = 1,
    EntranceBlocked = 2,
    EntranceNotHorizontalTunnel = 3,
    RoomOutOfBounds = 4,
    ProtectedRock = 5,
    RoomObstructed = 6,
    MissingRoof = 7,
    NothingToExcavate = 8,
    BaseTunnelMissing = 9,
    UnmineableRock = 10,
}

public readonly struct CaveRoomInvalidCell
{
    public CaveRoomInvalidCell(CellId cell, CaveRoomPlanFailureReason reason)
    {
        if (reason == CaveRoomPlanFailureReason.None)
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        Cell = cell;
        Reason = reason;
    }

    public CellId Cell { get; }

    public CaveRoomPlanFailureReason Reason { get; }
}

public sealed class CaveRoomPlan
{
    internal CaveRoomPlan(
        CaveRoomPreset preset,
        CellId entrance,
        IReadOnlyList<CellId> frontExcavationCells,
        IReadOnlyList<CaveRoomExcavationTarget> excavationTargets,
        IReadOnlyList<CellId> baseTunnelCells,
        IReadOnlyList<CellId> volumeCells,
        IReadOnlyList<CellId> roofCells)
    {
        Preset = preset ?? throw new ArgumentNullException(nameof(preset));
        Entrance = entrance;
        FrontExcavationCells = Copy(frontExcavationCells, nameof(frontExcavationCells));
        ExcavationTargets = CopyTargets(excavationTargets, nameof(excavationTargets));
        ExcavationCells = new ReadOnlyCollection<CellId>(
            ExcavationTargets.Select(target => target.Cell).ToArray());
        BaseTunnelCells = Copy(baseTunnelCells, nameof(baseTunnelCells));
        VolumeCells = Copy(volumeCells, nameof(volumeCells));
        RoofCells = Copy(roofCells, nameof(roofCells));
        _targetsByCell = ExcavationTargets.ToDictionary(target => target.Cell);
    }

    private readonly IReadOnlyDictionary<CellId, CaveRoomExcavationTarget> _targetsByCell;

    public CaveRoomPreset Preset { get; }
    public CellId Entrance { get; }
    public IReadOnlyList<CellId> FrontExcavationCells { get; }
    public IReadOnlyList<CaveRoomExcavationTarget> ExcavationTargets { get; }
    public IReadOnlyList<CellId> ExcavationCells { get; }
    public IReadOnlyList<CellId> BaseTunnelCells { get; }
    public IReadOnlyList<CellId> VolumeCells { get; }
    public IReadOnlyList<CellId> RoofCells { get; }

    public bool TryGetExcavationTarget(
        CellId cell,
        out CaveRoomExcavationTarget target)
    {
        return _targetsByCell.TryGetValue(cell, out target);
    }

    public static CaveRoomPlan CreateSnapshot(
        CaveRoomPreset preset,
        CellId entrance,
        IEnumerable<CellId> frontExcavationCells,
        IEnumerable<CellId> volumeCells,
        IEnumerable<CellId> roofCells)
    {
        if (preset == null)
        {
            throw new ArgumentNullException(nameof(preset));
        }

        CellId[] front = OrderedUnique(frontExcavationCells, nameof(frontExcavationCells));
        CellId[] volume = OrderedUnique(volumeCells, nameof(volumeCells));
        CellId[] roof = OrderedUnique(roofCells, nameof(roofCells));
        ValidateLegacySnapshot(front, volume);
        return new CaveRoomPlan(
            preset,
            entrance,
            front,
            FullTargets(volume),
            Array.Empty<CellId>(),
            volume,
            roof);
    }

    public static CaveRoomPlan CreateSnapshot(
        CaveRoomPreset preset,
        CellId entrance,
        IEnumerable<CellId> frontExcavationCells,
        IEnumerable<CellId> excavationCells,
        IEnumerable<CellId> baseTunnelCells,
        IEnumerable<CellId> volumeCells,
        IEnumerable<CellId> roofCells)
    {
        return CreateSnapshot(
            preset,
            entrance,
            frontExcavationCells,
            FullTargets(OrderedUnique(excavationCells, nameof(excavationCells))),
            baseTunnelCells,
            volumeCells,
            roofCells);
    }

    public static CaveRoomPlan CreateSnapshot(
        CaveRoomPreset preset,
        CellId entrance,
        IEnumerable<CellId> frontExcavationCells,
        IEnumerable<CaveRoomExcavationTarget> excavationTargets,
        IEnumerable<CellId> baseTunnelCells,
        IEnumerable<CellId> volumeCells,
        IEnumerable<CellId> roofCells)
    {
        if (preset == null)
        {
            throw new ArgumentNullException(nameof(preset));
        }

        CellId[] front = OrderedUnique(frontExcavationCells, nameof(frontExcavationCells));
        CaveRoomExcavationTarget[] targets = OrderedUniqueTargets(
            excavationTargets,
            nameof(excavationTargets));
        CellId[] excavation = targets.Select(target => target.Cell).ToArray();
        CellId[] baseTunnel = OrderedUnique(baseTunnelCells, nameof(baseTunnelCells));
        CellId[] volume = OrderedUnique(volumeCells, nameof(volumeCells));
        CellId[] roof = OrderedUnique(roofCells, nameof(roofCells));
        if (volume.Length == 0)
        {
            throw new ArgumentException("Cave room volume cannot be empty.", nameof(volumeCells));
        }

        if (baseTunnel.Length < 2)
        {
            throw new ArgumentException(
                "Cave room base tunnel must contain left and right entrance cells.",
                nameof(baseTunnelCells));
        }

        if (volume.Any(cell => cell.Z < CellId.MinimumDepth || cell.Z > CellId.MaximumDepth))
        {
            throw new ArgumentException("Cave room volume contains an invalid depth.", nameof(volumeCells));
        }

        if (baseTunnel.Any(cell => cell.Z != CellId.MinimumDepth
                || cell.Y != entrance.Y
                || !volume.Contains(cell)))
        {
            throw new ArgumentException(
                "Base tunnel cells must belong to the complete Z0 base row.",
                nameof(baseTunnelCells));
        }

        if (excavation.Any(cell => !volume.Contains(cell) || baseTunnel.Contains(cell)))
        {
            throw new ArgumentException(
                "Excavation cells must belong to the room volume and exclude the open base tunnel.",
                nameof(excavationTargets));
        }

        if (front.Any(cell => cell.Z != CellId.MinimumDepth || !excavation.Contains(cell)))
        {
            throw new ArgumentException(
                "Front excavation cells must belong to the Z0 excavation mask.",
                nameof(frontExcavationCells));
        }

        return new CaveRoomPlan(
            preset,
            entrance,
            front,
            targets,
            baseTunnel,
            volume,
            roof);
    }

    private static void ValidateLegacySnapshot(
        IReadOnlyCollection<CellId> front,
        IReadOnlyCollection<CellId> volume)
    {
        if (volume.Count == 0)
        {
            throw new ArgumentException(
                "Cave room volume cannot be empty.",
                nameof(volume));
        }

        if (volume.Any(cell =>
                cell.Z < CellId.MinimumDepth || cell.Z > CellId.MaximumDepth))
        {
            throw new ArgumentException(
                "Cave room volume contains an invalid depth.",
                nameof(volume));
        }

        if (front.Any(cell =>
                cell.Z != CellId.MinimumDepth || !volume.Contains(cell)))
        {
            throw new ArgumentException(
                "Front excavation cells must belong to the Z0 room mask.",
                nameof(front));
        }
    }

    private static IReadOnlyList<CellId> Copy(
        IReadOnlyList<CellId> cells,
        string parameterName)
    {
        return new ReadOnlyCollection<CellId>(
            cells?.ToArray() ?? throw new ArgumentNullException(parameterName));
    }

    private static IReadOnlyList<CaveRoomExcavationTarget> CopyTargets(
        IReadOnlyList<CaveRoomExcavationTarget> targets,
        string parameterName)
    {
        return new ReadOnlyCollection<CaveRoomExcavationTarget>(
            targets?.ToArray() ?? throw new ArgumentNullException(parameterName));
    }

    private static CellId[] OrderedUnique(
        IEnumerable<CellId> cells,
        string parameterName)
    {
        if (cells == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        CellId[] ordered = cells.OrderBy(cell => cell).ToArray();
        if (ordered.Distinct().Count() != ordered.Length)
        {
            throw new ArgumentException("Cave room cell lists must be unique.", parameterName);
        }

        return ordered;
    }

    private static CaveRoomExcavationTarget[] OrderedUniqueTargets(
        IEnumerable<CaveRoomExcavationTarget> targets,
        string parameterName)
    {
        if (targets == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        CaveRoomExcavationTarget[] ordered = targets
            .OrderBy(target => target.Cell)
            .ToArray();
        if (ordered.Select(target => target.Cell).Distinct().Count() != ordered.Length)
        {
            throw new ArgumentException(
                "Cave room excavation targets must use unique cells.",
                parameterName);
        }

        return ordered;
    }

    private static CaveRoomExcavationTarget[] FullTargets(
        IEnumerable<CellId> cells)
    {
        return cells.Select(cell => new CaveRoomExcavationTarget(
            cell,
            ExcavationQuarter.All)).ToArray();
    }
}

public sealed class CaveRoomPlanResult
{
    private CaveRoomPlanResult(
        CaveRoomPlan? plan,
        CaveRoomPlanFailureReason failureReason,
        string detail,
        IReadOnlyList<CaveRoomInvalidCell> invalidCells)
    {
        Plan = plan;
        FailureReason = failureReason;
        Detail = detail;
        InvalidCells = new ReadOnlyCollection<CaveRoomInvalidCell>(
            invalidCells?.ToArray() ?? throw new ArgumentNullException(nameof(invalidCells)));
    }

    public bool Succeeded => Plan != null;
    public CaveRoomPlan? Plan { get; }
    public CaveRoomPlanFailureReason FailureReason { get; }
    public string Detail { get; }
    public IReadOnlyList<CaveRoomInvalidCell> InvalidCells { get; }

    internal static CaveRoomPlanResult Success(CaveRoomPlan plan)
    {
        return new CaveRoomPlanResult(
            plan ?? throw new ArgumentNullException(nameof(plan)),
            CaveRoomPlanFailureReason.None,
            "The cave room can be excavated above this complete through tunnel.",
            Array.Empty<CaveRoomInvalidCell>());
    }

    internal static CaveRoomPlanResult Failure(
        CaveRoomPlanFailureReason reason,
        string detail,
        IEnumerable<CaveRoomInvalidCell>? invalidCells = null)
    {
        if (reason == CaveRoomPlanFailureReason.None)
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        return new CaveRoomPlanResult(
            null,
            reason,
            detail,
            invalidCells?.OrderBy(cell => cell.Cell).ThenBy(cell => cell.Reason).ToArray()
                ?? Array.Empty<CaveRoomInvalidCell>());
    }
}

}
