using System;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;

namespace Dig.Application.Saving
{

public sealed class LoadedAgentSkillProgressionRestorer
{
    private readonly IAgentRepository _agents;

    public LoadedAgentSkillProgressionRestorer(IAgentRepository agents)
    {
        _agents = agents ?? throw new ArgumentNullException(nameof(agents));
    }

    public Result Restore(LoadedGameState loaded)
    {
        if (loaded is null)
        {
            throw new ArgumentNullException(nameof(loaded));
        }

        foreach (System.Collections.Generic.KeyValuePair<
            EntityId,
            AgentSkillProgressionSnapshot> entry in loaded.AgentSkills)
        {
            if (_agents.Get(entry.Key) is null)
            {
                return Result.Failure(AgentApplicationErrors.NotFound);
            }
        }

        foreach (System.Collections.Generic.KeyValuePair<EntityId, bool> entry
            in loaded.AgentAutomaticPlanning)
        {
            if (_agents.Get(entry.Key) is null)
            {
                return Result.Failure(AgentApplicationErrors.NotFound);
            }
        }

        foreach (System.Collections.Generic.KeyValuePair<
            EntityId,
            AgentSkillProgressionSnapshot> entry in loaded.AgentSkills)
        {
            AgentState agent = _agents.Get(entry.Key)!;
            Result restored = agent.RestoreSkillProgression(entry.Value);
            if (restored.IsFailure)
            {
                return restored;
            }

            _agents.Save(agent);
        }

        foreach (System.Collections.Generic.KeyValuePair<EntityId, bool> entry
            in loaded.AgentAutomaticPlanning)
        {
            AgentState agent = _agents.Get(entry.Key)!;
            agent.RestoreAutomaticPlanningEnabled(entry.Value);
            _agents.Save(agent);
        }

        return Result.Success();
    }
}

}
