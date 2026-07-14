using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Domain.World;

namespace Dig.Domain.Navigation
{

internal sealed class NavigationRegionBuildResult
{
    public NavigationRegionBuildResult(
        IReadOnlyDictionary<CellId, int> regionsByCell,
        IReadOnlyCollection<NavigationRegionSnapshot> regions)
    {
        RegionsByCell = new ReadOnlyDictionary<CellId, int>(
            new Dictionary<CellId, int>(regionsByCell));
        Regions = new ReadOnlyCollection<NavigationRegionSnapshot>(
            regions.OrderBy(region => region.Id).ToArray());
    }

    public IReadOnlyDictionary<CellId, int> RegionsByCell { get; }

    public IReadOnlyList<NavigationRegionSnapshot> Regions { get; }
}

internal static class NavigationRegionBuilder
{
    public static NavigationRegionBuildResult Build(NavigationSnapshot topology)
    {
        if (topology is null)
        {
            throw new ArgumentNullException(nameof(topology));
        }

        CellId[] cells = topology.Chunks
            .SelectMany(chunk => chunk.WalkableCells)
            .OrderBy(cell => cell)
            .ToArray();
        Dictionary<CellId, HashSet<CellId>> adjacency = BuildWeakAdjacency(
            topology,
            cells);
        Dictionary<CellId, int> regionsByCell = new Dictionary<CellId, int>();
        List<NavigationRegionSnapshot> regions = new List<NavigationRegionSnapshot>();
        ChunkLayout layout = new ChunkLayout(
            topology.WorldSize,
            topology.ChunkSize);

        int nextRegionId = 0;
        foreach (CellId start in cells)
        {
            if (regionsByCell.ContainsKey(start))
            {
                continue;
            }

            int regionId = nextRegionId++;
            int cellCount = 0;
            HashSet<ChunkId> chunks = new HashSet<ChunkId>();
            Queue<CellId> frontier = new Queue<CellId>();
            regionsByCell.Add(start, regionId);
            frontier.Enqueue(start);

            while (frontier.Count > 0)
            {
                CellId current = frontier.Dequeue();
                cellCount++;
                chunks.Add(layout.GetChunk(current));

                foreach (CellId neighbor in adjacency[current].OrderBy(cell => cell))
                {
                    if (regionsByCell.TryAdd(neighbor, regionId))
                    {
                        frontier.Enqueue(neighbor);
                    }
                }
            }

            regions.Add(new NavigationRegionSnapshot(
                regionId,
                cellCount,
                chunks));
        }

        return new NavigationRegionBuildResult(regionsByCell, regions);
    }

    private static Dictionary<CellId, HashSet<CellId>> BuildWeakAdjacency(
        NavigationSnapshot topology,
        IReadOnlyCollection<CellId> cells)
    {
        Dictionary<CellId, HashSet<CellId>> adjacency = cells.ToDictionary(
            cell => cell,
            _ => new HashSet<CellId>());

        foreach (CellId cell in cells)
        {
            foreach (NavigationTransition transition in topology.GetTransitions(cell))
            {
                if (!adjacency.ContainsKey(transition.Target))
                {
                    continue;
                }

                adjacency[cell].Add(transition.Target);
                adjacency[transition.Target].Add(cell);
            }
        }

        return adjacency;
    }
}
}
