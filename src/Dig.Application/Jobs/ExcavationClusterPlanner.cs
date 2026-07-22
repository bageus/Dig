using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Application.Jobs
{

public sealed class ExcavationClusterPlanner
{
    public IReadOnlyList<CellId> Select(
        CellId seed,
        IReadOnlyCollection<CellId> designatedCells,
        int radius = 4)
    {
        if (designatedCells is null)
        {
            throw new ArgumentNullException(nameof(designatedCells));
        }

        if (radius < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius));
        }

        HashSet<CellId> designated = new HashSet<CellId>(designatedCells);
        if (!designated.Contains(seed))
        {
            return Array.Empty<CellId>();
        }

        Queue<CellId> frontier = new Queue<CellId>();
        HashSet<CellId> visited = new HashSet<CellId> { seed };
        List<CellId> selected = new List<CellId>();
        frontier.Enqueue(seed);
        while (frontier.Count > 0)
        {
            CellId current = frontier.Dequeue();
            selected.Add(current);
            foreach (CellId neighbor in Neighbors(current))
            {
                if (Distance(seed, neighbor) > radius
                    || !designated.Contains(neighbor)
                    || !visited.Add(neighbor))
                {
                    continue;
                }

                frontier.Enqueue(neighbor);
            }
        }

        return new ReadOnlyCollection<CellId>(selected
            .OrderBy(cell => Distance(seed, cell))
            .ThenBy(cell => cell.Y)
            .ThenBy(cell => cell.X)
            .ToArray());
    }

    private static IEnumerable<CellId> Neighbors(CellId cell)
    {
        if (cell.X > 0)
        {
            yield return new CellId(cell.X - 1, cell.Y, cell.Z);
        }

        yield return new CellId(cell.X + 1, cell.Y, cell.Z);
        if (cell.Y > 0)
        {
            yield return new CellId(cell.X, cell.Y - 1, cell.Z);
        }

        yield return new CellId(cell.X, cell.Y + 1, cell.Z);
    }

    private static int Distance(CellId first, CellId second)
    {
        return Math.Abs(first.X - second.X)
            + Math.Abs(first.Y - second.Y)
            + Math.Abs(first.Z - second.Z);
    }
}

}
