using Dig.Presentation.Overlays;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigOverlayManager
    {
        internal DigOverlayAppearance ConfigureRenderer(
            Renderer renderer,
            OverlayLayerKind layer,
            OverlaySemanticKind semantic)
        {
            DigOverlayAppearance appearance = ResolveAppearance(semantic);
            renderer.sharedMaterial = ResolveMaterial(semantic);
            ApplyColor(renderer, appearance.Color);
            renderer.sortingOrder = ResolveSortingOrder(layer);
            ConfigureMetadata(renderer.gameObject, layer, semantic, appearance);
            return appearance;
        }

        internal DigOverlayAppearance ConfigureLineRenderer(
            LineRenderer line,
            OverlayLayerKind layer,
            OverlaySemanticKind semantic)
        {
            DigOverlayAppearance appearance = ConfigureRenderer(
                line,
                layer,
                semantic);
            line.widthMultiplier = appearance.LineWidth;
            line.numCapVertices = appearance.Pattern == OverlayPatternKind.Solid
                ? 3
                : 0;
            line.numCornerVertices = appearance.Pattern == OverlayPatternKind.Double
                ? 3
                : 1;
            line.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return appearance;
        }

        private void ApplyColor(Renderer renderer, Color color)
        {
            _colorProperties ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_colorProperties);
            _colorProperties.SetColor("_BaseColor", color);
            _colorProperties.SetColor("_Color", color);
            renderer.SetPropertyBlock(_colorProperties);
        }

        private static void ConfigureMetadata(
            GameObject target,
            OverlayLayerKind layer,
            OverlaySemanticKind semantic,
            DigOverlayAppearance appearance)
        {
            DigOverlayMetadata metadata = target.GetComponent<DigOverlayMetadata>()
                ?? target.AddComponent<DigOverlayMetadata>();
            metadata.Configure(
                layer,
                semantic,
                appearance.Shape,
                appearance.Pattern);
        }
    }
}
