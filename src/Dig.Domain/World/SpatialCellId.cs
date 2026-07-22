using System;

namespace Dig.Domain.World
{

public readonly struct SpatialCellId : IEquatable<SpatialCellId>, IComparable<SpatialCellId>
{
    public SpatialCellId(int x, int y, int z)
    {
        WorldDepth.EnsureValid(z, nameof(z));

        X = x;
        Y = y;
        Z = z;
    }

    public int X { get; }

    public int Y { get; }

    public int Z { get; }

    [Obsolete("2D projection is transitional only. Authoritative world state must retain Z.")]
    public CellId Projection => new CellId(X, Y);

    public int CompareTo(SpatialCellId other)
    {
        int yComparison = Y.CompareTo(other.Y);
        if (yComparison != 0)
        {
            return yComparison;
        }

        int zComparison = Z.CompareTo(other.Z);
        return zComparison != 0 ? zComparison : X.CompareTo(other.X);
    }

    public bool Equals(SpatialCellId other)
    {
        return X == other.X && Y == other.Y && Z == other.Z;
    }

    public override bool Equals(object? obj)
    {
        return obj is SpatialCellId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }

    public override string ToString()
    {
        return $"{X},{Y},{Z}";
    }

    public static bool operator ==(SpatialCellId left, SpatialCellId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SpatialCellId left, SpatialCellId right)
    {
        return !left.Equals(right);
    }
}

}
