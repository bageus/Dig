using System.Collections.ObjectModel;
using Dig.Domain.World;

namespace Dig.Domain.Navigation;

public readonly struct NavigationChunkStamp : IEquatable<NavigationChunkStamp>
{
    public NavigationChunkStamp(
        ChunkId chunkId,
        long sourceChunkVersion,
        long navigationChunkVersion)
    {
        if (sourceChunkVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceChunkVersion));
        }

        if (navigationChunkVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(navigationChunkVersion));
        }

        ChunkId = chunkId;
        SourceChunkVersion = sourceChunkVersion;
        NavigationChunkVersion = navigationChunkVersion;
    }

    public ChunkId ChunkId { get; }

    public long SourceChunkVersion { get; }

    public long NavigationChunkVersion { get; }

    public bool Equals(NavigationChunkStamp other)
    {
        return ChunkId == other.ChunkId
            && SourceChunkVersion == other.SourceChunkVersion
            && NavigationChunkVersion == other.NavigationChunkVersion;
    }

    public override bool Equals(object? obj)
    {
        return obj is NavigationChunkStamp other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            ChunkId,
            SourceChunkVersion,
            NavigationChunkVersion);
    }

    public static bool operator ==(
        NavigationChunkStamp left,
        NavigationChunkStamp right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        NavigationChunkStamp left,
        NavigationChunkStamp right)
    {
        return !left.Equals(right);
    }
}

public sealed class NavigationChunkSnapshot
{
    private readonly HashSet<CellId> _walkableCells;

    public NavigationChunkSnapshot(
        ChunkId id,
        CellBounds bounds,
        long sourceChunkVersion,
        long navigationChunkVersion,
        IReadOnlyCollection<CellId> walkableCells)
    {
        if (sourceChunkVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceChunkVersion));
        }

        if (navigationChunkVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(navigationChunkVersion));
        }

        if (walkableCells is null)
        {
            throw new ArgumentNullException(nameof(walkableCells));
        }

        CellId[] ordered = walkableCells.OrderBy(cell => cell).ToArray();
        Id = id;
        Bounds = bounds;
        SourceChunkVersion = sourceChunkVersion;
        NavigationChunkVersion = navigationChunkVersion;
        WalkableCells = new ReadOnlyCollection<CellId>(ordered);
        _walkableCells = new HashSet<CellId>(ordered);
    }

    public ChunkId Id { get; }

    public CellBounds Bounds { get; }

    public long SourceChunkVersion { get; }

    public long NavigationChunkVersion { get; }

    public IReadOnlyList<CellId> WalkableCells { get; }

    public NavigationChunkStamp Stamp => new NavigationChunkStamp(
        Id,
        SourceChunkVersion,
        NavigationChunkVersion);

    public bool Contains(CellId cellId)
    {
        return _walkableCells.Contains(cellId);
    }
}

public sealed class NavigationUpdateDiagnostics
{
    public NavigationUpdateDiagnostics(
        bool fullRebuild,
        long previousNavigationVersion,
        long navigationVersion,
        long worldVersion,
        int extractedCellCount,
        int regionCount,
        IReadOnlyCollection<ChunkId> rebuiltChunks)
    {
        if (previousNavigationVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(previousNavigationVersion));
        }

        if (navigationVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(navigationVersion));
        }

        if (worldVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(worldVersion));
        }

        if (extractedCellCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(extractedCellCount));
        }

        if (regionCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(regionCount));
        }

        if (rebuiltChunks is null)
        {
            throw new ArgumentNullException(nameof(rebuiltChunks));
        }

        ChunkId[] ordered = rebuiltChunks.OrderBy(chunk => chunk).ToArray();
        FullRebuild = fullRebuild;
        PreviousNavigationVersion = previousNavigationVersion;
        NavigationVersion = navigationVersion;
        WorldVersion = worldVersion;
        ExtractedCellCount = extractedCellCount;
        RegionCount = regionCount;
        RebuiltChunks = new ReadOnlyCollection<ChunkId>(ordered);
    }

    public bool FullRebuild { get; }

    public long PreviousNavigationVersion { get; }

    public long NavigationVersion { get; }

    public long WorldVersion { get; }

    public int ExtractedCellCount { get; }

    public int RegionCount { get; }

    public IReadOnlyList<ChunkId> RebuiltChunks { get; }
}
