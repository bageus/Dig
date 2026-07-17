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

        internal static Vector3 CellLocalPosition(SpatialCellId cell)
        {
            return new Vector3(
                cell.X,
                DepthOrigin + (cell.Z * DepthSpacing),
                cell.Y);
        }

        internal static Vector3 ResidentLocalPosition(
            float cellX,
            float cellY,
            float cellZ)
        {
            return new Vector3(
                cellX,
                DepthOrigin + (cellZ * DepthSpacing) + ResidentDepthOffset,
                cellY - ResidentHeight);
        }
    }
}
