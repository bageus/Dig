using System;

namespace Dig.Unity
{
    internal readonly struct DigTerrainCellKey : IEquatable<DigTerrainCellKey>
    {
        internal DigTerrainCellKey(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        internal int X { get; }
        internal int Y { get; }
        internal int Z { get; }

        internal DigTerrainCellKey Offset(int x, int y, int z)
        {
            return new DigTerrainCellKey(X + x, Y + y, Z + z);
        }

        public bool Equals(DigTerrainCellKey other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object? obj)
        {
            return obj is DigTerrainCellKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = X;
                hash = (hash * 397) ^ Y;
                hash = (hash * 397) ^ Z;
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{X},{Y},{Z}";
        }

        public static bool operator ==(DigTerrainCellKey left, DigTerrainCellKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DigTerrainCellKey left, DigTerrainCellKey right)
        {
            return !left.Equals(right);
        }
    }
}
