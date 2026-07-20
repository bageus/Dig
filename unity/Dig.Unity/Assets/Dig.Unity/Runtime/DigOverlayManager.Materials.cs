using System;
using System.Collections.Generic;
using Dig.Presentation.Overlays;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigOverlayManager
    {
        [SerializeField]
        private DigOverlayAppearanceCatalog? appearanceCatalog;

        private readonly Dictionary<OverlaySemanticKind, Material> _materials =
            new Dictionary<OverlaySemanticKind, Material>();
        private Shader? _overlayShader;

        internal DigOverlayAppearance ResolveAppearance(
            OverlaySemanticKind semantic)
        {
            return appearanceCatalog == null
                ? DigOverlayAppearanceDefaults.Resolve(semantic)
                : appearanceCatalog.Resolve(semantic);
        }

        internal Material ResolveMaterial(OverlaySemanticKind semantic)
        {
            if (_materials.TryGetValue(semantic, out Material? material))
            {
                return material;
            }

            EnsureShader();
            DigOverlayAppearance appearance = ResolveAppearance(semantic);
            material = new Material(_overlayShader!)
            {
                name = $"Dig Overlay {semantic}",
                color = appearance.Color,
                enableInstancing = true,
            };
            _materials.Add(semantic, material);
            return material;
        }

        private void EnsureShader()
        {
            if (_overlayShader != null)
            {
                return;
            }

            _overlayShader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color");
            if (_overlayShader == null)
            {
                throw new InvalidOperationException(
                    "No supported overlay shader was found.");
            }
        }

        private void OnDestroy()
        {
            foreach (Material material in _materials.Values)
            {
                Destroy(material);
            }

            _materials.Clear();
        }
    }
}
