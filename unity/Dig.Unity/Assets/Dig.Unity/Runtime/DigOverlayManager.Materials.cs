using Dig.Presentation.Overlays;
using Dig.Presentation.Rendering;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigOverlayManager
    {
        [SerializeField]
        private DigOverlayAppearanceCatalog? appearanceCatalog;

        private DigRenderMaterialLibrary? _materialLibrary;
        private Material? _overlayMaterial;
        private MaterialPropertyBlock? _colorProperties;

        internal DigOverlayAppearance ResolveAppearance(
            OverlaySemanticKind semantic)
        {
            return appearanceCatalog == null
                ? DigOverlayAppearanceDefaults.Resolve(semantic)
                : appearanceCatalog.Resolve(semantic);
        }

        internal Material ResolveMaterial(OverlaySemanticKind semantic)
        {
            if (_overlayMaterial != null)
            {
                return _overlayMaterial;
            }

            _materialLibrary = GetComponent<DigRenderMaterialLibrary>();
            if (_materialLibrary == null)
            {
                _materialLibrary = gameObject.AddComponent<DigRenderMaterialLibrary>();
            }

            _overlayMaterial = _materialLibrary.Resolve(
                RenderMaterialSemantic.Overlay,
                RenderSurfaceKind.Overlay,
                Color.white);
            return _overlayMaterial;
        }
    }
}
