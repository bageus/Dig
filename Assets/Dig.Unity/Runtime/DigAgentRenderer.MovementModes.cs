using System;
using System.Collections.Generic;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

public sealed partial class DigAgentRenderer
{
    internal void RenderWithMovementModes(
        IReadOnlyList<AgentViewModel> agents,
        float movementDuration,
        IReadOnlyDictionary<string, ResidentMovementModeViewModel> movementModes)
    {
        if (movementModes == null)
        {
            throw new ArgumentNullException(nameof(movementModes));
        }

        HashSet<string> moving = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < agents.Count; index++)
        {
            AgentViewModel model = agents[index];
            if (_agents.TryGetValue(model.Id, out DigAgentVisual? visual)
                && (visual.Model.CellX != model.CellX
                    || visual.Model.CellY != model.CellY
                    || visual.Model.CellZ != model.CellZ))
            {
                moving.Add(model.Id);
            }
        }

        Render(agents, movementDuration);
        for (int index = 0; index < agents.Count; index++)
        {
            AgentViewModel model = agents[index];
            if (!_agents.TryGetValue(model.Id, out DigAgentVisual? visual))
            {
                continue;
            }

            movementModes.TryGetValue(
                model.Id,
                out ResidentMovementModeViewModel? movementMode);
            visual.ApplyMovementMode(movementMode, moving.Contains(model.Id));
        }
    }
}

}
