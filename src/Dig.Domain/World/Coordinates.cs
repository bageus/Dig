using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace Dig.Domain.World
{

public readonly struct CellId : IEquatable<CellId>, IComparable<CellId>
{
    public CellId(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }

    public int CompareTo(CellId other)
    {
        int yComparison = Y.CompareTo(other.Y);
        return yComparison != 0 ? yComparison : X.CompareTo(other.X);
    }

    public bool Equals(CellId other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        return obj is CellId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public override string ToString()
    {
        return $"{X},{Y}";
    }

    public static bool operator ==(CellId left, CellId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CellId left, CellId right)
    {
        return !left.Equals(right);
    }
}

public readonly struct ChunkId : IEquatable<ChunkId>, IComparable<ChunkId>
{
    public ChunkId(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }

    public int CompareTo(ChunkId other)
    {
        int yComparison = Y.CompareTo(other.Y);
        return yComparison != 0 ? yComparison : X.CompareTo(other.X);
    }

    public bool Equals(ChunkId other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        return obj is ChunkId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public override string ToString()
    {
        return $"{X},{Y}";
    }

    public static bool operator ==(ChunkId left, ChunkId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ChunkId left, ChunkId right)
    {
        return !left.Equals(right);
    }
}

public readonly struct WorldSize
{
    public WorldSize(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }

    public bool Contains(CellId cellId)
    {
        return cellId.X >= 0
            && cellId.Y >= 0
            && cellId.X < Width
            && cellId.Y < Height;
    }
}

public readonly struct CellBounds
{
    public CellBounds(int minX, int minY, int maxXExclusive, int maxYExclusive)
    {
        MinX = minX;
        MinY = minY;
        MaxXExclusive = maxXExclusive;
        MaxYExclusive = maxYExclusive;
    }

    public int MinX { get; }

    public int MinY { get; }

    public int MaxXExclusive { get; }

    public int MaxYExclusive { get; }
}

public sealed class ChunkLayout
{
    public ChunkLayout(WorldSize worldSize, int chunkSize)
    {
        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize));
        }

        WorldSize = worldSize;
        ChunkSize = chunkSize;
        ChunkCountX = DivideRoundUp(worldSize.Width, chunkSize);
        ChunkCountY = DivideRoundUp(worldSize.Height, chunkSize);
    }

    public WorldSize WorldSize { get; }

    public int ChunkSize { get; }

    public int ChunkCountX { get; }

    public int ChunkCountY { get; }

    public int ChunkCount => checked(ChunkCountX * ChunkCountY);

    public bool Contains(ChunkId chunkId)
    {
        return chunkId.X >= 0
            && chunkId.Y >= 0
            && chunkId.X < ChunkCountX
            && chunkId.Y < ChunkCountY;
    }

    public ChunkId GetChunk(CellId cellId)
    {
        if (!WorldSize.Contains(cellId))
        {
            throw new ArgumentOutOfRangeException(nameof(cellId));
        }

        return new ChunkId(cellId.X / ChunkSize, cellId.Y / ChunkSize);
    }

    public CellBounds GetBounds(ChunkId chunkId)
    {
        if (!Contains(chunkId))
        {
            throw new ArgumentOutOfRangeException(nameof(chunkId));
        }

        int minX = chunkId.X * ChunkSize;
        int minY = chunkId.Y * ChunkSize;
        return new CellBounds(
            minX,
            minY,
            Math.Min(minX + ChunkSize, WorldSize.Width),
            Math.Min(minY + ChunkSize, WorldSize.Height));
    }

    public IReadOnlyList<ChunkId> GetAffectedChunks(CellId cellId)
    {
        ChunkId owner = GetChunk(cellId);
        CellBounds bounds = GetBounds(owner);
        List<int> xCoordinates = new List<int> { owner.X };
        List<int> yCoordinates = new List<int> { owner.Y };

        if (cellId.X == bounds.MinX && owner.X > 0)
        {
            xCoordinates.Add(owner.X - 1);
        }

        if (cellId.X == bounds.MaxXExclusive - 1 && owner.X + 1 < ChunkCountX)
        {
            xCoordinates.Add(owner.X + 1);
        }

        if (cellId.Y == bounds.MinY && owner.Y > 0)
        {
            yCoordinates.Add(owner.Y - 1);
        }

        if (cellId.Y == bounds.MaxYExclusive - 1 && owner.Y + 1 < ChunkCountY)
        {
            yCoordinates.Add(owner.Y + 1);
        }

        List<ChunkId> affected = new List<ChunkId>();
        foreach (int y in yCoordinates)
        {
            foreach (int x in xCoordinates)
            {
                affected.Add(new ChunkId(x, y));
            }
        }

        affected.Sort();
        return new ReadOnlyCollection<ChunkId>(affected);
    }

    private static int DivideRoundUp(int value, int divisor)
    {
        return checked((value + divisor - 1) / divisor);
    }
}
}
