using System;
using Dig.Presentation.Overlays;
using UnityEngine;

namespace Dig.Unity
{
    [Serializable]
    public sealed class DigOverlayAppearanceProfile
    {
        [SerializeField]
        private OverlaySemanticKind semantic;

        [SerializeField]
        private Color color = Color.white;

        [SerializeField]
        private float lineWidth = 0.075f;

        [SerializeField]
        private OverlayShapeKind shape = OverlayShapeKind.Outline;

        [SerializeField]
        private OverlayPatternKind pattern = OverlayPatternKind.Solid;

        [SerializeField]
        private float scale = 1f;

        [SerializeField]
        private float tiltDegrees;

        public OverlaySemanticKind Semantic => semantic;

        internal DigOverlayAppearance Resolve()
        {
            return new DigOverlayAppearance(
                color,
                Mathf.Clamp(lineWidth, 0.02f, 0.30f),
                shape,
                pattern,
                Mathf.Clamp(scale, 0.50f, 2f),
                Mathf.Clamp(tiltDegrees, -45f, 45f));
        }
    }
}
