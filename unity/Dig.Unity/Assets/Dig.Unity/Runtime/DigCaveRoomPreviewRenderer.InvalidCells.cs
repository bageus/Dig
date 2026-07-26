using System.Collections.Generic;
using Dig.Application.World;
using Dig.Domain.World;
using Dig.Presentation.Overlays;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dig.Unity
{
    public sealed partial class DigCaveRoomPreviewRenderer
    {
        private static readonly Color InvalidCellColor =
            new Color(0.96f, 0.20f, 0.16f, 0.70f);

        private readonly List<GameObject> _invalidCellMarkers =
            new List<GameObject>();
        private MaterialPropertyBlock? _invalidCellProperties;

        private void UpdateInvalidCells(
            IReadOnlyList<CaveRoomInvalidCell> invalidCells)
        {
            for (int index = 0; index < invalidCells.Count; index++)
            {
                GameObject marker = EnsureInvalidCellMarker(index);
                CellId cell = invalidCells[index].Cell;
                Vector3 position = DigTunnelProjection.CellWorldPosition(cell);
                position.z += DigTunnelProjection.RockCellHalfExtent
                    + PreviewFaceOffset
                    + 0.012f;
                marker.transform.SetPositionAndRotation(position, Quaternion.identity);
                marker.transform.localScale = new Vector3(0.88f, 0.88f, 0.035f);
                marker.SetActive(true);
            }

            for (int index = invalidCells.Count; index < _invalidCellMarkers.Count; index++)
            {
                _invalidCellMarkers[index].SetActive(false);
            }
        }

        private GameObject EnsureInvalidCellMarker(int index)
        {
            while (_invalidCellMarkers.Count <= index)
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = $"Cave room invalid cell {_invalidCellMarkers.Count + 1}";
                marker.transform.SetParent(_root, worldPositionStays: true);
                Collider collider = marker.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                Renderer renderer = marker.GetComponent<Renderer>();
                _overlays!.ConfigureRenderer(
                    renderer,
                    OverlayLayerKind.Preview,
                    OverlaySemanticKind.PreviewInvalid);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                _invalidCellProperties ??= new MaterialPropertyBlock();
                _invalidCellProperties.Clear();
                _invalidCellProperties.SetColor("_BaseColor", InvalidCellColor);
                _invalidCellProperties.SetColor("_Color", InvalidCellColor);
                renderer.SetPropertyBlock(_invalidCellProperties);
                DigTransparentVisualSurface transparent =
                    marker.AddComponent<DigTransparentVisualSurface>();
                transparent.Configure(fixedOpacity: 0.70f);
                marker.SetActive(false);
                _invalidCellMarkers.Add(marker);
            }

            return _invalidCellMarkers[index];
        }
    }
}
