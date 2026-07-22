using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Application.World
{

public sealed class CaveRoomPlanner
{
    public CaveRoomPlanResult Plan(
        WorldSnapshot world,
        ExcavationBoundaryPolicy boundary,
        CaveRoomPresetKind kind,
        CellId entrance,
        IReadOnlyCollection<CaveRoomPlan>? completedPlans = null)
    {
        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        if (boundary is null)
        {
            throw new ArgumentNullException(nameof(boundary));
        }

        if (entrance.Z != CellId.MinimumDepth)
        {
            return CaveRoomPlanResult.Failure(
                CaveRoomPlanFailureReason.EntranceOutOfBounds,
                "Cave room extrusion currently starts on the front Z=0 layer.");
        }

        if (completedPlans?.Any(plan => plan.Entrance == entrance) == true)
        {
            return CaveRoomPlanResult.Failure(
                CaveRoomPlanFailureReason.RoomObstructed,
                "A completed cave room is immutable.");
        }

        Dictionary<CellId, CellSnapshot> cells = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(cell => cell.Id);
        if (!world.Size.Contains(entrance)
            || !cells.TryGetValue(entrance, out CellSnapshot entranceCell))
        {
            return CaveRoomPlanResult.Failure(
                CaveRoomPlanFailureReason.EntranceOutOfBounds,
                "The room entrance is outside the world.");
        }

        if (entranceCell.IsSolid)
        {
            return CaveRoomPlanResult.Failure(
                CaveRoomPlanFailureReason.EntranceBlocked,
                "The room entrance must be an excavated tunnel cell.");
        }

        bool horizontalTunnel = entrance.X > 0
                && IsOpen(cells, new CellId(entrance.X - 1, entrance.Y, entrance.Z))
            || entrance.X + 1 < world.Size.Width
                && IsOpen(cells, new CellId(entrance.X + 1, entrance.Y, entrance.Z));
        if (!horizontalTunnel)
        {
            return CaveRoomPlanResult.Failure(
                CaveRoomPlanFailureReason.EntranceNotHorizontalTunnel,
                "Cave rooms can only be attached to a horizontal tunnel.");
        }

        HashSet<CellId> upgradeOpenCells = BuildUpgradeOpenCells(
            completedPlans,
            entrance);
        CaveRoomPreset preset = CaveRoomPresetCatalog.Get(kind);
        List<CellId> front = new List<CellId>();
        List<CellId> volume = new List<CellId>();
        for (int level = 0; level < preset.Height; level++)
        {
            CaveRoomPlanResult? failure = AddRow(
                world,
                boundary,
                cells,
                upgradeOpenCells,
                preset,
                entrance,
                level,
                front,
                volume);
            if (failure != null)
            {
                return failure;
            }
        }

        List<CellId> roof = new List<CellId>(preset.TopWidth);
        int roofY = entrance.Y - preset.Height;
        int roofMinX = entrance.X - ((preset.TopWidth - 1) / 2);
        for (int offset = 0; offset < preset.TopWidth; offset++)
        {
            int x = roofMinX + offset;
            if (!Contains(world.Size, x, roofY))
            {
                return CaveRoomPlanResult.Failure(
                    CaveRoomPlanFailureReason.MissingRoof,
                    "One complete row of solid rock must remain above the room.");
            }

            CellId roofCell = new CellId(x, roofY, entrance.Z);
            if (!cells.TryGetValue(roofCell, out CellSnapshot roofSnapshot)
                || !roofSnapshot.IsSolid)
            {
                return CaveRoomPlanResult.Failure(
                    CaveRoomPlanFailureReason.MissingRoof,
                    "One complete row of solid rock must remain above the room.");
            }

            roof.Add(roofCell);
        }

        if (front.Count == 0)
        {
            return CaveRoomPlanResult.Failure(
                CaveRoomPlanFailureReason.NothingToExcavate,
                "The selected room cross-section is already open on Z=0.");
        }

        return CaveRoomPlanResult.Success(new CaveRoomPlan(
            preset,
            entrance,
            front,
            volume,
            roof));
    }

    public static int InterpolateWidth(CaveRoomPreset preset, int level)
    {
        if (preset is null)
        {
            throw new ArgumentNullException(nameof(preset));
        }

        if (level < 0 || level >= preset.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        if (preset.Height == 1)
        {
            return preset.BaseWidth;
        }

        double progress = level / (double)(preset.Height - 1);
        double width = preset.BaseWidth
            + ((preset.TopWidth - preset.BaseWidth) * progress);
        return (int)Math.Round(width, MidpointRounding.AwayFromZero);
    }

    private static HashSet<CellId> BuildUpgradeOpenCells(
        IReadOnlyCollection<CaveRoomPlan>? completedPlans,
        CellId entrance)
    {
        HashSet<CellId> cells = new HashSet<CellId>();
        if (completedPlans == null)
        {
            return cells;
        }

        foreach (CaveRoomPlan plan in completedPlans)
        {
            if (plan.Entrance != entrance)
            {
                continue;
            }

            for (int index = 0; index < plan.VolumeCells.Count; index++)
            {
                CellId cell = plan.VolumeCells[index];
                if (cell.Z == 0)
                {
                    cells.Add(new CellId(cell.X, cell.Y, cell.Z));
                }
            }
        }

        return cells;
    }

    private static CaveRoomPlanResult? AddRow(
        WorldSnapshot world,
        ExcavationBoundaryPolicy boundary,
        IReadOnlyDictionary<CellId, CellSnapshot> cells,
        ISet<CellId> upgradeOpenCells,
        CaveRoomPreset preset,
        CellId entrance,
        int level,
        ICollection<CellId> front,
        ICollection<CellId> volume)
    {
        int y = entrance.Y - level;
        int rowWidth = InterpolateWidth(preset, level);
        int minX = entrance.X - ((rowWidth - 1) / 2);
        for (int offset = 0; offset < rowWidth; offset++)
        {
            int x = minX + offset;
            if (!Contains(world.Size, x, y))
            {
                return CaveRoomPlanResult.Failure(
                    CaveRoomPlanFailureReason.RoomOutOfBounds,
                    "The room outline leaves the world bounds.");
            }

            CellId cell = new CellId(x, y, entrance.Z);
            if (!cells.TryGetValue(cell, out CellSnapshot snapshot))
            {
                return CaveRoomPlanResult.Failure(
                    CaveRoomPlanFailureReason.RoomOutOfBounds,
                    "The room outline leaves the world bounds.");
            }

            if (boundary.IsProtected(cell))
            {
                return CaveRoomPlanResult.Failure(
                    CaveRoomPlanFailureReason.ProtectedRock,
                    "The room overlaps a protected rock cell.");
            }

            if (level > 0
                && !snapshot.IsSolid
                && !upgradeOpenCells.Contains(cell))
            {
                return CaveRoomPlanResult.Failure(
                    CaveRoomPlanFailureReason.RoomObstructed,
                    "Only a completed room at the same entrance may overlap the new room.");
            }

            if (snapshot.IsSolid)
            {
                front.Add(cell);
            }

            for (int z = 0; z < preset.Depth; z++)
            {
                volume.Add(new CellId(cell.X, cell.Y, z));
            }
        }

        return null;
    }

    private static bool Contains(WorldSize size, int x, int y)
    {
        return x >= 0 && y >= 0 && x < size.Width && y < size.Height;
    }

    private static bool IsOpen(
        IReadOnlyDictionary<CellId, CellSnapshot> cells,
        CellId cell)
    {
        return cells.TryGetValue(cell, out CellSnapshot snapshot) && !snapshot.IsSolid;
    }
}

}