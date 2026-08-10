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

            DomainError? firstError = null;
            for (int index = 0; index < agents.Count; index++)
            {
                AgentViewModel agent = agents[index];
                long effectiveTick = TerrainSession!.ResolveTerrainAdvanceTick(
                    agent.Id,
                    tick);
                Result result = TerrainSession.Advance(
                    effectiveTick,
                    new[] { agent });
                if (result.IsFailure && firstError == null)
                {
                    firstError = result.Error;
                }
            }

            TerrainSession!.ReconcileChangedTerrain(tick, agents);
            return firstError == null
                ? Result.Success()
                : Result.Failure(firstError!);
        }
    }
}
