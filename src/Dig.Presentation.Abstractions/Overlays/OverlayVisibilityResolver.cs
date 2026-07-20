using System;
using System.Collections.Generic;
using System.Linq;

namespace Dig.Presentation.Overlays
{

public sealed partial class OverlayVisibilityResolver
{
    private readonly Dictionary<OverlayLayerKind, OverlayLayerDefinition> _layers;

    public OverlayVisibilityResolver(IEnumerable<OverlayLayerDefinition> layers)
    {
        if (layers is null)
        {
            throw new ArgumentNullException(nameof(layers));
        }

        _layers = layers.ToDictionary(value => value.Layer);
        if (_layers.Count != Enum.GetValues(typeof(OverlayLayerKind)).Length)
        {
            throw new ArgumentException("Every overlay layer requires one definition.", nameof(layers));
        }
    }

    public OverlayLayerDefinition ResolveLayer(OverlayLayerKind layer)
    {
        if (!_layers.TryGetValue(layer, out OverlayLayerDefinition? definition))
        {
            throw new ArgumentOutOfRangeException(nameof(layer));
        }

        return definition;
    }
}
}
