using UnityEngine;
using UnityEngine.Rendering;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    internal sealed class DigBuildingBoxSelectionHighlight : MonoBehaviour
    {
        private static readonly Color HighlightColor =
            new Color(0.18f, 0.58f, 1f, 0.34f);

        private GameObject? _surface;

        internal void SetHighlighted(bool highlighted)
        {
            EnsureSurface();
            _surface!.SetActive(highlighted);
            if (highlighted)
            {
                RefreshBounds();
            }
        }

        private void EnsureSurface()
        {
            if (_surface != null)
            {
                return;
            }

            _surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _surface.name = "BuildingBox Selection Highlight";
            _surface.transform.SetParent(transform, worldPositionStays: false);
            Collider collider = _surface.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = _surface.GetComponent<Renderer>();
            Renderer? source = GetComponentInChildren<Renderer>();
            if (source != null)
            {
                renderer.sharedMaterial = source.sharedMaterial;
            }

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            DigVisualTintTarget tint = _surface.AddComponent<DigVisualTintTarget>();
            tint.Configure(renderer.sharedMaterial, HighlightColor);
            _surface.AddComponent<DigTransparentVisualSurface>().Configure(HighlightColor.a);
            _surface.SetActive(false);
        }

        private void RefreshBounds()
        {
            BoxCollider? interaction = GetComponent<BoxCollider>();
            Vector3 center = interaction == null ? new Vector3(0f, 0.18f, 0f) : interaction.center;
            Vector3 size = interaction == null ? new Vector3(0.46f, 0.46f, 0.46f) : interaction.size;
            _surface!.transform.localPosition = center;
            _surface.transform.localRotation = Quaternion.identity;
            _surface.transform.localScale = size + new Vector3(0.08f, 0.08f, 0.08f);
        }
    }
}
