using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dig.Unity
{
    [Serializable]
    public sealed class DigTerrainVisualProfile
    {
        [SerializeField]
        private string stableId = string.Empty;

        [SerializeField]
        private DigTerrainProfileKind kind = DigTerrainProfileKind.Stone;

        [SerializeField]
        private Material? floorMaterial;

        [SerializeField]
        private Material? wallMaterial;

        [SerializeField]
        private Material? ceilingMaterial;

        [SerializeField]
        private Material? freshCutMaterial;

        public string StableId => stableId;

        public DigTerrainProfileKind Kind => kind;

        public Material? FloorMaterial => floorMaterial;

        public Material? WallMaterial => wallMaterial;

        public Material? CeilingMaterial => ceilingMaterial;

        public Material? FreshCutMaterial => freshCutMaterial;

        internal Material? Resolve(DigTerrainSurfaceRole role)
        {
            switch (role)
            {
                case DigTerrainSurfaceRole.Floor:
                    return floorMaterial;
                case DigTerrainSurfaceRole.Ceiling:
                    return ceilingMaterial;
                case DigTerrainSurfaceRole.FreshCut:
                    return freshCutMaterial;
                default:
                    return wallMaterial;
            }
        }

        internal void AppendValidation(int index, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                errors.Add($"Terrain profile {index} has no stable id.");
            }

            ValidateMaterial(floorMaterial, nameof(FloorMaterial), errors);
            ValidateMaterial(wallMaterial, nameof(WallMaterial), errors);
            ValidateMaterial(ceilingMaterial, nameof(CeilingMaterial), errors);
            ValidateMaterial(freshCutMaterial, nameof(FreshCutMaterial), errors);
        }

        private void ValidateMaterial(
            Material? material,
            string role,
            ICollection<string> errors)
        {
            if (material == null)
            {
                string id = string.IsNullOrWhiteSpace(stableId)
                    ? "<missing>"
                    : stableId;
                errors.Add($"Terrain profile '{id}' has no {role}.");
            }
        }
    }
}
