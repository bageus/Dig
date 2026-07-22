using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Domain.World;

namespace Dig.Domain.Navigation
{

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

public sealed partial class NavigationSnapshot
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

}
}
