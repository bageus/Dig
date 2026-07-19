using System;

namespace Dig.Unity
{
    internal enum DigTerrainSurfaceState
    {
        Solid = 0,
        Unexplored = 1,
        Designated = 2,
        Protected = 3,
    }

    internal readonly struct DigTerrainMaterialKey :
        IEquatable<DigTerrainMaterialKey>
    {
        internal DigTerrainMaterialKey(
            string materialId,
            DigTerrainSurfaceState state,
            byte shade)
        {
            MaterialId = materialId ?? throw new ArgumentNullException(nameof(materialId));
            State = state;
            Shade = shade;
        }

        internal string MaterialId { get; }
        internal DigTerrainSurfaceState State { get; }
        internal byte Shade { get; }

        public bool Equals(DigTerrainMaterialKey other)
        {
            return string.Equals(MaterialId, other.MaterialId, StringComparison.Ordinal)
                && State == other.State
                && Shade == other.Shade;
        }

        public override bool Equals(object? obj)
        {
            return obj is DigTerrainMaterialKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StringComparer.Ordinal.GetHashCode(MaterialId);
                hash = (hash * 397) ^ (int)State;
                return (hash * 397) ^ Shade;
            }
        }

        public override string ToString()
        {
            return $"{MaterialId}:{State}:{Shade}";
        }
    }
}
