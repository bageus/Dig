using System.Collections.Generic;
using UnityEngine;

namespace Dig.Unity
{
    internal static class DigWorldItemHoverExtensions
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Dictionary<DigWorldItemVisual, HoverState> States =
            new Dictionary<DigWorldItemVisual, HoverState>();

        internal static void SetHovered(this DigWorldItemVisual item, bool hovered)
        {
            if (item == null)
            {
                return;
            }

            if (!hovered)
            {
                Restore(item);
                return;
            }

            if (States.ContainsKey(item))
            {
                return;
            }

            Renderer[] renderers = item.GetComponentsInChildren<Renderer>(includeInactive: true);
            Color[] colors = new Color[renderers.Length];
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                renderer.GetPropertyBlock(block);
                Color color = block.GetColor(BaseColorId);
                if (color == default)
                {
                    color = block.GetColor(ColorId);
                }
                if (color == default && renderer.sharedMaterial != null)
                {
                    color = renderer.sharedMaterial.color;
                }

                colors[index] = color;
                Color highlighted = Color.Lerp(color, Color.white, 0.48f);
                highlighted.a = color.a;
                block.SetColor(BaseColorId, highlighted);
                block.SetColor(ColorId, highlighted);
                renderer.SetPropertyBlock(block);
                block.Clear();
            }

            States[item] = new HoverState(renderers, colors);
        }

        private static void Restore(DigWorldItemVisual item)
        {
            if (!States.TryGetValue(item, out HoverState state))
            {
                return;
            }

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            for (int index = 0; index < state.Renderers.Length; index++)
            {
                Renderer renderer = state.Renderers[index];
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(block);
                block.SetColor(BaseColorId, state.Colors[index]);
                block.SetColor(ColorId, state.Colors[index]);
                renderer.SetPropertyBlock(block);
                block.Clear();
            }

            States.Remove(item);
        }

        private readonly struct HoverState
        {
            internal HoverState(Renderer[] renderers, Color[] colors)
            {
                Renderers = renderers;
                Colors = colors;
            }

            internal Renderer[] Renderers { get; }
            internal Color[] Colors { get; }
        }
    }
}
