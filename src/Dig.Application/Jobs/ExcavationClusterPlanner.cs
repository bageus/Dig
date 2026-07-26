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
        IReadOnlyCollection<CellId> designatedCells)
    {
        return Select(
            seed,
            designatedCells,
            Array.Empty<IReadOnlyCollection<CellId>>());
    }

    public IReadOnlyList<CellId> Select(
        CellId seed,
        IReadOnlyCollection<CellId> designatedCells,
        IReadOnlyCollection<IReadOnlyCollection<CellId>> linkedGroups)
    {
        if (designatedCells is null)
        {
            throw new ArgumentNullException(nameof(designatedCells));
        }

        if (linkedGroups is null)
        {
            throw new ArgumentNullException(nameof(linkedGroups));
        }

        HashSet<CellId> designated = new HashSet<CellId>(designatedCells);
        if (!designated.Contains(seed))
        {
            return Array.Empty<CellId>();
        }

        Dictionary<CellId, CellId[]> links = BuildLinks(designated, linkedGroups);
        Queue<CellId> frontier = new Queue<CellId>();
        HashSet<CellId> visited = new HashSet<CellId> { seed };
        List<CellId> selected = new List<CellId>();
        frontier.Enqueue(seed);
        while (frontier.Count > 0)
        {
            CellId current = frontier.Dequeue();
            selected.Add(current);
            EnqueueNeighbors(current, designated, links, visited, frontier);
        }

        return new ReadOnlyCollection<CellId>(selected
            .OrderBy(cell => Distance(seed, cell))
            .ThenBy(cell => cell)
            .ToArray());
    }

    private static Dictionary<CellId, CellId[]> BuildLinks(
        ISet<CellId> designated,
        IReadOnlyCollection<IReadOnlyCollection<CellId>> linkedGroups)
    {
        Dictionary<CellId, HashSet<CellId>> mutable =
            new Dictionary<CellId, HashSet<CellId>>();
        foreach (IReadOnlyCollection<CellId> group in linkedGroups)
        {
            if (group == null)
            {
                throw new ArgumentException(
                    "Linked excavation groups cannot contain null.",
                    nameof(linkedGroups));
            }

            CellId[] active = group
                .Where(designated.Contains)
                .Distinct()
                .OrderBy(cell => cell)
                .ToArray();
            for (int sourceIndex = 0; sourceIndex < active.Length; sourceIndex++)
            {
                if (!mutable.TryGetValue(active[sourceIndex], out HashSet<CellId>? targets))
                {
                    targets = new HashSet<CellId>();
                    mutable.Add(active[sourceIndex], targets);
                }

                for (int targetIndex = 0; targetIndex < active.Length; targetIndex++)
                {
                    if (sourceIndex != targetIndex)
                    {
                        targets.Add(active[targetIndex]);
                    }
                }
            }
        }

        return mutable.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.OrderBy(cell => cell).ToArray());
    }

    private static void EnqueueNeighbors(
        CellId current,
        ISet<CellId> designated,
        IReadOnlyDictionary<CellId, CellId[]> links,
        ISet<CellId> visited,
        Queue<CellId> frontier)
    {
        foreach (CellId neighbor in HorizontalNeighbors(current))
        {
            TryEnqueue(neighbor, designated, visited, frontier);
        }

        if (!links.TryGetValue(current, out CellId[]? linked))
        {
            return;
        }

        for (int index = 0; index < linked.Length; index++)
        {
            TryEnqueue(linked[index], designated, visited, frontier);
        }
    }

    private static void TryEnqueue(
        CellId cell,
        ISet<CellId> designated,
        ISet<CellId> visited,
        Queue<CellId> frontier)
    {
        if (designated.Contains(cell) && visited.Add(cell))
        {
            frontier.Enqueue(cell);
        }
    }

    private static IEnumerable<CellId> HorizontalNeighbors(CellId cell)
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
