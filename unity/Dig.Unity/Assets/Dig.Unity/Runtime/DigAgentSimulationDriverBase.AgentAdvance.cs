using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Presentation.Agents;

namespace Dig.Unity
{
    public abstract partial class DigAgentSimulationDriverBase
    {
        private Result AdvanceTerrainForAgents(
            long tick,
            IReadOnlyList<AgentViewModel> agents)
        {
            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
            }

            for (int index = 0; index < agents.Count; index++)
            {
                AgentViewModel agent = agents[index];
                long effectiveTick = TerrainSession!.ResolveTerrainAdvanceTick(
                    agent.Id,
                    tick);
                Result result = TerrainSession.Advance(
                    effectiveTick,
                    new[] { agent });
                if (result.IsFailure)
                {
                    return result;
                }
            }

            return Result.Success();
        }
    }
}
