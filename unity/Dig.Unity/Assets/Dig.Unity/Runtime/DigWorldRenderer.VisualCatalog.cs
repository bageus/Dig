using System;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldRenderer
    {
        private const string TerrainCatalogResourcePath =
            "Dig/VisualCatalogs/Terrain";

        [SerializeField]
        private DigTerrainVisualCatalog? terrainVisualCatalog;

        private readonly DigTerrainRenderSnapshotBuilder _terrainSnapshotBuilder =
            new DigTerrainRenderSnapshotBuilder();
        private readonly TerrainDepositDecorationPresenter _depositDecorationPresenter =
            new TerrainDepositDecorationPresenter();
        private DigTerrainChunkRenderer? _terrainChunkRenderer;
        private WorldViewModel? _terrainWorld;
        private TerrainDepthVolumeViewModel? _terrainDepthVolume;
        private TerrainDepositDecorationVolumeViewModel? _terrainDepositDecorations;
        private bool _tunnelDigInteractionActive;

        public void SetVisualCatalog(DigTerrainVisualCatalog? catalog)
        {
            terrainVisualCatalog = catalog;
            _terrainChunkRenderer?.Invalidate();
            DigVisualCatalogDiagnostics.LogValidation(
                terrainVisualCatalog,
                this,
                "Terrain");
            RefreshChunkedTerrain();
        }

        internal void SetTerrainDeposits(TerrainDepositVolumeViewModel deposits)
        {
            if (deposits == null)
            {
                throw new ArgumentNullException(nameof(deposits));
            }

            _terrainDepositDecorations = _depositDecorationPresenter.Present(deposits);
            RefreshChunkedTerrain();
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

        private void RefreshChunkedTerrain(WorldViewModel world)
        {
            _terrainWorld = world;
            ApplyCellProxyState();
            DigTerrainRenderSnapshot snapshot = _terrainSnapshotBuilder.Build(
                world,
                _terrainDepthVolume,
                _terrainDepositDecorations,
                _tunnelCutaway,
                _protectedCells);
            EnsureTerrainChunkRenderer().Render(snapshot, terrainVisualCatalog);
        }

        private void RefreshChunkedTerrain()
        {
            if (_terrainWorld != null)
            {
                RefreshChunkedTerrain(_terrainWorld);
            }
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
