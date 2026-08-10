using System;

namespace Dig.Unity
{
    internal readonly struct DigTerrainChunkKey : IEquatable<DigTerrainChunkKey>
    {
        internal DigTerrainChunkKey(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        internal int X { get; }
        internal int Y { get; }
        internal int Z { get; }

        internal DigTerrainChunkKey Offset(int x, int y, int z)
        {
            return new DigTerrainChunkKey(X + x, Y + y, Z + z);
        }

        public bool Equals(DigTerrainChunkKey other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object? obj)
        {
            return obj is DigTerrainChunkKey other && Equals(other);
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

        public static bool operator ==(DigTerrainChunkKey left, DigTerrainChunkKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DigTerrainChunkKey left, DigTerrainChunkKey right)
        {
            return !left.Equals(right);
        }
    }
}
