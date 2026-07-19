using System;
using Dig.Presentation.World;

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
            DigTerrainSurfaceRole role,
            byte shade,
            string depositId,
            TerrainDepositVisualState depositState,
            byte depositDamageBand,
            TerrainDepositConnection depositConnections)
        {
            MaterialId = materialId
                ?? throw new ArgumentNullException(nameof(materialId));
            DepositId = depositId
                ?? throw new ArgumentNullException(nameof(depositId));
            State = state;
            Role = role;
            Shade = shade;
            DepositState = depositState;
            DepositDamageBand = depositDamageBand;
            DepositConnections = depositConnections;
        }

        internal string MaterialId { get; }

        internal DigTerrainSurfaceState State { get; }

        internal DigTerrainSurfaceRole Role { get; }

        internal byte Shade { get; }

        internal string DepositId { get; }

        internal TerrainDepositVisualState DepositState { get; }

        internal byte DepositDamageBand { get; }

        internal TerrainDepositConnection DepositConnections { get; }

        internal bool HasVisibleDeposit =>
            !string.IsNullOrWhiteSpace(DepositId)
            && (DepositState == TerrainDepositVisualState.Revealed
                || DepositState == TerrainDepositVisualState.Damaged);

        public bool Equals(DigTerrainMaterialKey other)
        {
            return string.Equals(
                    MaterialId,
                    other.MaterialId,
                    StringComparison.Ordinal)
                && State == other.State
                && Role == other.Role
                && Shade == other.Shade
                && string.Equals(
                    DepositId,
                    other.DepositId,
                    StringComparison.Ordinal)
                && DepositState == other.DepositState
                && DepositDamageBand == other.DepositDamageBand
                && DepositConnections == other.DepositConnections;
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
                hash = (hash * 397) ^ (int)Role;
                hash = (hash * 397) ^ Shade;
                hash = (hash * 397)
                    ^ StringComparer.Ordinal.GetHashCode(DepositId);
                hash = (hash * 397) ^ (int)DepositState;
                hash = (hash * 397) ^ DepositDamageBand;
                return (hash * 397) ^ (int)DepositConnections;
            }
        }

        public override string ToString()
        {
            if (!HasVisibleDeposit)
            {
                return $"{MaterialId}:{State}:{Role}:{Shade}";
            }

            return $"{MaterialId}:{State}:{Role}:{Shade}:"
                + $"{DepositId}:{DepositState}:{DepositDamageBand}:"
                + $"{(byte)DepositConnections}";
        }
    }
}
