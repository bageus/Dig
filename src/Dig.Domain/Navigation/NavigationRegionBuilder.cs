using System.Collections.ObjectModel;
using Dig.Domain.World;

namespace Dig.Domain.Navigation;

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

        Dictionary<CellId, int> regionsByCell = new Dictionary<CellId, int>();
        List<NavigationRegionSnapshot> regions = new List<NavigationRegionSnapshot>();
        ChunkLayout layout = new ChunkLayout(
            topology.WorldSize,
            topology.ChunkSize);
        CellId[] cells = topology.Chunks
            .SelectMany(chunk => chunk.WalkableCells)
            .OrderBy(cell => cell)
            .ToArray();

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

                foreach (NavigationTransition transition in topology.GetTransitions(current))
                {
                    if (regionsByCell.TryAdd(transition.Target, regionId))
                    {
                        frontier.Enqueue(transition.Target);
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
}
