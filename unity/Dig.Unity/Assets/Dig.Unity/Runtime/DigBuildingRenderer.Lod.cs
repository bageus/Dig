using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigBuildingRenderer
    {
        private readonly TerrainVisualDetailPolicy _buildingDetailPolicy =
            new TerrainVisualDetailPolicy();
        private Camera? _buildingLodCamera;

        internal TerrainVisualDetailLevel BuildingDetailLevel =>
            _buildingDetailLevel;

        internal float BuildingPixelsPerCell { get; private set; }

        internal int BuildingVisibleRendererCount { get; private set; }

        internal int BuildingVisibleTriangleCount { get; private set; }

        internal int BuildingRebuildCount { get; private set; }

        private void LateUpdate()
        {
            if (_buildingLodCamera == null)
            {
                _buildingLodCamera = Camera.main;
            }

            if (_buildingLodCamera == null)
            {
                return;
            }

            BuildingPixelsPerCell = MeasurePixelsPerCell(
                _buildingLodCamera,
                transform);
            TerrainVisualDetailLevel next = _buildingDetailPolicy.Resolve(
                BuildingPixelsPerCell,
                _buildingDetailLevel);
            if (next != _buildingDetailLevel)
            {
                _buildingDetailLevel = next;
                foreach (DigBuildingVisual visual in _buildings.Values)
                {
                    visual.SetDetailLevel(next);
                }
            }

            RefreshDiagnostics();
        }

        private void RefreshDiagnostics()
        {
            int renderers = 0;
            int triangles = 0;
            int rebuilds = 0;
            foreach (DigBuildingVisual visual in _buildings.Values)
            {
                renderers += visual.VisibleRendererCount;
                triangles += visual.VisibleTriangleCount;
                rebuilds += visual.RebuildCount;
            }

            BuildingVisibleRendererCount = renderers;
            BuildingVisibleTriangleCount = triangles;
            BuildingRebuildCount = rebuilds;
        }

        private static float MeasurePixelsPerCell(
            Camera camera,
            Transform worldRoot)
        {
            Vector3 origin = camera.WorldToScreenPoint(
                worldRoot.TransformPoint(Vector3.zero));
            Vector3 logicalX = camera.WorldToScreenPoint(
                worldRoot.TransformPoint(Vector3.right));
            Vector3 logicalY = camera.WorldToScreenPoint(
                worldRoot.TransformPoint(Vector3.forward));
            if (origin.z <= 0f || logicalX.z <= 0f || logicalY.z <= 0f)
            {
                return 0f;
            }

            float xPixels = Vector2.Distance(
                new Vector2(origin.x, origin.y),
                new Vector2(logicalX.x, logicalX.y));
            float yPixels = Vector2.Distance(
                new Vector2(origin.x, origin.y),
                new Vector2(logicalY.x, logicalY.y));
            return Mathf.Max(xPixels, yPixels);
        }

        private void OnDestroy()
        {
            _representatives?.Dispose();
            _representatives = null;
        }
    }
}
