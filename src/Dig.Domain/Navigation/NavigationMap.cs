using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Navigation
{

public sealed class NavigationMap
{
    private readonly Dictionary<ChunkId, NavigationChunkSnapshot> _chunks;
    private TraversalLink[] _links;
    private NavigationSnapshot? _snapshot;

    public NavigationMap(TraversalProfile profile)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _chunks = new Dictionary<ChunkId, NavigationChunkSnapshot>();
        _links = Array.Empty<TraversalLink>();
    }

    public TraversalProfile Profile { get; }

    public long Version { get; private set; }

    public long LinkVersion { get; private set; }

    public NavigationUpdateDiagnostics? LastDiagnostics { get; private set; }

    public Result<NavigationSnapshot> GetSnapshot()
    {
        return _snapshot is null
            ? Result<NavigationSnapshot>.Failure(NavigationErrors.NotBuilt)
            : Result<NavigationSnapshot>.Success(_snapshot);
    }

    public Result<NavigationUpdateDiagnostics> Rebuild(
        WorldSnapshot world,
        IEnumerable<TraversalLink> links)
    {
        return Update(world, Array.Empty<ChunkId>(), links, fullRebuild: true);
    }

    public Result<NavigationUpdateDiagnostics> Refresh(
        WorldSnapshot world,
        IReadOnlyCollection<ChunkId> invalidatedChunks,
        IEnumerable<TraversalLink> links)
    {
        if (invalidatedChunks is null)
        {
            throw new ArgumentNullException(nameof(invalidatedChunks));
        }

        if (_snapshot is null)
        {
            return Result<NavigationUpdateDiagnostics>.Failure(
                NavigationErrors.NotBuilt);
        }

        if (_snapshot.WorldSize.Width != world.Size.Width
            || _snapshot.WorldSize.Height != world.Size.Height
            || _snapshot.WorldSize.Depth != world.Size.Depth
            || _snapshot.ChunkSize != world.ChunkSize)
        {
            return Result<NavigationUpdateDiagnostics>.Failure(
                NavigationErrors.WorldLayoutMismatch);
        }

        return Update(world, invalidatedChunks, links, fullRebuild: false);
    }

    private Result<NavigationUpdateDiagnostics> Update(
        WorldSnapshot world,
        IReadOnlyCollection<ChunkId> invalidatedChunks,
        IEnumerable<TraversalLink> links,
        bool fullRebuild)
    {
        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        if (links is null)
        {
            throw new ArgumentNullException(nameof(links));
        }

        Result<NavigationWorldIndex> indexResult = NavigationWorldIndex.Create(world);
        if (indexResult.IsFailure)
        {
            return Result<NavigationUpdateDiagnostics>.Failure(indexResult.Error!);
        }

        NavigationWorldIndex index = indexResult.Value;
        Result<TraversalLink[]> linksResult = NormalizeLinks(world.Size, links);
        if (linksResult.IsFailure)
        {
            return Result<NavigationUpdateDiagnostics>.Failure(linksResult.Error!);
        }

        TraversalLink[] normalizedLinks = linksResult.Value;
        bool linksChanged = !LinksEqual(_links, normalizedLinks);
        HashSet<ChunkId> rebuild = new HashSet<ChunkId>();

        if (fullRebuild)
        {
            _chunks.Clear();
            for (int z = 0; z < index.Layout.ChunkCountZ; z++)
            {
                for (int y = 0; y < index.Layout.ChunkCountY; y++)
                {
                    for (int x = 0; x < index.Layout.ChunkCountX; x++)
                    {
                        rebuild.Add(new ChunkId(x, y, z));
                    }
                }
            }
        }
        else
        {
            foreach (ChunkId chunkId in invalidatedChunks)
            {
                if (!index.Layout.Contains(chunkId))
                {
                    return Result<NavigationUpdateDiagnostics>.Failure(
                        NavigationErrors.InvalidatedChunkOutOfBounds);
                }

                rebuild.Add(chunkId);
            }

            foreach (ChunkSnapshot chunk in world.Chunks)
            {
                if (!_chunks.TryGetValue(
                    chunk.Id,
                    out NavigationChunkSnapshot? current)
                    || current.SourceChunkVersion != chunk.ChunkVersion)
                {
                    rebuild.Add(chunk.Id);
                }
            }
        }

        if (linksChanged)
        {
            AddLinkEndpointChunks(rebuild, index.Layout, _links);
            AddLinkEndpointChunks(rebuild, index.Layout, normalizedLinks);
        }

        long previousVersion = Version;
        if (fullRebuild || rebuild.Count > 0 || linksChanged)
        {
            Version = checked(Version + 1);
        }

        if (linksChanged)
        {
            LinkVersion = checked(LinkVersion + 1);
        }

        HashSet<CellId> linkEndpoints = GetAllowedLinkEndpoints(normalizedLinks);
        int extractedCellCount = 0;
        foreach (ChunkId chunkId in rebuild.OrderBy(chunk => chunk))
        {
            ChunkSnapshot chunk = index.GetChunk(chunkId);
            _chunks[chunkId] = BuildChunk(
                chunk,
                index,
                linkEndpoints,
                Version);
            extractedCellCount = checked(extractedCellCount + chunk.Cells.Count);
        }

        _links = normalizedLinks;
        NavigationSnapshot topology = CreateSnapshot(
            world,
            new Dictionary<CellId, int>(),
            Array.Empty<NavigationRegionSnapshot>());
        NavigationRegionBuildResult regionBuild =
            NavigationRegionBuilder.Build(topology);
        _snapshot = CreateSnapshot(
            world,
            regionBuild.RegionsByCell,
            regionBuild.Regions);

        LastDiagnostics = new NavigationUpdateDiagnostics(
            fullRebuild,
            previousVersion,
            Version,
            world.Version,
            extractedCellCount,
            regionBuild.Regions.Count,
            rebuild);
        return Result<NavigationUpdateDiagnostics>.Success(LastDiagnostics);
    }

    private NavigationChunkSnapshot BuildChunk(
        ChunkSnapshot chunk,
        NavigationWorldIndex index,
        HashSet<CellId> linkEndpoints,
        long navigationVersion)
    {
        List<CellId> walkable = new List<CellId>();
        foreach (CellSnapshot cell in chunk.Cells)
        {
            if (IsWalkable(cell, index, linkEndpoints))
            {
                walkable.Add(cell.Id);
            }
        }

        return new NavigationChunkSnapshot(
            chunk.Id,
            chunk.Bounds,
            chunk.ChunkVersion,
            navigationVersion,
            walkable);
    }

    private bool IsWalkable(
        CellSnapshot cell,
        NavigationWorldIndex index,
        HashSet<CellId> linkEndpoints)
    {
        if (cell.IsSolid)
        {
            return false;
        }

        if (Profile.Mode == TraversalMode.Free || linkEndpoints.Contains(cell.Id))
        {
            return true;
        }

        if (cell.Id.Y == 0)
        {
            return true;
        }

        CellId belowId = new CellId(cell.Id.X, cell.Id.Y - 1, cell.Id.Z);
        return index.TryGetCell(belowId, out CellSnapshot below) && below.IsSolid;
    }

    private NavigationSnapshot CreateSnapshot(
        WorldSnapshot world,
        IReadOnlyDictionary<CellId, int> regionsByCell,
        IReadOnlyCollection<NavigationRegionSnapshot> regions)
    {
        return new NavigationSnapshot(
            Profile,
            world.Size,
            world.ChunkSize,
            world.Version,
            Version,
            LinkVersion,
            _chunks.Values.ToArray(),
            regionsByCell,
            regions,
            _links);
    }

    private HashSet<CellId> GetAllowedLinkEndpoints(TraversalLink[] links)
    {
        HashSet<CellId> endpoints = new HashSet<CellId>();
        foreach (TraversalLink link in links)
        {
            if (Profile.Allows(link.Kind))
            {
                endpoints.Add(link.From);
                endpoints.Add(link.To);
            }
        }

        return endpoints;
    }

    private static void AddLinkEndpointChunks(
        HashSet<ChunkId> chunks,
        ChunkLayout layout,
        IEnumerable<TraversalLink> links)
    {
        foreach (TraversalLink link in links)
        {
            chunks.Add(layout.GetChunk(link.From));
            chunks.Add(layout.GetChunk(link.To));
        }
    }

    private static Result<TraversalLink[]> NormalizeLinks(
        WorldSize worldSize,
        IEnumerable<TraversalLink> links)
    {
        Dictionary<string, TraversalLink> unique =
            new Dictionary<string, TraversalLink>(StringComparer.Ordinal);
        foreach (TraversalLink link in links)
        {
            if (!worldSize.Contains(link.From) || !worldSize.Contains(link.To))
            {
                return Result<TraversalLink[]>.Failure(
                    NavigationErrors.LinkOutOfBounds);
            }

            if (!unique.TryAdd(link.Id, link))
            {
                return Result<TraversalLink[]>.Failure(
                    NavigationErrors.DuplicateLink);
            }
        }

        return Result<TraversalLink[]>.Success(
            unique.Values.OrderBy(link => link.Id, StringComparer.Ordinal).ToArray());
    }

    private static bool LinksEqual(
        IReadOnlyList<TraversalLink> left,
        IReadOnlyList<TraversalLink> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (int index = 0; index < left.Count; index++)
        {
            TraversalLink a = left[index];
            TraversalLink b = right[index];
            if (!string.Equals(a.Id, b.Id, StringComparison.Ordinal)
                || a.From != b.From
                || a.To != b.To
                || a.Kind != b.Kind
                || a.Cost != b.Cost
                || a.Bidirectional != b.Bidirectional
                || a.SourceVersion != b.SourceVersion)
            {
                return false;
            }
        }

        return true;
    }
}
}
