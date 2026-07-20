using System.Collections.Generic;

namespace Dig.Presentation.Overlays
{

public static partial class DefaultOverlayDefinitions
{
    public static IReadOnlyList<OverlayLayerDefinition> Layers { get; } =
        new[]
        {
            new OverlayLayerDefinition(OverlayLayerKind.Hover, 950, false, true, 0),
            new OverlayLayerDefinition(OverlayLayerKind.Selection, 900, false, true, 0),
            new OverlayLayerDefinition(OverlayLayerKind.Preview, 800, false, true, 0),
            new OverlayLayerDefinition(OverlayLayerKind.Reservations, 700, false, true, 0),
            new OverlayLayerDefinition(OverlayLayerKind.Designation, 600, false, true, 0),
            new OverlayLayerDefinition(OverlayLayerKind.Jobs, 500, false, true, 3),
            new OverlayLayerDefinition(OverlayLayerKind.Routes, 300, true, true, 4),
            new OverlayLayerDefinition(OverlayLayerKind.Diagnostics, 100, true, false, 0),
        };
}
}
