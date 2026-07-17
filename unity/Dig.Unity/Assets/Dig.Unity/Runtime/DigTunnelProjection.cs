using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    internal static class DigTunnelProjection
    {
        internal const float DepthOrigin = 0.90f;
        internal const float DepthSpacing = -0.55f;
        internal const float ResidentHeight = 0.68f;
        internal const float ResidentDepthOffset = 0.12f;
        internal const float RouteHeight = 0.16f;
        internal const float RouteDepthOffset = 0.08f;

        internal static Vector3 CellWorldPosition(SpatialCellId cell)
        {
            return new Vector3(
                cell.X,
                -cell.Y,
                DepthOrigin + (cell.Z * DepthSpacing));
        }

        internal static Vector3 ResidentWorldPosition(
            float cellX,
            float cellY,
            float cellZ)
        {
            return new Vector3(
                cellX,
                -cellY + ResidentHeight,
                DepthOrigin + (cellZ * DepthSpacing) + ResidentDepthOffset);
        }

        internal static Vector3 RouteWorldPosition(SpatialCellId cell)
        {
            Vector3 position = CellWorldPosition(cell);
            position.y += RouteHeight;
            position.z += RouteDepthOffset;
            return position;
        }
    }
}
