using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldRenderer
    {
        private const string TerrainCatalogResourcePath =
            "Dig/VisualCatalogs/Terrain";

        [SerializeField]
        private DigTerrainVisualCatalog? terrainVisualCatalog;

        private DigTerrainChunkRenderer? _terrainChunkRenderer;
        private bool _tunnelDigInteractionActive;

        public void SetVisualCatalog(DigTerrainVisualCatalog? catalog)
        {
            terrainVisualCatalog = catalog;
            _terrainChunkRenderer?.Invalidate();
            DigVisualCatalogDiagnostics.LogValidation(
                terrainVisualCatalog,
                this,
                "Terrain");
        }

        internal void SetTunnelDigInteractionActive(bool active)
        {
            _tunnelDigInteractionActive = active;
            ApplyCellProxyState();
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
            ApplyCellProxyState();
            if (_cells.Count == 0 || _chunks.Count == 0)
            {
                return;
            }

            EnsureTerrainChunkRenderer().Render(
                _cells,
                _chunks,
                _solidCells,
                _tunnelCutaway,
                _protectedCells,
                terrainVisualCatalog);
        }

        private DigTerrainChunkRenderer EnsureTerrainChunkRenderer()
        {
            if (_terrainChunkRenderer != null)
            {
                return _terrainChunkRenderer;
            }

            _terrainChunkRenderer = GetComponent<DigTerrainChunkRenderer>();
            if (_terrainChunkRenderer == null)
            {
                _terrainChunkRenderer = gameObject.AddComponent<DigTerrainChunkRenderer>();
            }

            return _terrainChunkRenderer;
        }

        private void ApplyCellProxyState()
        {
            foreach (DigCellVisual visual in _cells.Values)
            {
                Renderer? renderer = visual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.enabled = _tunnelDigInteractionActive
                        && object.ReferenceEquals(visual, _rejectedCell);
                }

                Collider? collider = visual.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = _tunnelDigInteractionActive
                        && visual.gameObject.activeSelf;
                }
            }
        }
    }
}
