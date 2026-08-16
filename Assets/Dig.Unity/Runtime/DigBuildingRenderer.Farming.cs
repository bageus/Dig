using System;
using System.Collections.Generic;
using Dig.Domain.Farming;

namespace Dig.Unity
{

public sealed partial class DigBuildingRenderer
{
    internal void RenderFarmContents(
        IReadOnlyDictionary<string, FarmSnapshot> farms)
    {
        if (farms == null) throw new ArgumentNullException(nameof(farms));
        foreach (KeyValuePair<string, DigBuildingVisual> pair in _buildings)
        {
            DigFarmVisualDecoration? decoration =
                pair.Value.GetComponent<DigFarmVisualDecoration>();
            if (decoration != null
                && farms.TryGetValue(pair.Key, out FarmSnapshot? snapshot))
            {
                decoration.SetState(snapshot);
            }
        }
    }
}

}
