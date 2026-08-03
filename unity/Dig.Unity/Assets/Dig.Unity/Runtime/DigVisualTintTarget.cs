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
                if (renderer == null)
                {
                    continue;
                }

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
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(_properties);
                _properties.SetColor("_BaseColor", tint);
                _properties.SetColor("_Color", tint);
                renderer.SetPropertyBlock(_properties);
            }
        }

        private void EnsureRenderers()
        {
            if (HasLiveRendererCache())
            {
                return;
            }

            DigVisualPrefabRoot? root = GetComponent<DigVisualPrefabRoot>();
            Renderer[] candidates = root == null
                ? GetComponentsInChildren<Renderer>(includeInactive: true)
                : root.ResolveTintRenderers();
            int liveCount = 0;
            for (int index = 0; index < candidates.Length; index++)
            {
                if (candidates[index] != null)
                {
                    liveCount++;
                }
            }

            if (liveCount == candidates.Length)
            {
                _renderers = candidates;
                return;
            }

            Renderer[] live = new Renderer[liveCount];
            int destination = 0;
            for (int index = 0; index < candidates.Length; index++)
            {
                Renderer renderer = candidates[index];
                if (renderer != null)
                {
                    live[destination++] = renderer;
                }
            }

            _renderers = live;
        }

        private bool HasLiveRendererCache()
        {
            if (_renderers.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < _renderers.Length; index++)
            {
                if (_renderers[index] == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
