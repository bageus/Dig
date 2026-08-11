using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Domain.Exploration
{
public sealed class ExplorationState
{
    private static readonly (int X, int Y, int Z)[] Directions =
        CreateDirections();
    private static readonly (int X, int Y, int Z)[] BoundaryDirections = Directions;
    private readonly HashSet<CellId> _explored = new HashSet<CellId>();
    private readonly HashSet<CellId> _visible = new HashSet<CellId>();
    private readonly Dictionary<EntityId, LastKnownWorldItemMarker> _markers =
        new Dictionary<EntityId, LastKnownWorldItemMarker>();
    private readonly HashSet<ChunkId> _dirtyChunks = new HashSet<ChunkId>();

    public long Version { get; private set; }
    public IReadOnlyCollection<CellId> Explored => ReadCells(_explored);
    public IReadOnlyCollection<CellId> Visible => ReadCells(_visible);
    public IReadOnlyCollection<LastKnownWorldItemMarker> Markers =>
        new ReadOnlyCollection<LastKnownWorldItemMarker>(_markers.Values
            .OrderBy(value => value.StackId.ToString(), StringComparer.Ordinal).ToArray());

    public CellVisibility GetVisibility(CellId cell) => _visible.Contains(cell)
        ? CellVisibility.Visible
        : _explored.Contains(cell) ? CellVisibility.ExploredNotVisible : CellVisibility.Unexplored;
    public bool IsVisible(CellId cell) => _visible.Contains(cell);

    public bool Recalculate(
        WorldSnapshot world,
        IEnumerable<VisionSourceSnapshot> sources,
        ISet<CellId>? closedDoors = null,
        ISet<CellId>? additionalBlockers = null)
    {
        if (world is null) throw new ArgumentNullException(nameof(world));
        if (sources is null) throw new ArgumentNullException(nameof(sources));
        Dictionary<CellId, CellSnapshot> cells = world.Chunks.SelectMany(c => c.Cells)
            .ToDictionary(cell => cell.Id);
        HashSet<CellId> next = new HashSet<CellId>();
        foreach (VisionSourceSnapshot source in sources.OrderBy(value => value.Id, StringComparer.Ordinal))
            foreach (CellId origin in source.Origins)
                Flood(origin, source.Radius, world.Size, cells, closedDoors, additionalBlockers, next);
        bool changed = !_visible.SetEquals(next);
        if (!changed) return false;
        foreach (CellId cell in _visible.Concat(next).Distinct()
            .Where(cell => _visible.Contains(cell) != next.Contains(cell)))
            _dirtyChunks.Add(ToChunk(cell, world.ChunkSize));
        _visible.Clear(); _visible.UnionWith(next); _explored.UnionWith(next); Version++;
        return true;
    }

    public void ObserveItems(IEnumerable<ItemStackSnapshot> items, long tick)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        bool changed = false;
        foreach (ItemStackSnapshot item in items.OrderBy(value => value.StackId.ToString(), StringComparer.Ordinal))
        {
            if (!item.Location.HasCell || !_visible.Contains(item.Location.CellId)) continue;
            LastKnownWorldItemMarker marker = new LastKnownWorldItemMarker(
                item.StackId, item.ItemId, item.Location.CellId, tick);
            changed |= !_markers.TryGetValue(item.StackId, out LastKnownWorldItemMarker old)
                || old.Cell != marker.Cell || old.ObservedTick != tick;
            _markers[item.StackId] = marker;
        }
        if (changed) Version++;
    }

    public void ObserveMarkers(IEnumerable<LastKnownWorldItemMarker> markers)
    {
        if (markers is null) throw new ArgumentNullException(nameof(markers));
        bool changed = false;
        foreach (LastKnownWorldItemMarker marker in markers
            .OrderBy(value => value.StackId.ToString(), StringComparer.Ordinal))
        {
            if (!_visible.Contains(marker.Cell)) continue;
            changed |= !_markers.TryGetValue(marker.StackId, out LastKnownWorldItemMarker old)
                || old.Cell != marker.Cell || old.ItemId != marker.ItemId
                || old.ObservedTick != marker.ObservedTick;
            _markers[marker.StackId] = marker;
        }
        if (changed) Version++;
    }

    public IReadOnlyList<ChunkId> DrainDirtyChunks()
    {
        ChunkId[] dirty = _dirtyChunks.OrderBy(value => value).ToArray();
        _dirtyChunks.Clear();
        return new ReadOnlyCollection<ChunkId>(dirty);
    }

    public ExplorationSaveSnapshot CreateSaveSnapshot() =>
        new ExplorationSaveSnapshot(1, _explored, _markers.Values);

    public static ExplorationState Restore(ExplorationSaveSnapshot snapshot)
    {
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));
        ExplorationState state = new ExplorationState();
        state._explored.UnionWith(snapshot.Explored);
        foreach (LastKnownWorldItemMarker marker in snapshot.Markers) state._markers.Add(marker.StackId, marker);
        return state;
    }

    private static void Flood(
        CellId origin, int radius, WorldSize size, IReadOnlyDictionary<CellId, CellSnapshot> cells,
        ISet<CellId>? doors, ISet<CellId>? blockers, HashSet<CellId> result)
    {
        if (!size.Contains(origin)) return;
        Queue<(CellId Cell, int Distance)> queue = new Queue<(CellId, int)>();
        HashSet<CellId> visited = new HashSet<CellId> { origin };
        queue.Enqueue((origin, 0));
        while (queue.Count > 0)
        {
            (CellId cell, int distance) = queue.Dequeue();
            result.Add(cell);
            bool blocked = IsBlocked(cell, origin, cells, doors, blockers);
            if (!blocked) RevealBoundary(cell, size, cells, doors, blockers, result);
            if (distance == radius || blocked) continue;
            foreach ((int x, int y, int z) in Directions)
            {
                CellId next = new CellId(cell.X + x, cell.Y + y, cell.Z + z);
                if (size.Contains(next) && visited.Add(next)) queue.Enqueue((next, distance + 1));
            }
        }
    }

    private static bool IsBlocked(
        CellId cell, CellId origin, IReadOnlyDictionary<CellId, CellSnapshot> cells,
        ISet<CellId>? doors, ISet<CellId>? blockers) => cell != origin
        && (doors?.Contains(cell) == true || blockers?.Contains(cell) == true
            || !cells.TryGetValue(cell, out CellSnapshot value) || value.IsSolid);

    private static void RevealBoundary(
        CellId cell, WorldSize size, IReadOnlyDictionary<CellId, CellSnapshot> cells,
        ISet<CellId>? doors, ISet<CellId>? blockers, HashSet<CellId> result)
    {
        foreach ((int x, int y, int z) in BoundaryDirections)
        {
            CellId boundary = new CellId(cell.X + x, cell.Y + y, cell.Z + z);
            if (size.Contains(boundary)
                && IsBlocked(boundary, cell, cells, doors, blockers))
                result.Add(boundary);
        }
    }

    private static (int X, int Y, int Z)[] CreateDirections()
    {
        List<(int X, int Y, int Z)> directions = new List<(int, int, int)>();
        for (int z = -1; z <= 1; z++)
        for (int y = -1; y <= 1; y++)
        for (int x = -1; x <= 1; x++)
            if (x != 0 || y != 0 || z != 0) directions.Add((x, y, z));
        return directions.ToArray();
    }

    private static IReadOnlyCollection<CellId> ReadCells(IEnumerable<CellId> cells) =>
        new ReadOnlyCollection<CellId>(cells.OrderBy(value => value).ToArray());

    private static ChunkId ToChunk(CellId cell, int chunkSize) =>
        new ChunkId(cell.X / chunkSize, cell.Y / chunkSize, cell.Z);
}

public sealed class ExplorationSaveSnapshot
{
    public ExplorationSaveSnapshot(
        int schemaVersion, IEnumerable<CellId> explored,
        IEnumerable<LastKnownWorldItemMarker> markers)
    {
        if (schemaVersion != 1) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        SchemaVersion = schemaVersion;
        Explored = new ReadOnlyCollection<CellId>(explored.Distinct().OrderBy(value => value).ToArray());
        Markers = new ReadOnlyCollection<LastKnownWorldItemMarker>(markers
            .OrderBy(value => value.StackId.ToString(), StringComparer.Ordinal).ToArray());
    }
    public int SchemaVersion { get; }
    public IReadOnlyList<CellId> Explored { get; }
    public IReadOnlyList<LastKnownWorldItemMarker> Markers { get; }
}
}
