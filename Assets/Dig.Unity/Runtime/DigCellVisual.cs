using Dig.Domain.World;
using Dig.Presentation.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigCellVisual : MonoBehaviour
    {
        private static readonly Color TunnelDesignationColor =
            new Color(0.68f, 0.86f, 0.62f, 1f);
        private static readonly ExcavationQuarter[] ExcavationQuarters =
        {
            ExcavationQuarter.UpperLeft,
            ExcavationQuarter.LowerLeft,
            ExcavationQuarter.UpperRight,
            ExcavationQuarter.LowerRight,
        };
        private static readonly Vector2[] ExcavationQuarterOffsets =
        {
            new Vector2(-0.252f, -0.252f),
            new Vector2(-0.252f, 0.252f),
            new Vector2(0.252f, -0.252f),
            new Vector2(0.252f, 0.252f),
        };

        private readonly Renderer[] _quarterRenderers = new Renderer[4];
        private Renderer? _renderer;
        private MaterialPropertyBlock? _properties;
        private Color _baseColor;
        private Color? _designationTint;
        private ExcavationQuarter _completedExcavationQuarters;
        private bool _selected;
        private bool _rejected;
        private bool _quarterGeometryInitialized;

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

            _completedExcavationQuarters = model.IsSolid
                ? model.CompletedExcavationQuarters
                : ExcavationQuarter.None;

            _rejected = false;
            AlignWithChunkBuilderSpace(model);
            EnsureRenderState();
            RefreshExcavationGeometry();
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

        internal void SetExcavationProgress(ExcavationQuarter completed)
        {
            _completedExcavationQuarters = completed;
            RefreshExcavationGeometry();
            RefreshColor();
        }

        private void AlignWithChunkBuilderSpace(WorldCellViewModel model)
        {
            float depth = DigTunnelProjection.DepthOrigin
                + (model.Z * DigTunnelProjection.DepthSpacing);
            transform.localPosition = new Vector3(model.X, depth, model.Y);
        }

        private void DisableInteractionCollider()
        {
            Collider? collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        private void RefreshExcavationGeometry()
        {
            EnsureRenderState();
            if (_renderer == null)
            {
                return;
            }

            bool showQuarters = Model.IsSolid
                && _completedExcavationQuarters != ExcavationQuarter.None;
            _renderer.enabled = Model.IsSolid && !showQuarters;
            if (!showQuarters)
            {
                SetQuarterGeometryActive(active: false);
                return;
            }

            EnsureQuarterGeometry();
            for (int index = 0; index < ExcavationQuarters.Length; index++)
            {
                bool excavated = (_completedExcavationQuarters
                    & ExcavationQuarters[index]) != 0;
                _quarterRenderers[index].gameObject.SetActive(!excavated);
            }
        }

        private void EnsureQuarterGeometry()
        {
            if (_quarterGeometryInitialized)
            {
                return;
            }

            EnsureRenderState();
            Material? material = _renderer?.sharedMaterial;
            for (int index = 0; index < ExcavationQuarters.Length; index++)
            {
                GameObject quarter = GameObject.CreatePrimitive(PrimitiveType.Cube);
                quarter.name = $"Rock {ExcavationQuarters[index]}";
                quarter.transform.SetParent(transform, worldPositionStays: false);
                // The side-view world root rotates logical Y onto Unity Z. Quarter
                // geometry must therefore split local X/Z and preserve local Y as
                // depth; using local Y here makes upper/lower pieces overlap on screen.
                quarter.transform.localPosition = new Vector3(
                    ExcavationQuarterOffsets[index].x,
                    0f,
                    ExcavationQuarterOffsets[index].y);
                quarter.transform.localRotation = Quaternion.identity;
                quarter.transform.localScale = new Vector3(0.486f, 1f, 0.486f);
                Collider collider = quarter.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                Renderer renderer = quarter.GetComponent<Renderer>();
                if (material != null)
                {
                    renderer.sharedMaterial = material;
                }

                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                _quarterRenderers[index] = renderer;
            }

            _quarterGeometryInitialized = true;
        }

        private void SetQuarterGeometryActive(bool active)
        {
            if (!_quarterGeometryInitialized)
            {
                return;
            }

            for (int index = 0; index < _quarterRenderers.Length; index++)
            {
                _quarterRenderers[index].gameObject.SetActive(active);
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
            ApplyColor(_renderer, color);
            if (_quarterGeometryInitialized)
            {
                for (int index = 0; index < _quarterRenderers.Length; index++)
                {
                    ApplyColor(_quarterRenderers[index], color);
                }
            }
        }

        private void EnsureRenderState()
        {
            _renderer ??= GetComponent<Renderer>();
            _properties ??= new MaterialPropertyBlock();
        }

        private void ApplyColor(Renderer? renderer, Color color)
        {
            EnsureRenderState();
            if (renderer == null || _properties == null)
            {
                return;
            }

            renderer.GetPropertyBlock(_properties);
            _properties.SetColor("_BaseColor", color);
            _properties.SetColor("_Color", color);
            renderer.SetPropertyBlock(_properties);
        }
    }
}
