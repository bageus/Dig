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

        public WorldCellViewModel Model { get; private set; }

        public void Configure(WorldCellViewModel model, Color baseColor)
        {
            Model = model;
            _baseColor = baseColor;
            EnsureRenderState();
            ApplyColor(_baseColor);
        }

        public void SetSelected(bool selected)
        {
            Color color = selected
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
