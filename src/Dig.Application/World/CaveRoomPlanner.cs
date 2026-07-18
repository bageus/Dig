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
        CellId entrance)
    {
        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        if (boundary is null)
        {
            throw new ArgumentNullException(nameof(boundary));
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
                && IsOpen(cells, new CellId(entrance.X - 1, entrance.Y))
            || entrance.X + 1 < world.Size.Width
                && IsOpen(cells, new CellId(entrance.X + 1, entrance.Y));
        if (!horizontalTunnel)
        {
            return CaveRoomPlanResult.Failure(
                CaveRoomPlanFailureReason.EntranceNotHorizontalTunnel,
                "Cave rooms can only be attached to a horizontal tunnel.");
        }

        CaveRoomPreset preset = CaveRoomPresetCatalog.Get(kind);
        List<CellId> front = new List<CellId>();
        List<SpatialCellId> volume = new List<SpatialCellId>();
        for (int level = 0; level < preset.Height; level++)
        {
            CaveRoomPlanResult? failure = AddRow(
                world,
                boundary,
                cells,
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

            CellId roofCell = new CellId(x, roofY);
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

    private static CaveRoomPlanResult? AddRow(
        WorldSnapshot world,
        ExcavationBoundaryPolicy boundary,
        IReadOnlyDictionary<CellId, CellSnapshot> cells,
        CaveRoomPreset preset,
        CellId entrance,
        int level,
        ICollection<CellId> front,
        ICollection<SpatialCellId> volume)
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

            CellId cell = new CellId(x, y);
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

            if (level > 0 && !snapshot.IsSolid)
            {
                return CaveRoomPlanResult.Failure(
                    CaveRoomPlanFailureReason.RoomObstructed,
                    "Only the entrance row may overlap an existing tunnel.");
            }

            if (snapshot.IsSolid)
            {
                front.Add(cell);
            }

            for (int z = 0; z < preset.Depth; z++)
            {
                volume.Add(new SpatialCellId(cell.X, cell.Y, z));
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
