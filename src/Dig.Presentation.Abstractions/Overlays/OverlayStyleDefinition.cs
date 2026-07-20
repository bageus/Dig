namespace Dig.Presentation.Overlays
{

public sealed class OverlayStyleDefinition
{
    public OverlayStyleDefinition(
        OverlaySemanticKind semantic,
        OverlayShapeKind shape,
        OverlayPatternKind pattern,
        int widthPercent,
        int scalePercent,
        int tiltDegrees)
    {
        Semantic = semantic;
        Shape = shape;
        Pattern = pattern;
        WidthPercent = widthPercent;
        ScalePercent = scalePercent;
        TiltDegrees = tiltDegrees;
    }

    public OverlaySemanticKind Semantic { get; }
    public OverlayShapeKind Shape { get; }
    public OverlayPatternKind Pattern { get; }
    public int WidthPercent { get; }
    public int ScalePercent { get; }
    public int TiltDegrees { get; }
}
}
