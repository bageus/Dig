using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dig.Presentation.Overlays
{

public sealed class OverlayVisibilitySnapshot
{
    public OverlayVisibilitySnapshot(
        OverlayVisibilityProfile profile,
        IReadOnlyCollection<OverlayLayerKind> visibleLayers,
        long version)
    {
        if (!Enum.IsDefined(typeof(OverlayVisibilityProfile), profile)
            || visibleLayers is null
            || version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(profile));
        }

        OverlayLayerKind[] ordered = visibleLayers
            .Distinct()
            .OrderBy(layer => layer)
            .ToArray();
        if (ordered.Any(layer => !Enum.IsDefined(typeof(OverlayLayerKind), layer)))
        {
            throw new ArgumentException("Overlay layers must be defined.", nameof(visibleLayers));
        }

        Profile = profile;
        VisibleLayers = new ReadOnlyCollection<OverlayLayerKind>(ordered);
        Version = version;
    }

    public OverlayVisibilityProfile Profile { get; }
    public IReadOnlyList<OverlayLayerKind> VisibleLayers { get; }
    public long Version { get; }

    public bool IsVisible(OverlayLayerKind layer)
    {
        return VisibleLayers.Contains(layer);
    }
}
}
