namespace Dig.Presentation.Overlays
{

public enum OverlayLayerKind
{
    Hover = 0,
    Selection = 1,
    Preview = 2,
    Designation = 3,
    Jobs = 4,
    Reservations = 5,
    Routes = 6,
    Diagnostics = 7,
}

public enum OverlaySemanticKind
{
    Hover = 0,
    Selection = 1,
    PreviewValid = 2,
    PreviewInvalid = 3,
    Designation = 4,
    JobAvailable = 5,
    JobClaimed = 6,
    JobInProgress = 7,
    JobBlocked = 8,
    JobAttention = 9,
    JobTerminal = 10,
    Route = 11,
    Reservation = 12,
    Diagnostic = 13,
}

public enum OverlayShapeKind
{
    Ring = 0,
    Diamond = 1,
    Cross = 2,
    Chevron = 3,
    Outline = 4,
    Line = 5,
}

public enum OverlayPatternKind
{
    Solid = 0,
    Dashed = 1,
    CrossHatch = 2,
    Double = 3,
}

public enum OverlayVisibilityProfile
{
    Release = 0,
    Debug = 1,
    All = 2,
}
}
