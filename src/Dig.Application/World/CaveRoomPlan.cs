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
}

public sealed class CaveRoomPlan
{
    internal CaveRoomPlan(
        CaveRoomPreset preset,
        CellId entrance,
        IReadOnlyList<CellId> frontExcavationCells,
        IReadOnlyList<CellId> volumeCells,
        IReadOnlyList<CellId> roofCells)
    {
        Preset = preset ?? throw new ArgumentNullException(nameof(preset));
        Entrance = entrance;
        FrontExcavationCells = Copy(frontExcavationCells, nameof(frontExcavationCells));
        VolumeCells = Copy(volumeCells, nameof(volumeCells));
        RoofCells = Copy(roofCells, nameof(roofCells));
    }

    public CaveRoomPreset Preset { get; }
    public CellId Entrance { get; }
    public IReadOnlyList<CellId> FrontExcavationCells { get; }
    public IReadOnlyList<CellId> VolumeCells { get; }
    public IReadOnlyList<CellId> RoofCells { get; }

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
        if (volume.Length == 0)
        {
            throw new ArgumentException("Cave room volume cannot be empty.", nameof(volumeCells));
        }

        if (volume.Any(cell => cell.Z < CellId.MinimumDepth || cell.Z > CellId.MaximumDepth))
        {
            throw new ArgumentException("Cave room volume contains an invalid depth.", nameof(volumeCells));
        }

        if (front.Any(cell => cell.Z != CellId.MinimumDepth || !volume.Contains(cell)))
        {
            throw new ArgumentException(
                "Front excavation cells must belong to the Z0 volume slice.",
                nameof(frontExcavationCells));
        }

        return new CaveRoomPlan(preset, entrance, front, volume, roof);
    }

    private static IReadOnlyList<CellId> Copy(
        IReadOnlyList<CellId> cells,
        string parameterName)
    {
        return new ReadOnlyCollection<CellId>(
            cells?.ToArray() ?? throw new ArgumentNullException(parameterName));
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
}

public sealed class CaveRoomPlanResult
{
    private CaveRoomPlanResult(
        CaveRoomPlan? plan,
        CaveRoomPlanFailureReason failureReason,
        string detail)
    {
        Plan = plan;
        FailureReason = failureReason;
        Detail = detail;
    }

    public bool Succeeded => Plan != null;
    public CaveRoomPlan? Plan { get; }
    public CaveRoomPlanFailureReason FailureReason { get; }
    public string Detail { get; }

    internal static CaveRoomPlanResult Success(CaveRoomPlan plan)
    {
        return new CaveRoomPlanResult(
            plan ?? throw new ArgumentNullException(nameof(plan)),
            CaveRoomPlanFailureReason.None,
            "The cave room can be excavated from this horizontal tunnel cell.");
    }

    internal static CaveRoomPlanResult Failure(
        CaveRoomPlanFailureReason reason,
        string detail)
    {
        if (reason == CaveRoomPlanFailureReason.None)
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        return new CaveRoomPlanResult(null, reason, detail);
    }
}

}
