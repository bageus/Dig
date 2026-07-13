using System.Collections.ObjectModel;

namespace Dig.Domain.World;

public readonly struct CellSnapshot
{
    public CellSnapshot(
        CellId id,
        CellState state,
        bool isSolid,
        int hardness,
        long worldVersion)
    {
        if (hardness < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hardness));
        }

        if (worldVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(worldVersion));
        }

        Id = id;
        State = state;
        IsSolid = isSolid;
        Hardness = hardness;
        WorldVersion = worldVersion;
    }

    public CellId Id { get; }

    public CellState State { get; }

    public bool IsSolid { get; }

    public int Hardness { get; }

    public long WorldVersion { get; }
}

public sealed class ChunkSnapshot
{
    public ChunkSnapshot(
        ChunkId id,
        CellBounds bounds,
        long worldVersion,
        long chunkVersion,
        IReadOnlyList<CellSnapshot> cells)
    {
        if (worldVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(worldVersion));
        }

        if (chunkVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkVersion));
        }

        if (cells is null)
        {
            throw new ArgumentNullException(nameof(cells));
        }

        Id = id;
        Bounds = bounds;
        WorldVersion = worldVersion;
        ChunkVersion = chunkVersion;
        Cells = new ReadOnlyCollection<CellSnapshot>(cells.ToArray());
    }

    public ChunkId Id { get; }

    public CellBounds Bounds { get; }

    public long WorldVersion { get; }

    public long ChunkVersion { get; }

    public IReadOnlyList<CellSnapshot> Cells { get; }
}

public sealed class WorldSnapshot
{
    public WorldSnapshot(
        WorldSize size,
        int chunkSize,
        long version,
        IReadOnlyList<ChunkSnapshot> chunks)
    {
        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize));
        }

        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (chunks is null)
        {
            throw new ArgumentNullException(nameof(chunks));
        }

        Size = size;
        ChunkSize = chunkSize;
        Version = version;
        Chunks = new ReadOnlyCollection<ChunkSnapshot>(chunks.ToArray());
    }

    public WorldSize Size { get; }

    public int ChunkSize { get; }

    public long Version { get; }

    public IReadOnlyList<ChunkSnapshot> Chunks { get; }
}
