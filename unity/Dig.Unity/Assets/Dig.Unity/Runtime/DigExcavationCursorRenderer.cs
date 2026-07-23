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

        private GameObject? _marker;
        private Renderer? _renderer;
        private MaterialPropertyBlock? _properties;
        private DigOverlayManager? _overlays;

        internal void Initialize(DigOverlayManager overlays)
        {
            _overlays = overlays;
        }

        internal void Show(CellId cell, bool depth)
        {
            EnsureMarker();
            Vector3 center = DigTunnelProjection.CellWorldPosition(cell);
            _marker!.transform.position = center + new Vector3(
                0f,
                0f,
                DigTunnelProjection.RockCellHalfExtent + FaceOffset);
            _marker.transform.rotation = Quaternion.identity;
            _marker.transform.localScale = new Vector3(0.94f, 0.94f, 0.025f);

            Color color = depth ? DepthColor : TunnelColor;
            _properties!.Clear();
            _properties.SetColor("_BaseColor", color);
            _properties.SetColor("_Color", color);
            _renderer!.SetPropertyBlock(_properties);
            _marker.SetActive(true);
        }

        internal void Hide()
        {
            if (_marker != null)
            {
                _marker.SetActive(false);
            }
        }

        private void EnsureMarker()
        {
            if (_marker != null)
            {
                return;
            }

            _marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _marker.name = "Excavation tool cursor";
            _marker.transform.SetParent(transform, worldPositionStays: true);
            Collider collider = _marker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            _renderer = _marker.GetComponent<Renderer>();
            _overlays!.ConfigureRenderer(
                _renderer,
                OverlayLayerKind.Preview,
                OverlaySemanticKind.PreviewValid);
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.sortingOrder = 50;
            _properties = new MaterialPropertyBlock();
        }
    }
}
