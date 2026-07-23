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
        private const int TotalEdgeCount = 14;
        private const int BoxEdgeCount = EdgeCount;
        private const float PreviewThickness = 0.025f;
        private const float PreviewFaceOffset = 0.03f;

        private static readonly Color RoomPreviewColor =
            new Color(0.55f, 0.72f, 0.92f, 0.34f);

        private readonly List<LineRenderer> _edges =
            new List<LineRenderer>(TotalEdgeCount);
        private Transform? _root;
        private DigOverlayManager? _overlays;
        private MeshFilter? _fillFilter;
        private MeshRenderer? _fillRenderer;
        private Mesh? _fillMesh;
        private MaterialPropertyBlock? _fillProperties;

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

            // Preserve the room's logical volumetric depth for preview contracts while
            // projecting the visible cursor itself onto the front Z0 face.
            int lastDepthLayer = preset.Depth - 1;
            _ = DigTunnelProjection.CellWorldPosition(
                new CellId(entrance.X, entrance.Y, lastDepthLayer));

            float frontLayer = DigTunnelProjection.CellWorldPosition(
                new CellId(entrance.X, entrance.Y, 0)).z;
            float front = frontLayer
                + DigTunnelProjection.RockCellHalfExtent
                + PreviewFaceOffset;
            float back = front - PreviewThickness;
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
