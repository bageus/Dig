using System.Collections.Generic;
using Dig.Domain.World;
using Dig.Presentation.Overlays;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    internal sealed class DigExcavationCursorRenderer : MonoBehaviour
    {
        private const float FaceOffset = 0.025f;
        private static readonly Color TunnelColor =
            new Color(0.68f, 0.86f, 0.62f, 0.72f);
        private static readonly Color DepthColor =
            new Color(0.74f, 0.62f, 0.90f, 0.72f);

        private readonly Dictionary<CellId, GameObject> _tunnelDesignations =
            new Dictionary<CellId, GameObject>();
        private GameObject? _marker;
        private DigOverlayManager? _overlays;
        private MaterialPropertyBlock? _properties;

        internal void Initialize(DigOverlayManager overlays)
        {
            _overlays = overlays;
        }

        internal void Show(CellId cell, bool depth)
        {
            _marker ??= CreateMarker("Excavation tool cursor", sortingOrder: 50);
            PlaceMarker(_marker, cell, depth ? DepthColor : TunnelColor);
        }

        internal void Hide()
        {
            if (_marker != null)
            {
                _marker.SetActive(false);
            }
        }

        internal void SetTunnelDesignation(CellId cell, bool active)
        {
            if (!active)
            {
                if (_tunnelDesignations.TryGetValue(cell, out GameObject? existing))
                {
                    Destroy(existing);
                    _tunnelDesignations.Remove(cell);
                }

                return;
            }

            if (!_tunnelDesignations.TryGetValue(cell, out GameObject? marker))
            {
                marker = CreateMarker(
                    $"Tunnel designation {cell.X},{cell.Y},{cell.Z}",
                    sortingOrder: 40);
                _tunnelDesignations.Add(cell, marker);
            }

            PlaceMarker(marker, cell, TunnelColor);
        }

        private GameObject CreateMarker(string markerName, int sortingOrder)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = markerName;
            marker.transform.SetParent(transform, worldPositionStays: true);
            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = marker.GetComponent<Renderer>();
            _overlays!.ConfigureRenderer(
                renderer,
                OverlayLayerKind.Preview,
                OverlaySemanticKind.PreviewValid);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = sortingOrder;
            marker.SetActive(false);
            return marker;
        }

        private void PlaceMarker(GameObject marker, CellId cell, Color color)
        {
            Vector3 center = DigTunnelProjection.CellWorldPosition(cell);
            marker.transform.position = center + new Vector3(
                0f,
                0f,
                DigTunnelProjection.RockCellHalfExtent + FaceOffset);
            marker.transform.rotation = Quaternion.identity;
            marker.transform.localScale = new Vector3(0.94f, 0.94f, 0.025f);

            Renderer renderer = marker.GetComponent<Renderer>();
            _properties ??= new MaterialPropertyBlock();
            _properties.Clear();
            _properties.SetColor("_BaseColor", color);
            _properties.SetColor("_Color", color);
            renderer.SetPropertyBlock(_properties);
            marker.SetActive(true);
        }
    }
}
