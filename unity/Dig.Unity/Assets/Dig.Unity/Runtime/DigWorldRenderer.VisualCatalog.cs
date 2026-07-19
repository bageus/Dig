using System.Collections.Generic;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldRenderer
    {
        private const string TerrainCatalogResourcePath =
            "Dig/VisualCatalogs/Terrain";

        private readonly Dictionary<Vector2Int, long> _catalogAppliedVersions =
            new Dictionary<Vector2Int, long>();
        private readonly HashSet<string> _reportedFallbackIds =
            new HashSet<string>(System.StringComparer.Ordinal);
        private readonly List<Vector2Int> _removedCatalogCells =
            new List<Vector2Int>();

        [SerializeField]
        private DigTerrainVisualCatalog? terrainVisualCatalog;

        public void SetVisualCatalog(DigTerrainVisualCatalog? catalog)
        {
            terrainVisualCatalog = catalog;
            _catalogAppliedVersions.Clear();
            _reportedFallbackIds.Clear();
            DigVisualCatalogDiagnostics.LogValidation(
                terrainVisualCatalog,
                this,
                "Terrain");
        }

        private void Awake()
        {
            if (terrainVisualCatalog == null)
            {
                terrainVisualCatalog = Resources.Load<DigTerrainVisualCatalog>(
                    TerrainCatalogResourcePath);
            }

            DigVisualCatalogDiagnostics.LogValidation(
                terrainVisualCatalog,
                this,
                "Terrain");
        }

        private void LateUpdate()
        {
            if (terrainVisualCatalog == null || _cells.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<Vector2Int, DigCellVisual> pair in _cells)
            {
                WorldCellViewModel model = pair.Value.Model;
                if (_catalogAppliedVersions.TryGetValue(
                        pair.Key,
                        out long appliedVersion)
                    && appliedVersion == model.WorldVersion)
                {
                    continue;
                }

                DigVisualAsset asset = terrainVisualCatalog.Resolve(model.MaterialId);
                ApplyCatalogMaterial(pair.Value, asset);
                _catalogAppliedVersions[pair.Key] = model.WorldVersion;
                if (asset.IsFallback && _reportedFallbackIds.Add(model.MaterialId))
                {
                    Debug.LogWarning(
                        $"Terrain visual '{model.MaterialId}' uses catalog fallback.",
                        this);
                }
            }

            RemoveMissingCatalogState();
        }

        private static void ApplyCatalogMaterial(
            DigCellVisual visual,
            DigVisualAsset asset)
        {
            if (asset.Material == null)
            {
                return;
            }

            DigVisualPrefabRoot? root = visual.GetComponent<DigVisualPrefabRoot>();
            Renderer[] renderers = root == null
                ? visual.GetComponentsInChildren<Renderer>(includeInactive: true)
                : root.ResolveTintRenderers();
            for (int index = 0; index < renderers.Length; index++)
            {
                renderers[index].sharedMaterial = asset.Material;
            }
        }

        private void RemoveMissingCatalogState()
        {
            if (_catalogAppliedVersions.Count == _cells.Count)
            {
                return;
            }

            _removedCatalogCells.Clear();
            foreach (Vector2Int key in _catalogAppliedVersions.Keys)
            {
                if (!_cells.ContainsKey(key))
                {
                    _removedCatalogCells.Add(key);
                }
            }

            for (int index = 0; index < _removedCatalogCells.Count; index++)
            {
                _catalogAppliedVersions.Remove(_removedCatalogCells[index]);
            }
        }
    }
}
