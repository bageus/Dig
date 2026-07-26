using Dig.Domain.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    internal sealed class DigExcavationQuarterMarker : MonoBehaviour
    {
        private static readonly ExcavationQuarter[] Quarters =
        {
            ExcavationQuarter.UpperLeft,
            ExcavationQuarter.LowerLeft,
            ExcavationQuarter.UpperRight,
            ExcavationQuarter.LowerRight,
        };

        private static readonly Vector2[] Offsets =
        {
            new Vector2(-0.252f, 0.252f),
            new Vector2(-0.252f, -0.252f),
            new Vector2(0.252f, 0.252f),
            new Vector2(0.252f, -0.252f),
        };

        private static readonly Color ExcavatedColor =
            new Color(0.055f, 0.065f, 0.075f, 0.90f);

        private readonly Renderer[] _renderers = new Renderer[4];
        private MaterialPropertyBlock? _properties;
        private Color _designationColor;
        private ExcavationQuarter _completed;
        private bool _initialized;

        internal void Initialize(Color designationColor)
        {
            _designationColor = designationColor;
            if (_initialized)
            {
                Apply();
                return;
            }

            Renderer rootRenderer = GetComponent<Renderer>();
            Material material = rootRenderer.sharedMaterial;
            rootRenderer.enabled = false;
            for (int index = 0; index < Quarters.Length; index++)
            {
                GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
                part.name = Quarters[index].ToString();
                part.transform.SetParent(transform, worldPositionStays: false);
                part.transform.localPosition = new Vector3(
                    Offsets[index].x,
                    Offsets[index].y,
                    0f);
                part.transform.localRotation = Quaternion.identity;
                part.transform.localScale = new Vector3(0.486f, 0.486f, 1f);
                Collider collider = part.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                Renderer renderer = part.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sortingOrder = rootRenderer.sortingOrder;
                _renderers[index] = renderer;
            }

            _initialized = true;
            Apply();
        }

        internal void SetProgress(ExcavationQuarter completed)
        {
            _completed = completed;
            Apply();
        }

        private void Apply()
        {
            if (!_initialized)
            {
                return;
            }

            for (int index = 0; index < Quarters.Length; index++)
            {
                bool excavated = (_completed & Quarters[index]) != 0;
                Renderer renderer = _renderers[index];
                _properties ??= new MaterialPropertyBlock();
                _properties.Clear();
                Color color = excavated ? ExcavatedColor : _designationColor;
                _properties.SetColor("_BaseColor", color);
                _properties.SetColor("_Color", color);
                renderer.SetPropertyBlock(_properties);
                Transform part = renderer.transform;
                Vector2 offset = Offsets[index];
                part.localPosition = new Vector3(
                    offset.x,
                    offset.y,
                    excavated ? -0.32f : 0f);
                part.localScale = new Vector3(
                    excavated ? 0.45f : 0.486f,
                    excavated ? 0.45f : 0.486f,
                    excavated ? 0.42f : 1f);
            }
        }
    }
}
