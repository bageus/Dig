using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    internal sealed class DigTransparentVisualSurface : MonoBehaviour
    {
        private readonly List<Material> _ownedMaterials = new List<Material>();
        private DigVisualTintTarget? _tintTarget;
        private float? _fixedOpacity;

        internal void Configure(float? fixedOpacity)
        {
            _fixedOpacity = fixedOpacity;
            _tintTarget = GetComponent<DigVisualTintTarget>();
            Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                Material[] sources = renderer.sharedMaterials;
                Material[] transparent = (Material[])sources.Clone();
                for (int materialIndex = 0; materialIndex < sources.Length; materialIndex++)
                {
                    Material? source = sources[materialIndex];
                    if (source == null)
                    {
                        continue;
                    }

                    Material material = new Material(source)
                    {
                        name = $"{source.name} Transparent",
                        hideFlags = HideFlags.HideAndDontSave,
                    };
                    ConfigureTransparentMaterial(material);
                    transparent[materialIndex] = material;
                    _ownedMaterials.Add(material);
                }

                renderer.sharedMaterials = transparent;
            }

            ApplyFixedOpacity();
        }

        private void LateUpdate()
        {
            ApplyFixedOpacity();
        }

        private void ApplyFixedOpacity()
        {
            if (!_fixedOpacity.HasValue || _tintTarget == null)
            {
                return;
            }

            Color tint = _tintTarget.CurrentTint;
            if (Mathf.Approximately(tint.a, _fixedOpacity.Value))
            {
                return;
            }

            tint.a = _fixedOpacity.Value;
            _tintTarget.SetTint(tint);
        }

        private static void ConfigureTransparentMaterial(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            SetFloatIfPresent(material, "_Surface", 1f);
            SetFloatIfPresent(material, "_Mode", 3f);
            SetFloatIfPresent(material, "_Blend", 0f);
            SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloatIfPresent(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfPresent(material, "_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetShaderPassEnabled("ShadowCaster", false);
        }

        private static void SetFloatIfPresent(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private void OnDestroy()
        {
            for (int index = 0; index < _ownedMaterials.Count; index++)
            {
                Material material = _ownedMaterials[index];
                if (material != null)
                {
                    Destroy(material);
                }
            }

            _ownedMaterials.Clear();
        }
    }
}
