using System.Collections.ObjectModel;
using Dig.Domain.World;

namespace Dig.Domain.Navigation;

public readonly struct NavigationTransition
{
    public NavigationTransition(
        CellId target,
        int cost,
        TraversalLinkKind? linkKind = null)
    {
        if (cost <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cost));
        }

        Target = target;
        Cost = cost;
        LinkKind = linkKind;
    }

    public CellId Target { get; }

    public int Cost { get; }

    public TraversalLinkKind? LinkKind { get; }
}

public sealed class NavigationRegionSnapshot
{
    public NavigationRegionSnapshot(
        int id,
        int cellCount,
        IReadOnlyCollection<ChunkId> chunks)
    {
        if (id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        if (cellCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellCount));
        }

        if (chunks is null)
        {
            throw new ArgumentNullException(nameof(chunks));
        }

        Id = id;
        CellCount = cellCount;
        Chunks = new ReadOnlyCollection<ChunkId>(
            chunks.OrderBy(chunk => chunk).ToArray());
    }

    public int Id { get; }

    public int CellCount { get; }

    public IReadOnlyList<ChunkId> Chunks { get; }
}

public sealed class NavigationSnapshot
{
    private readonly ChunkLayout _layout;
    private readonly Dictionary<ChunkId, NavigationChunkSnapshot> _chunks;
    private readonly Dictionary<CellId, int> _regionsByCell;
    private readonly Dictionary<CellId, IReadOnlyList<NavigationTransition>> _linkTransitions;

