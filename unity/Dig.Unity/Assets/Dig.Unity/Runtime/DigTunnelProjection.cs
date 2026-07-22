using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    internal static class DigTunnelProjection
    {
        internal const float DepthOrigin = 0.41f;
        internal const float DepthSpacing = -0.55f;
        internal const float RockCellHalfExtent = 0.48f;
        internal const float FloorThickness = 0.08f;
        internal const float FloorDepth = 0.45f;
        internal const float ResidentFootSink = 0.02f;
        internal const float ResidentDepthOffset = 0f;
        internal const float RouteHeight = 0.10f;
        internal const float RouteDepthOffset = 0.02f;

        internal static Vector3 CellWorldPosition(CellId cell)
        {
            return new Vector3(
                cell.X,
                -cell.Y,
                DepthOrigin + (cell.Z * DepthSpacing));
        }

        internal static float WalkSurfaceY(float cellY)
        {
            return -(cellY + 1f) + RockCellHalfExtent;
        }

        internal static Vector3 FloorWorldPosition(CellId cell)
        {
            return new Vector3(
                cell.X,
                WalkSurfaceY(cell.Y) - (FloorThickness * 0.5f),
                DepthOrigin + (cell.Z * DepthSpacing));
        }

        internal static Vector3 ResidentWorldPosition(
            float cellX,
            float cellY,
            float cellZ)
        {
            return new Vector3(
                cellX,
                // Resident and creature roots are authored at their feet. Keeping the
                // authoritative root on the walk surface also keeps root colliders and
                // authored rigs aligned; visual height belongs to the child rig.
                WalkSurfaceY(cellY) - ResidentFootSink,
                DepthOrigin + (cellZ * DepthSpacing) + ResidentDepthOffset);
        }

        internal static Vector3 RouteWorldPosition(CellId cell)
        {
            return new Vector3(
                cell.X,
                WalkSurfaceY(cell.Y) + RouteHeight,
                DepthOrigin + (cell.Z * DepthSpacing) + RouteDepthOffset);
        }
    }
}
