using System.Collections.Generic;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Domain.Navigation
{
public sealed partial class NavigationMap
{
    private static IReadOnlyCollection<CellId> CollectShaftGapCells(
        WorldSnapshot world)
    {
        Dictionary<CellId, CellSnapshot> cells = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(cell => cell.Id);
        List<CellId> result = new List<CellId>();
        foreach (CellSnapshot cell in cells.Values)
        {
            if (cell.IsSolid && !cell.State.IsExcavationOpen)
            {
                continue;
            }

            if (!HasFullActorSupport(cells, world.Size, cell.Id)
                && IsShaftTopologyCell(cells, world.Size, cell.Id))
            {
                result.Add(cell.Id);
            }
        }

        result.Sort();
        return result;
    }

    private static bool HasFullActorSupport(
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

    private static bool IsShaftTopologyCell(
        IReadOnlyDictionary<CellId, CellSnapshot> cells,
        WorldSize size,
        CellId cell)
    {
        if (cells.TryGetValue(cell, out CellSnapshot current)
            && current.State.ExcavationCutPattern == ExcavationCutPattern.HorizontalRows)
        {
            return true;
        }

        CellId above = new CellId(cell.X, cell.Y - 1, cell.Z);
        CellId below = new CellId(cell.X, cell.Y + 1, cell.Z);
        return HasHorizontalRowPattern(cells, size, above)
            || HasHorizontalRowPattern(cells, size, below);
    }

    private static bool HasHorizontalRowPattern(
        IReadOnlyDictionary<CellId, CellSnapshot> cells,
        WorldSize size,
        CellId cell)
    {
        return size.Contains(cell)
            && cells.TryGetValue(cell, out CellSnapshot snapshot)
            && snapshot.State.ExcavationCutPattern
                == ExcavationCutPattern.HorizontalRows;
    }
}
}
