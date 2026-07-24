using System;
using Dig.Application.World;
using Dig.Domain.World;
using Dig.Presentation.Overlays;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigCaveRoomPreviewRenderer
    {
        private static readonly Color MissingTunnelColor =
            new Color(0.92f, 0.22f, 0.18f, 0.72f);

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
            UpdateFill(corners);

            for (int index = 0; index < _edges.Count; index++)
            {
                _edges[index].enabled = false;
            }

            if (valid)
            {
                return;
            }

            // Keep the room body readable without a green box outline. When the
            // entrance/support tunnel is missing, mark only the bottom opening in red.
            LineRenderer missingTunnel = _edges[0];
            _overlays!.ConfigureLineRenderer(
                missingTunnel,
                OverlayLayerKind.Preview,
                OverlaySemanticKind.PreviewValid);
            missingTunnel.startColor = MissingTunnelColor;
            missingTunnel.endColor = MissingTunnelColor;
            missingTunnel.enabled = true;
            missingTunnel.SetPosition(0, corners[0]);
            missingTunnel.SetPosition(1, corners[1]);
        }
    }
}
