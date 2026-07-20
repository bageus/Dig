using System.Collections.Generic;

namespace Dig.Presentation.Overlays
{

public static class DefaultOverlayStyles
{
    public static IReadOnlyList<OverlayStyleDefinition> Values { get; } =
        new[]
        {
            new OverlayStyleDefinition(OverlaySemanticKind.Selection, OverlayShapeKind.Diamond, OverlayPatternKind.Double, 125, 118, 0),
            new OverlayStyleDefinition(OverlaySemanticKind.PreviewValid, OverlayShapeKind.Outline, OverlayPatternKind.Solid, 100, 100, 0),
            new OverlayStyleDefinition(OverlaySemanticKind.PreviewInvalid, OverlayShapeKind.Cross, OverlayPatternKind.CrossHatch, 125, 105, 8),
            new OverlayStyleDefinition(OverlaySemanticKind.JobAvailable, OverlayShapeKind.Ring, OverlayPatternKind.Solid, 100, 100, 0),
            new OverlayStyleDefinition(OverlaySemanticKind.JobClaimed, OverlayShapeKind.Chevron, OverlayPatternKind.Solid, 105, 104, 0),
            new OverlayStyleDefinition(OverlaySemanticKind.JobInProgress, OverlayShapeKind.Diamond, OverlayPatternKind.Double, 110, 108, 0),
            new OverlayStyleDefinition(OverlaySemanticKind.JobBlocked, OverlayShapeKind.Cross, OverlayPatternKind.CrossHatch, 120, 94, 12),
            new OverlayStyleDefinition(OverlaySemanticKind.JobAttention, OverlayShapeKind.Diamond, OverlayPatternKind.Dashed, 125, 112, 12),
            new OverlayStyleDefinition(OverlaySemanticKind.JobTerminal, OverlayShapeKind.Ring, OverlayPatternKind.Dashed, 75, 82, 0),
            new OverlayStyleDefinition(OverlaySemanticKind.Route, OverlayShapeKind.Line, OverlayPatternKind.Dashed, 100, 100, 0),
        };
}
}
