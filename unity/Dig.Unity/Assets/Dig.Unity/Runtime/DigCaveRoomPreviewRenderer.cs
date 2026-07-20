using System.Collections.Generic;
using Dig.Application.World;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed partial class DigCaveRoomPreviewRenderer : MonoBehaviour
    {
        private const int EdgeCount = 12;
        private const int InvalidCrossEdgeCount = 2;
        private const int TotalEdgeCount = EdgeCount + InvalidCrossEdgeCount;
        private const int BoxEdgeCount = EdgeCount;

        private readonly List<LineRenderer> _edges =
            new List<LineRenderer>(TotalEdgeCount);
        private Transform? _root;
        private DigOverlayManager? _overlays;

        private static Vector3[] CreateCorners(
            CaveRoomPreset preset,
            CellId entrance)
        {
            int baseMinX = entrance.X - ((preset.BaseWidth - 1) / 2);
            int topMinX = entrance.X - ((preset.TopWidth - 1) / 2);
            float baseLeft = baseMinX - 0.5f;
            float baseRight = baseMinX + preset.BaseWidth - 0.5f;
            float topLeft = topMinX - 0.5f;
            float topRight = topMinX + preset.TopWidth - 0.5f;
            float bottom = -entrance.Y - 0.5f;
            float top = -entrance.Y + preset.Height - 0.5f;
            float firstDepth = DigTunnelProjection.CellWorldPosition(
                new SpatialCellId(entrance.X, entrance.Y, 0)).z;
            float lastDepth = DigTunnelProjection.CellWorldPosition(
                new SpatialCellId(entrance.X, entrance.Y, preset.Depth - 1)).z;
            float halfDepth = Mathf.Abs(DigTunnelProjection.DepthSpacing) * 0.47f;
            float front = Mathf.Max(firstDepth, lastDepth) + halfDepth;
            float back = Mathf.Min(firstDepth, lastDepth) - halfDepth;
            return new[]
            {
                new Vector3(baseLeft, bottom, front),
                new Vector3(baseRight, bottom, front),
                new Vector3(topRight, top, front),
                new Vector3(topLeft, top, front),
                new Vector3(baseLeft, bottom, back),
                new Vector3(baseRight, bottom, back),
                new Vector3(topRight, top, back),
                new Vector3(topLeft, top, back),
            };
        }
    }
}
