using UnityEngine;

namespace Dig.Unity
{
    internal static class DigSideViewProjection
    {
        internal const float TerrainDepth = 0f;
        internal const float BuildingDepth = -0.44f;
        internal const float ItemDepth = -0.58f;
        internal const float ActorDepth = -0.72f;
        internal const float GhostDepth = -0.90f;
        internal const float OverlayDepth = -1.06f;
        internal const float RouteDepth = -1.18f;

        internal static Vector3 CellCenter(
            float cellX,
            float cellY,
            float depth = TerrainDepth)
        {
            return new Vector3(cellX, -cellY, depth);
        }

        internal static Vector3 WorldCenter(int width, int height)
        {
            return CellCenter(
                (width - 1) * 0.5f,
                (height - 1) * 0.5f);
        }
    }
}
