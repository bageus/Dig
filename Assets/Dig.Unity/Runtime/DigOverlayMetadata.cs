using Dig.Presentation.Overlays;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigOverlayMetadata : MonoBehaviour
    {
        [SerializeField]
        private OverlayLayerKind layer;

        [SerializeField]
        private OverlaySemanticKind semantic;

        [SerializeField]
        private OverlayShapeKind shape;

        [SerializeField]
        private OverlayPatternKind pattern;

        public OverlayLayerKind Layer => layer;
        public OverlaySemanticKind Semantic => semantic;
        public OverlayShapeKind Shape => shape;
        public OverlayPatternKind Pattern => pattern;

        internal void Configure(
            OverlayLayerKind valueLayer,
            OverlaySemanticKind valueSemantic,
            OverlayShapeKind valueShape,
            OverlayPatternKind valuePattern)
        {
            layer = valueLayer;
            semantic = valueSemantic;
            shape = valueShape;
            pattern = valuePattern;
        }
    }
}
