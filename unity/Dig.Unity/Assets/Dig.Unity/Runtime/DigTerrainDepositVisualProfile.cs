using System;
using System.Collections.Generic;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    public enum DigTerrainDepositProfileKind
    {
        Iron = 0,
        Gold = 1,
        Crystal = 2,
        Coal = 3,
        Stone = 4,
    }

    [Serializable]
    public sealed class DigTerrainDepositVisualProfile
    {
        [SerializeField]
        private string stableId = string.Empty;

        [SerializeField]
        private DigTerrainDepositProfileKind kind;

        [SerializeField]
        private Material? revealedMaterial;

        [SerializeField]
        private Material? damagedMaterial;

        public string StableId => stableId;

        public DigTerrainDepositProfileKind Kind => kind;

        internal Material? Resolve(TerrainDepositVisualState state)
        {
            switch (state)
            {
                case TerrainDepositVisualState.Revealed:
                    return revealedMaterial;
                case TerrainDepositVisualState.Damaged:
                    return damagedMaterial;
                default:
                    return null;
            }
        }

        internal void AppendValidation(int index, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                errors.Add($"Deposit profile {index} has no stable id.");
            }

            if (!Enum.IsDefined(typeof(DigTerrainDepositProfileKind), kind))
            {
                errors.Add($"Deposit profile {index} has an invalid kind.");
            }

            if (revealedMaterial == null)
            {
                errors.Add(
                    $"Deposit profile '{stableId}' has no revealed material.");
            }

            if (damagedMaterial == null)
            {
                errors.Add(
                    $"Deposit profile '{stableId}' has no damaged material.");
            }
        }
    }
}
