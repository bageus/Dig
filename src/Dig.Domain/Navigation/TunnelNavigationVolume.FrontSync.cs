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
        IReadOnlyCollection<CellId> plannedTunnelCells,
        IReadOnlyCollection<CellId> plannedVerticalCells,
        TunnelDemoLayout? demoLayout = null)
    {
        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        if (plannedTunnelCells is null)
        {
            throw new ArgumentNullException(nameof(plannedTunnelCells));
        }

        if (plannedVerticalCells is null)
        {
            throw new ArgumentNullException(nameof(plannedVerticalCells));
        }

        Dictionary<CellId, CellSnapshot> cells = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(cell => cell.Id);
        HashSet<CellId> tunnelPlans = new HashSet<CellId>(
            plannedTunnelCells.Where(world.Size.Contains));
        HashSet<CellId> verticalPlans = new HashSet<CellId>(
            plannedVerticalCells.Where(world.Size.Contains));
        HashSet<CellId> open = new HashSet<CellId>();
        HashSet<CellId> vertical = new HashSet<CellId>();
        HashSet<CellId> supported = new HashSet<CellId>();

        foreach (CellSnapshot snapshot in cells.Values)
        {
            if (!snapshot.State.IsExplored)
            {
                continue;
            }

            if (snapshot.IsSolid && !snapshot.State.IsExcavationOpen)
            {
                continue;
            }

            CellId cell = snapshot.Id;
            bool plannedTunnel = tunnelPlans.Contains(cell);
            bool plannedVertical = verticalPlans.Contains(cell);
            bool verticalEndpoint = IsVerticalEndpoint(
                verticalPlans,
                world.Size,
                cell);
            if (!plannedTunnel
                && !verticalEndpoint
                && !IsSupported(cells, world.Size, cell))
            {
                continue;
            }

            open.Add(cell);
            if (plannedVertical)
            {
                vertical.Add(cell);
            }

            if (IsSupported(cells, world.Size, cell))
            {
                supported.Add(cell);
            }
        }

        return new TunnelNavigationVolume(
            world.Size.Width,
            world.Size.Height,
            world.Size.Depth,
            open,
            vertical,
            supported,
            demoLayout);
    }

    private static bool IsVerticalEndpoint(
        HashSet<CellId> verticalPlans,
        WorldSize size,
        CellId cell)
    {
        CellId above = new CellId(cell.X, cell.Y - 1, cell.Z);
        CellId below = new CellId(cell.X, cell.Y + 1, cell.Z);
        return (size.Contains(above) && verticalPlans.Contains(above))
            || (size.Contains(below) && verticalPlans.Contains(below));
    }

    private static bool IsSupported(
        IReadOnlyDictionary<CellId, CellSnapshot> cells,
        WorldSize size,
        CellId cell)
    {
        CellId below = new CellId(cell.X, cell.Y + 1, cell.Z);
        return size.Contains(below)
            && cells.TryGetValue(below, out CellSnapshot support)
            && support.IsSolid
            && support.State.CompletedExcavationQuarters == ExcavationQuarter.None;
    }
}

}
