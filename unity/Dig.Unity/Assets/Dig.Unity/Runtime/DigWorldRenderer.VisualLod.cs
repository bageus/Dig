using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldRenderer
    {
        private readonly TerrainVisualDetailPolicy _terrainVisualDetailPolicy =
            new TerrainVisualDetailPolicy();
        private TerrainVisualDetailLevel _terrainVisualDetailLevel =
            TerrainVisualDetailLevel.Full;
        private Camera? _terrainVisualLodCamera;

        internal TerrainVisualDetailLevel TerrainVisualDetailLevel =>
            _terrainVisualDetailLevel;

        internal float TerrainPixelsPerCell { get; private set; }

        private void LateUpdate()
        {
            if (_terrainVisualLodCamera == null)
            {
                _terrainVisualLodCamera = Camera.main;
            }

            if (_terrainVisualLodCamera == null)
            {
                return;
            }

            TerrainPixelsPerCell = MeasurePixelsPerCell(
                _terrainVisualLodCamera,
                transform);
            TerrainVisualDetailLevel next = _terrainVisualDetailPolicy.Resolve(
                TerrainPixelsPerCell,
                _terrainVisualDetailLevel);
            if (next == _terrainVisualDetailLevel)
            {
                return;
            }

            _terrainVisualDetailLevel = next;
            _terrainChunkRenderer?.SetDetailLevel(next);
            _caveTemplateTrimRenderer?.SetDetailLevel(next);
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
    }
}
