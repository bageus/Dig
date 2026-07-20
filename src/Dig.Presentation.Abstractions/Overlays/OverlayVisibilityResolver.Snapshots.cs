using System.Collections.Generic;
using System.Linq;

namespace Dig.Presentation.Overlays
{

public sealed partial class OverlayVisibilityResolver
{
    public static OverlayVisibilityResolver CreateDefault()
    {
        return new OverlayVisibilityResolver(DefaultOverlayDefinitions.Layers);
    }

    public OverlayVisibilitySnapshot CreateSnapshot(
        OverlayVisibilityProfile profile,
        IReadOnlyDictionary<OverlayLayerKind, bool>? visibilityOverrides = null)
    {
        OverlayLayerKind[] visible = _layers.Values
            .Where(value => IsVisible(profile, value, visibilityOverrides))
            .OrderByDescending(value => value.Priority)
            .ThenBy(value => value.Layer)
            .Select(value => value.Layer)
            .ToArray();
        long version = ((long)profile * 1000L)
            + visible.Sum(value => ((int)value + 1) * 17L);
        return new OverlayVisibilitySnapshot(profile, visible, version);
    }

    private static bool IsVisible(
        OverlayVisibilityProfile profile,
        OverlayLayerDefinition definition,
        IReadOnlyDictionary<OverlayLayerKind, bool>? visibilityOverrides)
    {
        if (profile == OverlayVisibilityProfile.Release && definition.DebugOnly)
        {
            return false;
        }

        bool visible = profile == OverlayVisibilityProfile.All
            || definition.DefaultVisible;
        return visibilityOverrides != null
            && visibilityOverrides.TryGetValue(definition.Layer, out bool value)
                ? value
                : visible;
    }
}
}
