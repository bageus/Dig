using Dig.Presentation.Buildings;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigBuildingBoxGhostRenderer
    {
        private DigRepresentativeBuildingPrefabLibrary? _representatives;

        private void InitializeRepresentatives()
        {
            _representatives = DigRepresentativeBuildingPrefabLibrary.Acquire();
        }

        private void DisposeRepresentatives()
        {
            _representatives?.Dispose();
            _representatives = null;
        }

        private DigBuildingVisualResolution Resolve(BuildingBoxGhostViewModel preview)
        {
            string stableId = preview.DefinitionId.ToString();
            DigBuildingVisualResolution catalogResolution = default;
            bool hasCatalogResolution = visualCatalog != null;
            if (visualCatalog != null)
            {
                catalogResolution = visualCatalog.ResolveBuilding(
                    stableId,
                    BuildingVisualState.Completed);
                if (catalogResolution.HasProfile)
                {
                    return catalogResolution;
                }
            }

            if (_representatives != null
                && _representatives.TryResolve(
                    stableId,
                    BuildingVisualState.Completed,
                    out DigBuildingVisualResolution representative))
            {
                return representative;
            }

            if (hasCatalogResolution)
            {
                return catalogResolution;
            }

            // The confirmed delivery site is projected separately as
            // BuildingVisualState.BuildingBox until the dwarf arrives.
            return new DigBuildingVisualResolution(
                DigVisualAsset.CreateRuntimeFallback(stableId, Color.white),
                Vector2Int.one,
                Vector2.zero,
                hasProfile: false);
        }
    }
}
