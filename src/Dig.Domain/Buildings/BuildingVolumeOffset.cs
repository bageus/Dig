using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Domain.Buildings
{

public readonly struct BuildingVolumeOffset
    : IEquatable<BuildingVolumeOffset>, IComparable<BuildingVolumeOffset>
{
    public BuildingVolumeOffset(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public int X { get; }

    public int Y { get; }

    public int Z { get; }

    public BuildingVolumeOffset Rotate(BuildingOrientation orientation)
    {
        return orientation switch
        {
            BuildingOrientation.North => this,
            BuildingOrientation.East => new BuildingVolumeOffset(-Z, Y, X),
            BuildingOrientation.South => new BuildingVolumeOffset(-X, Y, -Z),
            BuildingOrientation.West => new BuildingVolumeOffset(Z, Y, -X),
            _ => throw new ArgumentOutOfRangeException(nameof(orientation)),
        };
    }

    public CellId Apply(CellId origin)
    {
        return new CellId(
            checked(origin.X + X),
            checked(origin.Y + Y),
            checked(origin.Z + Z));
    }

    public int CompareTo(BuildingVolumeOffset other)
    {
        int zComparison = Z.CompareTo(other.Z);
        if (zComparison != 0) return zComparison;
        int yComparison = Y.CompareTo(other.Y);
        return yComparison != 0 ? yComparison : X.CompareTo(other.X);
    }

    public bool Equals(BuildingVolumeOffset other) =>
        X == other.X && Y == other.Y && Z == other.Z;

    public override bool Equals(object? obj) =>
        obj is BuildingVolumeOffset other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
}

internal static class BuildingVolumeOffsets
{
    internal static BuildingVolumeOffset[] Normalize(
        IEnumerable<BuildingVolumeOffset> values)
    {
        BuildingVolumeOffset[] result = values
            .Distinct()
            .OrderBy(value => value)
            .ToArray();
        if (result.Length == 0
            || !result.Contains(new BuildingVolumeOffset(0, 0, 0)))
        {
            throw new ArgumentException(
                "Occupied building volume must contain its origin.",
                nameof(values));
        }

        return result;
    }
}

}
