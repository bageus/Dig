using System;

namespace Dig.Presentation.Overlays
{

public sealed class OverlayLayerDefinition
{
    public OverlayLayerDefinition(
        OverlayLayerKind layer,
        int priority,
        bool debugOnly,
        bool defaultVisible,
        int toggleSlot)
    {
        if (!Enum.IsDefined(typeof(OverlayLayerKind), layer)
            || priority < 0
            || priority > 1000
            || toggleSlot < 0
            || toggleSlot > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(layer));
        }

        Layer = layer;
        Priority = priority;
        DebugOnly = debugOnly;
        DefaultVisible = defaultVisible;
        ToggleSlot = toggleSlot;
    }

    public OverlayLayerKind Layer { get; }
    public int Priority { get; }
    public bool DebugOnly { get; }
    public bool DefaultVisible { get; }
    public int ToggleSlot { get; }
}
}
