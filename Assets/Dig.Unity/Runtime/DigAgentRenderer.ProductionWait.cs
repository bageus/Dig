using System;
using System.Collections.Generic;

namespace Dig.Unity
{

public sealed partial class DigAgentRenderer
{
    internal void SynchronizeProductionWaitOffsets(
        IReadOnlyDictionary<string, float> offsets)
    {
        if (offsets == null)
        {
            throw new ArgumentNullException(nameof(offsets));
        }

        foreach (KeyValuePair<string, DigAgentVisual> pair in _agents)
        {
            bool active = offsets.TryGetValue(pair.Key, out float offsetX);
            pair.Value.SetProductionWaitPose(active, offsetX);
        }
    }
}

}