    public NavigationSnapshot(
        TraversalProfile profile,
        WorldSize worldSize,
        int chunkSize,
        long worldVersion,
        long navigationVersion,
        long linkVersion,
        IReadOnlyCollection<NavigationChunkSnapshot> chunks,
        IReadOnlyDictionary<CellId, int> regionsByCell,
        IReadOnlyCollection<NavigationRegionSnapshot> regions,
        IReadOnlyCollection<TraversalLink> links)
    {
        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize));
        }

        if (worldVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(worldVersion));
        }

        if (navigationVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(navigationVersion));
        }

        if (linkVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(linkVersion));
        }

        if (chunks is null)
        {
            throw new ArgumentNullException(nameof(chunks));
        }

        if (regionsByCell is null)
        {
            throw new ArgumentNullException(nameof(regionsByCell));
        }

        if (regions is null)
        {
            throw new ArgumentNullException(nameof(regions));
        }

        if (links is null)
        {
            throw new ArgumentNullException(nameof(links));
        }

        Profile = profile;
        WorldSize = worldSize;
        ChunkSize = chunkSize;
        WorldVersion = worldVersion;
        NavigationVersion = navigationVersion;
        LinkVersion = linkVersion;
        _layout = new ChunkLayout(worldSize, chunkSize);
        _chunks = chunks.ToDictionary(chunk => chunk.Id);
        _regionsByCell = new Dictionary<CellId, int>(regionsByCell);
        Chunks = new ReadOnlyCollection<NavigationChunkSnapshot>(
            chunks.OrderBy(chunk => chunk.Id).ToArray());
        Regions = new ReadOnlyCollection<NavigationRegionSnapshot>(
            regions.OrderBy(region => region.Id).ToArray());
        Links = new ReadOnlyCollection<TraversalLink>(
            links.OrderBy(link => link.Id, StringComparer.Ordinal).ToArray());
        _linkTransitions = BuildLinkTransitions(Links, profile);
    }

    public TraversalProfile Profile { get; }

    public WorldSize WorldSize { get; }

    public int ChunkSize { get; }

    public long WorldVersion { get; }

    public long NavigationVersion { get; }

    public long LinkVersion { get; }

    public IReadOnlyList<NavigationChunkSnapshot> Chunks { get; }

    public IReadOnlyList<NavigationRegionSnapshot> Regions { get; }

    public IReadOnlyList<TraversalLink> Links { get; }

    public bool IsWalkable(CellId cellId)
    {
        if (!WorldSize.Contains(cellId))
        {
            return false;
        }

        ChunkId chunkId = _layout.GetChunk(cellId);
        return _chunks.TryGetValue(chunkId, out NavigationChunkSnapshot? chunk)
            && chunk.Contains(cellId);
    }

    public bool TryGetRegion(CellId cellId, out int regionId)
    {
        return _regionsByCell.TryGetValue(cellId, out regionId);
    }

    public bool TryGetChunkStamp(
        ChunkId chunkId,
        out NavigationChunkStamp stamp)
    {
        if (_chunks.TryGetValue(chunkId, out NavigationChunkSnapshot? chunk))
        {
            stamp = chunk.Stamp;
            return true;
        }

        stamp = default;
        return false;
    }

    public NavigationChunkStamp GetChunkStamp(CellId cellId)
    {
        ChunkId chunkId = _layout.GetChunk(cellId);
        return _chunks[chunkId].Stamp;
    }

    public IReadOnlyList<NavigationTransition> GetTransitions(CellId from)
    {
        if (!IsWalkable(from))
        {
            return Array.Empty<NavigationTransition>();
        }

        Dictionary<CellId, NavigationTransition> transitions =
            new Dictionary<CellId, NavigationTransition>();
        AddNormalTransitions(from, transitions);
        if (_linkTransitions.TryGetValue(
            from,
            out IReadOnlyList<NavigationTransition>? links))
        {
            foreach (NavigationTransition transition in links)
            {
                AddLowestCost(transitions, transition);
            }
        }

        NavigationTransition[] ordered = transitions.Values
            .OrderBy(transition => transition.Target)
            .ThenBy(transition => transition.Cost)
            .ToArray();
        return new ReadOnlyCollection<NavigationTransition>(ordered);
    }

    private void AddNormalTransitions(
        CellId from,
        Dictionary<CellId, NavigationTransition> transitions)
    {
        AddIfWalkable(
            transitions,
            new CellId(from.X - 1, from.Y),
            Profile.OrthogonalCost);
        AddIfWalkable(
            transitions,
            new CellId(from.X + 1, from.Y),
            Profile.OrthogonalCost);

        if (Profile.Mode == TraversalMode.Free)
        {
            AddIfWalkable(
                transitions,
                new CellId(from.X, from.Y - 1),
                Profile.OrthogonalCost);
            AddIfWalkable(
                transitions,
                new CellId(from.X, from.Y + 1),
                Profile.OrthogonalCost);
            return;
        }

        foreach (int direction in new[] { -1, 1 })
        {
            for (int step = 1; step <= Profile.MaxStepUp; step++)
            {
                AddIfWalkable(
                    transitions,
                    new CellId(from.X + direction, from.Y + step),
                    checked(Profile.StepCost * step));
            }

            for (int step = 1; step <= Profile.MaxStepDown; step++)
            {
                AddIfWalkable(
                    transitions,
                    new CellId(from.X + direction, from.Y - step),
                    checked(Profile.StepCost * step));
            }
        }
    }

    private void AddIfWalkable(
        Dictionary<CellId, NavigationTransition> transitions,
        CellId target,
        int cost)
    {
        if (IsWalkable(target))
        {
            AddLowestCost(
                transitions,
                new NavigationTransition(target, cost));
        }
    }

    private static void AddLowestCost(
        Dictionary<CellId, NavigationTransition> transitions,
        NavigationTransition candidate)
    {
        if (!transitions.TryGetValue(
            candidate.Target,
            out NavigationTransition existing)
            || candidate.Cost < existing.Cost)
        {
            transitions[candidate.Target] = candidate;
        }
    }

    private static Dictionary<CellId, IReadOnlyList<NavigationTransition>>
        BuildLinkTransitions(
            IReadOnlyList<TraversalLink> links,
            TraversalProfile profile)
    {
        Dictionary<CellId, List<NavigationTransition>> mutable =
            new Dictionary<CellId, List<NavigationTransition>>();
        foreach (TraversalLink link in links)
        {
            if (!profile.Allows(link.Kind))
            {
                continue;
            }

            AddLink(mutable, link.From, link.To, link);
            if (link.Bidirectional)
            {
                AddLink(mutable, link.To, link.From, link);
            }
        }

        return mutable.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<NavigationTransition>)new ReadOnlyCollection<NavigationTransition>(
                pair.Value
                    .OrderBy(transition => transition.Target)
                    .ThenBy(transition => transition.Cost)
                    .ToArray()));
    }

    private static void AddLink(
        Dictionary<CellId, List<NavigationTransition>> transitions,
        CellId from,
        CellId to,
        TraversalLink link)
    {
        if (!transitions.TryGetValue(from, out List<NavigationTransition>? list))
        {
            list = new List<NavigationTransition>();
            transitions.Add(from, list);
        }

        list.Add(new NavigationTransition(to, link.Cost, link.Kind));
    }
}
