using System;
using Dig.Presentation.World;

namespace Dig.Unity
{
    public sealed partial class DigWorldRenderer
    {
        private CaveTemplateTrimVolumeViewModel _caveTemplateTrims =
            CaveTemplateTrimVolumeViewModel.Empty();
        private DigCaveTemplateTrimRenderer? _caveTemplateTrimRenderer;

        internal void SetCaveTemplateTrims(CaveTemplateTrimVolumeViewModel trims)
        {
            _caveTemplateTrims = trims
                ?? throw new ArgumentNullException(nameof(trims));
            RefreshCaveTemplateTrims();
        }

        private void RefreshCaveTemplateTrims()
        {
            if (_caveTemplateTrimRenderer == null
                && _caveTemplateTrims.Instances.Count == 0)
            {
                return;
            }

            EnsureCaveTemplateTrimRenderer().Render(
                _caveTemplateTrims,
                terrainVisualCatalog);
        }

        private DigCaveTemplateTrimRenderer EnsureCaveTemplateTrimRenderer()
        {
            if (_caveTemplateTrimRenderer != null)
            {
                return _caveTemplateTrimRenderer;
            }

            _caveTemplateTrimRenderer = GetComponent<DigCaveTemplateTrimRenderer>();
            if (_caveTemplateTrimRenderer == null)
            {
                _caveTemplateTrimRenderer =
                    gameObject.AddComponent<DigCaveTemplateTrimRenderer>();
            }

            return _caveTemplateTrimRenderer;
        }
    }
}
