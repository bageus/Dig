using System;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.World;
using Dig.Presentation.Overlays;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigCaveRoomPreviewRenderer
    {
        private static readonly Color MissingTunnelColor =
            new Color(0.92f, 0.22f, 0.18f, 0.84f);
        private static readonly Color ValidOutlineColor =
            new Color(0.55f, 0.82f, 0.96f, 0.92f);

        internal void Show(
            CaveRoomPreset preset,
            CellId entrance,
            CaveRoomPlanResult result)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            EnsureResources();
            Vector3[] corners = CreateCorners(preset, entrance);
            UpdateFill(corners);
            UpdateInvalidCells(result.InvalidCells);

            for (int index = 0; index < _edges.Count; index++)
            {
                _edges[index].enabled = false;
            }

            Vector2Int[] frontEdges =
            {
                new Vector2Int(0, 1),
                new Vector2Int(1, 2),
                new Vector2Int(2, 3),
                new Vector2Int(3, 0),
            };
            for (int index = 0; index < frontEdges.Length; index++)
            {
                LineRenderer edge = _edges[index];
                _overlays!.ConfigureLineRenderer(
                    edge,
                    OverlayLayerKind.Preview,
                    OverlaySemanticKind.PreviewValid);
                edge.startColor = ValidOutlineColor;
                edge.endColor = ValidOutlineColor;
                edge.enabled = true;
                edge.SetPosition(0, corners[frontEdges[index].x]);
                edge.SetPosition(1, corners[frontEdges[index].y]);
            }

            if (!result.Succeeded
                && result.InvalidCells.Any(value =>
                    value.Reason == CaveRoomPlanFailureReason.BaseTunnelMissing))
            {
                LineRenderer missingTunnel = _edges[0];
                _overlays!.ConfigureLineRenderer(
                    missingTunnel,
                    OverlayLayerKind.Preview,
                    OverlaySemanticKind.PreviewInvalid);
                missingTunnel.startColor = MissingTunnelColor;
                missingTunnel.endColor = MissingTunnelColor;
            }
        }
    }
}
