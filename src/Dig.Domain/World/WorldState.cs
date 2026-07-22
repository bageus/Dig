using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Domain.Core;

namespace Dig.Domain.World
{

public sealed partial class WorldState : AggregateRoot
{
    private readonly CellState[] _cells;
    private readonly long[] _chunkVersions;
    private readonly HashSet<ChunkId> _dirtyChunks;

    private WorldState(
        WorldSize size,
        ChunkLayout layout,
        MaterialCatalog materials,
        CellState[] cells)
    {
        Size = size;
        Layout = layout;
        Materials = materials;
        _cells = cells;
        _chunkVersions = new long[layout.ChunkCount];
        _dirtyChunks = new HashSet<ChunkId>();
    }

    public WorldSize Size { get; }

    public ChunkLayout Layout { get; }

    public MaterialCatalog Materials { get; }

    public long Version { get; private set; }

    public static Result<WorldState> CreateFilled(
        WorldSize size,
        int chunkSize,
        MaterialCatalog materials,
        MaterialId fillMaterialId,
        bool explored = false,
        short temperature = 20)
    {
        if (materials is null)
        {
            throw new ArgumentNullException(nameof(materials));
        }

        if (!materials.Contains(fillMaterialId))
        {
            return Result<WorldState>.Failure(WorldErrors.UnknownMaterial);
        }

        ChunkLayout layout = new ChunkLayout(size, chunkSize);
        int cellCount = size.CellCount;
        CellState initialState = new CellState(
            fillMaterialId,
            CellDesignation.None,
            explored,
            damage: 0,
            temperature);
        CellState[] cells = Enumerable.Repeat(initialState, cellCount).ToArray();

        return Result<WorldState>.Success(
            new WorldState(size, layout, materials, cells));
    }

    public Result<CellSnapshot> GetCell(CellId cellId)
    {
        if (!Size.Contains(cellId))
        {
            return Result<CellSnapshot>.Failure(WorldErrors.CellOutOfBounds);
        }

        return Result<CellSnapshot>.Success(CreateCellSnapshot(cellId));
    }

    public Result<long> GetChunkVersion(ChunkId chunkId)
    {
        if (!Layout.Contains(chunkId))
        {
            return Result<long>.Failure(WorldErrors.ChunkOutOfBounds);
        }

        return Result<long>.Success(_chunkVersions[GetChunkIndex(chunkId)]);
    }

    public Result<ChunkSnapshot> CreateChunkSnapshot(ChunkId chunkId)
    {
        if (!Layout.Contains(chunkId))
        {
            return Result<ChunkSnapshot>.Failure(WorldErrors.ChunkOutOfBounds);
        }

        CellBounds bounds = Layout.GetBounds(chunkId);
        List<CellSnapshot> cells = new List<CellSnapshot>();
        for (int z = bounds.MinZ; z < bounds.MaxZExclusive; z++)
        {
            for (int y = bounds.MinY; y < bounds.MaxYExclusive; y++)
            {
                for (int x = bounds.MinX; x < bounds.MaxXExclusive; x++)
                {
                    cells.Add(CreateCellSnapshot(new CellId(x, y, z)));
                }
            }
        }

        ChunkSnapshot snapshot = new ChunkSnapshot(
            chunkId,
            bounds,
            Version,
            _chunkVersions[GetChunkIndex(chunkId)],
            cells);
        return Result<ChunkSnapshot>.Success(snapshot);
    }

    public WorldSnapshot CreateSnapshot()
    {
        List<ChunkSnapshot> chunks = new List<ChunkSnapshot>(Layout.ChunkCount);
        foreach (ChunkId chunkId in GetAllChunkIds())
        {
            chunks.Add(CreateChunkSnapshot(chunkId).Value);
        }

        return new WorldSnapshot(Size, Layout.ChunkSize, Version, chunks);
    }

    public IReadOnlyList<ChunkId> PeekDirtyChunks()
    {
        return CreateReadOnlyChunks(_dirtyChunks);
    }

    public IReadOnlyList<ChunkId> DrainDirtyChunks()
    {
        IReadOnlyList<ChunkId> dirty = CreateReadOnlyChunks(_dirtyChunks);
        _dirtyChunks.Clear();
        return dirty;
    }

    private CellSnapshot CreateCellSnapshot(CellId cellId)
    {
        CellState state = _cells[GetCellIndex(cellId)];
        MaterialDefinition material = Materials.Get(state.MaterialId)!;
        return new CellSnapshot(
            cellId,
            state,
            material.IsSolid,
            material.Hardness,
            Version);
    }

    private IReadOnlyList<ChunkId> GetAllChunkIds()
    {
        List<ChunkId> chunks = new List<ChunkId>(Layout.ChunkCount);
        for (int z = 0; z < Layout.ChunkCountZ; z++)
        {
            for (int y = 0; y < Layout.ChunkCountY; y++)
            {
                for (int x = 0; x < Layout.ChunkCountX; x++)
                {
                    chunks.Add(new ChunkId(x, y, z));
                }
            }
        }

        return new ReadOnlyCollection<ChunkId>(chunks);
    }

    private int GetCellIndex(CellId cellId)
    {
        return checked((((cellId.Z * Size.Height) + cellId.Y) * Size.Width) + cellId.X);
    }

    private int GetChunkIndex(ChunkId chunkId)
    {
        return checked((((chunkId.Z * Layout.ChunkCountY) + chunkId.Y) * Layout.ChunkCountX) + chunkId.X);
    }

    private static IReadOnlyList<ChunkId> CreateReadOnlyChunks(
        IEnumerable<ChunkId> chunks)
    {
        ChunkId[] ordered = chunks.OrderBy(chunk => chunk).ToArray();
        return new ReadOnlyCollection<ChunkId>(ordered);
    }
}
}
