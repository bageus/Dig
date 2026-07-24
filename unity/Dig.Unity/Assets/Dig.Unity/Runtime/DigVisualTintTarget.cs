using System;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigVisualTintTarget : MonoBehaviour
    {
        private Renderer[] _renderers = Array.Empty<Renderer>();
        private MaterialPropertyBlock? _properties;

        internal Color CurrentTint { get; private set; } = Color.white;

        public void Configure(Material? material, Color tint)
        {
            EnsureRenderers();
            for (int index = 0; index < _renderers.Length; index++)
            {
                Renderer renderer = _renderers[index];
                if (material != null)
                {
                    renderer.sharedMaterial = material;
                }
            }

            SetTint(tint);
        }

        public void SetTint(Color tint)
        {
            EnsureRenderers();
            CurrentTint = tint;
            _properties ??= new MaterialPropertyBlock();
            for (int index = 0; index < _renderers.Length; index++)
            {
                Renderer renderer = _renderers[index];
                renderer.GetPropertyBlock(_properties);
                _properties.SetColor("_BaseColor", tint);
                _properties.SetColor("_Color", tint);
                renderer.SetPropertyBlock(_properties);
            }
        }

        private void EnsureRenderers()
        {
            if (_renderers.Length > 0)
            {
                return;
            }

            DigVisualPrefabRoot? root = GetComponent<DigVisualPrefabRoot>();
            _renderers = root == null
                ? GetComponentsInChildren<Renderer>(includeInactive: true)
                : root.ResolveTintRenderers();
        }
    }
}
