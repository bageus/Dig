using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigCellVisual : MonoBehaviour
    {
        private Renderer? _renderer;
        private Color _baseColor;
        private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();

        public WorldCellViewModel Model { get; private set; }

        public void Configure(WorldCellViewModel model, Color baseColor)
        {
            Model = model;
            _baseColor = baseColor;
            _renderer = GetComponent<Renderer>();
            ApplyColor(_baseColor);
        }

        public void SetSelected(bool selected)
        {
            Color color = selected
                ? Color.Lerp(_baseColor, Color.white, 0.45f)
                : _baseColor;
            ApplyColor(color);
        }

        private void ApplyColor(Color color)
        {
            if (_renderer == null)
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
