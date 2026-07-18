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
        IReadOnlyList<SpatialCellId> volumeCells,
        IReadOnlyList<CellId> roofCells)
    {
        Preset = preset ?? throw new ArgumentNullException(nameof(preset));
        Entrance = entrance;
        FrontExcavationCells = new ReadOnlyCollection<CellId>(
            frontExcavationCells?.ToArray()
                ?? throw new ArgumentNullException(nameof(frontExcavationCells)));
        VolumeCells = new ReadOnlyCollection<SpatialCellId>(
            volumeCells?.ToArray()
                ?? throw new ArgumentNullException(nameof(volumeCells)));
        RoofCells = new ReadOnlyCollection<CellId>(
            roofCells?.ToArray()
                ?? throw new ArgumentNullException(nameof(roofCells)));
    }

    public CaveRoomPreset Preset { get; }
    public CellId Entrance { get; }
    public IReadOnlyList<CellId> FrontExcavationCells { get; }
    public IReadOnlyList<SpatialCellId> VolumeCells { get; }
    public IReadOnlyList<CellId> RoofCells { get; }
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
