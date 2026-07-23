using Dig.Domain.World;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigCellVisual : MonoBehaviour
    {
        private static readonly Color TunnelDesignationColor =
            new Color(0.68f, 0.86f, 0.62f, 1f);

        private Renderer? _renderer;
        private MaterialPropertyBlock? _properties;
        private Color _baseColor;
        private Color? _designationTint;
        private bool _selected;
        private bool _rejected;

        public WorldCellViewModel Model { get; private set; }

        private void Awake()
        {
            DisableInteractionCollider();
        }

        public void Configure(WorldCellViewModel model, Color baseColor)
        {
            Model = model;
            _baseColor = model.IsDesignated ? TunnelDesignationColor : baseColor;
            if (!model.IsDesignated)
            {
                _designationTint = null;
            }

            _rejected = false;
            AlignWithChunkBuilderSpace(model);
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

        internal void SetDesignationTint(Color? color)
        {
            _designationTint = color;
            RefreshColor();
        }

        private void AlignWithChunkBuilderSpace(WorldCellViewModel model)
        {
            float depth = DigTunnelProjection.DepthOrigin
                + (model.Z * DigTunnelProjection.DepthSpacing);
            transform.localPosition = new Vector3(model.X, depth, model.Y);

            if (!model.IsSolid && transform.localScale != Vector3.zero)
            {
                transform.localScale = new Vector3(
                    0.94f,
                    DigTunnelProjection.FloorDepth,
                    DigTunnelProjection.FloorThickness);
            }
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
            Color baseColor = _designationTint ?? _baseColor;
            Color color = _rejected
                ? Color.Lerp(baseColor, Color.red, 0.82f)
                : _selected
                    ? Color.Lerp(baseColor, Color.white, 0.45f)
                    : baseColor;
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
