using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Navigation
{

internal sealed class NavigationWorldIndex
{
    private readonly Dictionary<ChunkId, ChunkSnapshot> _chunks;
    private readonly Dictionary<CellId, CellSnapshot> _cells;

    private NavigationWorldIndex(
        WorldSnapshot snapshot,
        ChunkLayout layout,
        Dictionary<ChunkId, ChunkSnapshot> chunks,
        Dictionary<CellId, CellSnapshot> cells)
    {
        Snapshot = snapshot;
        Layout = layout;
        _chunks = chunks;
        _cells = cells;
    }

    public WorldSnapshot Snapshot { get; }

    public ChunkLayout Layout { get; }

    public static Result<NavigationWorldIndex> Create(WorldSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        ChunkLayout layout = new ChunkLayout(snapshot.Size, snapshot.ChunkSize);
        Dictionary<ChunkId, ChunkSnapshot> chunks =
            new Dictionary<ChunkId, ChunkSnapshot>();
        Dictionary<CellId, CellSnapshot> cells =
            new Dictionary<CellId, CellSnapshot>();

        foreach (ChunkSnapshot chunk in snapshot.Chunks)
        {
            if (!layout.Contains(chunk.Id) || !chunks.TryAdd(chunk.Id, chunk))
            {
                return Result<NavigationWorldIndex>.Failure(
                    NavigationErrors.MissingWorldChunk);
            }

            foreach (CellSnapshot cell in chunk.Cells)
            {
                if (!snapshot.Size.Contains(cell.Id) || !cells.TryAdd(cell.Id, cell))
                {
                    return Result<NavigationWorldIndex>.Failure(
                        NavigationErrors.MissingWorldChunk);
                }
            }
        }

        if (chunks.Count != layout.ChunkCount
            || cells.Count != snapshot.Size.CellCount)
        {
            return Result<NavigationWorldIndex>.Failure(
                NavigationErrors.MissingWorldChunk);
        }

        return Result<NavigationWorldIndex>.Success(
            new NavigationWorldIndex(snapshot, layout, chunks, cells));
    }

    public ChunkSnapshot GetChunk(ChunkId chunkId)
    {
        return _chunks[chunkId];
    }

    public CellSnapshot GetCell(CellId cellId)
    {
        return _cells[cellId];
    }

    public bool TryGetCell(CellId cellId, out CellSnapshot cell)
    {
        return _cells.TryGetValue(cellId, out cell);
    }
}
}
