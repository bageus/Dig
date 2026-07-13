using System.Collections.ObjectModel;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;

namespace Dig.Infrastructure.InMemory;

public sealed class InMemoryAgentRepository : IAgentRepository
{
    private readonly Dictionary<EntityId, AgentState> _agents =
        new Dictionary<EntityId, AgentState>();

    public Result Add(AgentState agent)
    {
        if (agent is null)
        {
            throw new ArgumentNullException(nameof(agent));
        }

        if (!_agents.TryAdd(agent.Id, agent))
        {
            return Result.Failure(AgentApplicationErrors.AlreadyExists);
        }

        return Result.Success();
    }

    public AgentState? Get(EntityId agentId)
    {
        return _agents.TryGetValue(agentId, out AgentState? agent) ? agent : null;
    }

    public IReadOnlyList<AgentState> GetAll()
    {
        AgentState[] agents = _agents.Values
            .OrderBy(agent => agent.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        return new ReadOnlyCollection<AgentState>(agents);
    }

    public void Save(AgentState agent)
    {
        if (agent is null)
        {
            throw new ArgumentNullException(nameof(agent));
        }

        if (!_agents.ContainsKey(agent.Id))
        {
            throw new InvalidOperationException(
                $"Agent '{agent.Id}' must be added before it can be saved.");
        }

        _agents[agent.Id] = agent;
    }
}
