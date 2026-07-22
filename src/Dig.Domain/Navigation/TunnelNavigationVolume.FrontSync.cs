using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Domain.Navigation
{

public sealed partial class TunnelNavigationVolume
{
    public static TunnelNavigationVolume FromWorldSnapshot(
        WorldSnapshot world,
        IReadOnlyCollection<CellId> plannedVerticalCells,
        TunnelDemoLayout? demoLayout = null)
    {
        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        if (plannedVerticalCells is null)
        {
            throw new ArgumentNullException(nameof(plannedVerticalCells));
        }

        Dictionary<CellId, CellSnapshot> cells = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(cell => cell.Id);
        HashSet<CellId> verticalPlans = new HashSet<CellId>(
            plannedVerticalCells.Where(world.Size.Contains));
        HashSet<CellId> open = new HashSet<CellId>();
        HashSet<CellId> vertical = new HashSet<CellId>();

        foreach (CellSnapshot snapshot in cells.Values)
        {
            if (snapshot.IsSolid)
            {
                continue;
            }

            CellId cell = snapshot.Id;
            bool plannedVertical = verticalPlans.Contains(cell);
            if (!plannedVertical && !IsSupported(cells, world.Size, cell))
            {
                continue;
            }

            open.Add(cell);
            if (plannedVertical)
            {
                vertical.Add(cell);
            }
        }

        return new TunnelNavigationVolume(
            world.Size.Width,
            world.Size.Height,
            world.Size.Depth,
            open,
            vertical,
            demoLayout);
    }

    private static bool IsSupported(
        IReadOnlyDictionary<CellId, CellSnapshot> cells,
        WorldSize size,
        CellId cell)
    {
        CellId below = new CellId(cell.X, cell.Y + 1, cell.Z);
        return size.Contains(below)
            && cells.TryGetValue(below, out CellSnapshot support)
            && support.IsSolid;
    }
}

}
