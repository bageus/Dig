using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Domain.Navigation
{

public sealed partial class TunnelNavigationVolume
{
    public TunnelNavigationVolume WithSynchronizedFrontLayer(
        WorldSnapshot world,
        IReadOnlyCollection<CellId> plannedVerticalCells)
    {
        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        if (plannedVerticalCells is null)
        {
            throw new ArgumentNullException(nameof(plannedVerticalCells));
        }

        if (world.Size.Width != Width || world.Size.Height != Height)
        {
            throw new ArgumentException(
                "The world and tunnel volume dimensions must match.",
                nameof(world));
        }

        Dictionary<CellId, CellSnapshot> cells = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(cell => cell.Id);
        HashSet<CellId> verticalPlans = new HashSet<CellId>(plannedVerticalCells);
        HashSet<SpatialCellId> open = new HashSet<SpatialCellId>(Cells);
        HashSet<SpatialCellId> vertical = new HashSet<SpatialCellId>(VerticalCells);

        foreach (CellSnapshot snapshot in cells.Values)
        {
            if (snapshot.IsSolid)
            {
                continue;
            }

            CellId cell = snapshot.Id;
            SpatialCellId front = new SpatialCellId(cell.X, cell.Y, 0);
            bool plannedVertical = verticalPlans.Contains(cell);
            bool supported = IsSupported(cells, cell);
            if (!plannedVertical && !supported)
            {
                continue;
            }

            open.Add(front);
            if (plannedVertical)
            {
                vertical.Add(front);
            }
        }

        if (_openCells.SetEquals(open) && _verticalCells.SetEquals(vertical))
        {
            return this;
        }

        return new TunnelNavigationVolume(
            Width,
            Height,
            Depth,
            open,
            vertical,
            DemoLayout);
    }

    private static bool IsSupported(
        IReadOnlyDictionary<CellId, CellSnapshot> cells,
        CellId cell)
    {
        CellId below = new CellId(cell.X, cell.Y + 1);
        return cells.TryGetValue(below, out CellSnapshot support) && support.IsSolid;
    }
}

}
