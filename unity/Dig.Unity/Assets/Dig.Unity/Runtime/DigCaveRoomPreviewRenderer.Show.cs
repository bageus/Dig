using System;
using Dig.Application.World;
using Dig.Domain.World;
using Dig.Presentation.Overlays;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigCaveRoomPreviewRenderer
    {
        internal void Show(
            CaveRoomPreset preset,
            CellId entrance,
            bool valid)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            EnsureResources();
            Vector3[] corners = CreateCorners(preset, entrance);
            Vector2Int[] connections = CreateBoxConnections();
            OverlaySemanticKind semantic = valid
                ? OverlaySemanticKind.PreviewValid
                : OverlaySemanticKind.PreviewInvalid;
            for (int index = 0; index < connections.Length; index++)
            {
                LineRenderer edge = _edges[index];
                _overlays!.ConfigureLineRenderer(
                    edge,
                    OverlayLayerKind.Preview,
                    semantic);
                edge.enabled = true;
                edge.SetPosition(0, corners[connections[index].x]);
                edge.SetPosition(1, corners[connections[index].y]);
            }

            ConfigureInvalidMarker(corners, valid, semantic);
        }
    }
}
