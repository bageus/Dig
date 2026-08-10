using System;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigAgentVisual
    {
        private bool PrepareHoverForRendererMutation()
        {
            bool reapplyHover = _hovered && !_selected;
            RestoreHoverBeforeRendererMutation();
            InvalidateHoverRendererCache();
            return reapplyHover;
        }

        private void CompleteHoverRendererMutation(bool reapplyHover)
        {
            InvalidateHoverRendererCache();
            if (reapplyHover)
            {
                ApplyHover();
            }
        }

        private void RestoreHoverBeforeRendererMutation()
        {
            if (!_hoverApplied)
            {
                return;
            }

            MaterialPropertyBlock properties = ResolveHoverProperties();
            for (int index = 0; index < _hoverRenderers.Length; index++)
            {
                Renderer renderer = _hoverRenderers[index];
                if (renderer == null || index >= _hoverBaseColors.Length)
                {
                    continue;
                }

                properties.Clear();
                renderer.GetPropertyBlock(properties);
                properties.SetColor(BaseColorId, _hoverBaseColors[index]);
                properties.SetColor(ColorId, _hoverBaseColors[index]);
                renderer.SetPropertyBlock(properties);
            }

            _hoverApplied = false;
        }

        private void InvalidateHoverRendererCache()
        {
            _hoverRenderers = Array.Empty<Renderer>();
            _hoverBaseColors = Array.Empty<Color>();
        }
    }
}
