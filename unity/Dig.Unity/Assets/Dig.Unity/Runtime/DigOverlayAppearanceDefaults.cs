using Dig.Presentation.Overlays;
using UnityEngine;

namespace Dig.Unity
{
    internal static class DigOverlayAppearanceDefaults
    {
        internal static DigOverlayAppearance Resolve(
            OverlaySemanticKind semantic)
        {
            OverlayStyleDefinition style = FindStyle(semantic);
            return new DigOverlayAppearance(
                ResolveColor(semantic),
                0.075f * (style.WidthPercent / 100f),
                style.Shape,
                style.Pattern,
                style.ScalePercent / 100f,
                style.TiltDegrees);
        }

        private static OverlayStyleDefinition FindStyle(
            OverlaySemanticKind semantic)
        {
            for (int index = 0; index < DefaultOverlayStyles.Values.Count; index++)
            {
                OverlayStyleDefinition value = DefaultOverlayStyles.Values[index];
                if (value.Semantic == semantic)
                {
                    return value;
                }
            }

            return new OverlayStyleDefinition(
                semantic,
                OverlayShapeKind.Outline,
                OverlayPatternKind.Solid,
                100,
                100,
                0);
        }

        private static Color ResolveColor(OverlaySemanticKind semantic)
        {
            return semantic switch
            {
                OverlaySemanticKind.Selection => Color.white,
                OverlaySemanticKind.PreviewValid =>
                    new Color(0.25f, 1f, 0.42f, 1f),
                OverlaySemanticKind.PreviewInvalid =>
                    new Color(1f, 0.18f, 0.16f, 1f),
                OverlaySemanticKind.JobAvailable =>
                    new Color(1f, 0.75f, 0.18f, 1f),
                OverlaySemanticKind.JobClaimed =>
                    new Color(0.28f, 0.78f, 1f, 1f),
                OverlaySemanticKind.JobInProgress =>
                    new Color(0.20f, 0.92f, 0.68f, 1f),
                OverlaySemanticKind.JobBlocked =>
                    new Color(1f, 0.32f, 0.22f, 1f),
                OverlaySemanticKind.JobAttention =>
                    new Color(1f, 0.32f, 0.78f, 1f),
                OverlaySemanticKind.JobTerminal =>
                    new Color(0.46f, 0.46f, 0.46f, 1f),
                OverlaySemanticKind.Route =>
                    new Color(0.40f, 0.92f, 1f, 1f),
                OverlaySemanticKind.Reservation =>
                    new Color(0.92f, 0.70f, 0.18f, 1f),
                OverlaySemanticKind.Designation =>
                    new Color(1f, 0.47f, 0.12f, 1f),
                OverlaySemanticKind.BuildingFootprint =>
                    new Color(0.72f, 0.54f, 1f, 1f),
                OverlaySemanticKind.StorageDemand =>
                    new Color(1f, 0.74f, 0.20f, 1f),
                OverlaySemanticKind.Deposit =>
                    new Color(0.34f, 0.92f, 0.86f, 1f),
                OverlaySemanticKind.Fog =>
                    new Color(0.24f, 0.28f, 0.38f, 0.86f),
                OverlaySemanticKind.DirtyChunk =>
                    new Color(1f, 0.22f, 0.78f, 1f),
                OverlaySemanticKind.NavigationDiagnostic =>
                    new Color(1f, 0.30f, 0.22f, 1f),
                _ => new Color(0.72f, 0.78f, 0.86f, 1f),
            };
        }
    }
}
