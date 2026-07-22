using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Application.World
{

public sealed class CaveRoomShellProtectionPolicy
{
    public IReadOnlyList<CellId> Resolve(CaveRoomPlan plan, WorldSize worldSize)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        HashSet<CellId> protectedCells = new HashSet<CellId>(plan.RoofCells);
        for (int level = 0; level < plan.Preset.Height; level++)
        {
            int y = plan.Entrance.Y - level;
            int width = CaveRoomPlanner.InterpolateWidth(plan.Preset, level);
            int minX = plan.Entrance.X - ((width - 1) / 2);
            AddIfContained(protectedCells, worldSize, minX - 1, y);
            AddIfContained(protectedCells, worldSize, minX + width, y);
        }

        int floorY = plan.Entrance.Y + 1;
        int floorMinX = plan.Entrance.X - ((plan.Preset.BaseWidth - 1) / 2);
        for (int offset = 0; offset < plan.Preset.BaseWidth; offset++)
        {
            AddIfContained(protectedCells, worldSize, floorMinX + offset, floorY);
        }

        return protectedCells.OrderBy(cell => cell).ToArray();
    }

    private static void AddIfContained(
        ISet<CellId> cells,
        WorldSize size,
        int x,
        int y)
    {
        if (x >= 0 && y >= 0 && x < size.Width && y < size.Height)
        {
            cells.Add(new CellId(x, y, 0));
        }
    }
}

}