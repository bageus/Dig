using System.Collections.Generic;
using Dig.Presentation.Overlays;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigCaveRoomPreviewRenderer
    {
        private void ConfigureInvalidMarker(
            IReadOnlyList<Vector3> corners,
            bool valid,
            OverlaySemanticKind semantic)
        {
            Vector2Int[] cross =
            {
                new Vector2Int(0, 2),
                new Vector2Int(1, 3),
            };
            for (int index = 0; index < 2; index++)
            {
                LineRenderer edge = _edges[BoxEdgeCount + index];
                edge.enabled = !valid;
                if (valid)
                {
                    continue;
                }

                _overlays!.ConfigureLineRenderer(
                    edge,
                    OverlayLayerKind.Preview,
                    semantic);
                edge.SetPosition(0, corners[cross[index].x]);
                edge.SetPosition(1, corners[cross[index].y]);
            }
        }

        private static Vector2Int[] CreateBoxConnections()
        {
            return new[]
            {
                new Vector2Int(0, 1),
                new Vector2Int(1, 2),
                new Vector2Int(2, 3),
                new Vector2Int(3, 0),
                new Vector2Int(4, 5),
                new Vector2Int(5, 6),
                new Vector2Int(6, 7),
                new Vector2Int(7, 4),
                new Vector2Int(0, 4),
                new Vector2Int(1, 5),
                new Vector2Int(2, 6),
                new Vector2Int(3, 7),
            };
        }
    }
}
