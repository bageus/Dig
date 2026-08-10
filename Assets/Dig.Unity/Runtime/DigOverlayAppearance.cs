using Dig.Presentation.Overlays;
using UnityEngine;

namespace Dig.Unity
{
    internal readonly struct DigOverlayAppearance
    {
        internal DigOverlayAppearance(
            Color color,
            float lineWidth,
            OverlayShapeKind shape,
            OverlayPatternKind pattern,
            float scale,
            float tiltDegrees)
        {
            Color = color;
            LineWidth = lineWidth;
            Shape = shape;
            Pattern = pattern;
            Scale = scale;
            TiltDegrees = tiltDegrees;
        }

        internal Color Color { get; }
        internal float LineWidth { get; }
        internal OverlayShapeKind Shape { get; }
        internal OverlayPatternKind Pattern { get; }
        internal float Scale { get; }
        internal float TiltDegrees { get; }
    }
}
