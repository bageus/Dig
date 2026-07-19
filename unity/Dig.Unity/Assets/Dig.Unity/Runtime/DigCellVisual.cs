using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigCellVisual : MonoBehaviour
    {
        private Renderer? _renderer;
        private MaterialPropertyBlock? _properties;
        private Color _baseColor;
        private bool _selected;
        private bool _rejected;

        public WorldCellViewModel Model { get; private set; }

        public void Configure(WorldCellViewModel model, Color baseColor)
        {
            Model = model;
            _baseColor = baseColor;
            _rejected = false;
            DisableInteractionCollider();
            EnsureRenderState();
            RefreshColor();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            RefreshColor();
        }

        internal void SetRejected(bool rejected)
        {
            _rejected = rejected;
            RefreshColor();
        }

        private void DisableInteractionCollider()
        {
            Collider? collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        private void RefreshColor()
        {
            Color color = _rejected
                ? Color.Lerp(_baseColor, Color.red, 0.82f)
                : _selected
                    ? Color.Lerp(_baseColor, Color.white, 0.45f)
                    : _baseColor;
            ApplyColor(color);
        }

        private void EnsureRenderState()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }

            if (_properties == null)
            {
                _properties = new MaterialPropertyBlock();
            }
        }

        private void ApplyColor(Color color)
        {
            EnsureRenderState();
            if (_renderer == null || _properties == null)
            {
                return;
            }

            _renderer.GetPropertyBlock(_properties);
            _properties.SetColor("_BaseColor", color);
            _properties.SetColor("_Color", color);
            _renderer.SetPropertyBlock(_properties);
        }
    }
}
