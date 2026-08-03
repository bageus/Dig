using System;
using System.Collections.Generic;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    internal Result AdvanceMovementSubstep(
        IReadOnlyDictionary<string, CellId> movementTargets)
    {
        if (movementTargets == null)
        {
            throw new ArgumentNullException(nameof(movementTargets));
        }

        IReadOnlyList<AgentState> agents = _repository.GetAll();
        for (int index = 0; index < agents.Count; index++)
        {
            AgentState agent = agents[index];
            if (!agent.IsAlive
                || !_residentSexes.ContainsKey(agent.Id)
                || GetCombatIntent(agent.Id) != null
                || SkipNormalMovement(agent))
            {
                continue;
            }

            if (TryAdvanceManualTunnelMovement(agent, out Result manualMovement))
            {
                if (manualMovement.IsFailure)
                {
                    CancelManualMovementWithWarning(agent.Id, manualMovement.Error!);
                }

                continue;
            }

            if (TryAdvanceSpatialWorkMovement(agent, out Result spatialMovement))
            {
                if (spatialMovement.IsFailure)
                {
                    CancelManualMovementWithWarning(agent.Id, spatialMovement.Error!);
                }

                continue;
            }

            if (movementTargets.TryGetValue(agent.Id.ToString(), out CellId destination))
            {
                TryAdvanceAutomaticMovement(agent, destination);
            }
        }

        return Result.Success();
    }
}

}
