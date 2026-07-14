using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;

namespace Dig.Infrastructure.InMemory
{

public sealed class InMemoryAgentRepository : IAgentRepository
{
    private readonly Dictionary<EntityId, AgentState> _agents =
        new Dictionary<EntityId, AgentState>();
    private AgentState[]? _orderedAgents;
    private IReadOnlyList<AgentState>? _orderedView;

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

        _orderedAgents = null;
        _orderedView = null;
        return Result.Success();
    }

    public AgentState? Get(EntityId agentId)
    {
        return _agents.TryGetValue(agentId, out AgentState? agent) ? agent : null;
    }

    public IReadOnlyList<AgentState> GetAll()
    {
        if (_orderedView is not null)
        {
            return _orderedView;
        }

        _orderedAgents = _agents.Values
            .OrderBy(agent => agent.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        _orderedView = new ReadOnlyCollection<AgentState>(_orderedAgents);
        return _orderedView;
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
        if (_orderedAgents is null)
        {
            return;
        }

        for (int index = 0; index < _orderedAgents.Length; index++)
        {
            if (_orderedAgents[index].Id == agent.Id)
            {
                _orderedAgents[index] = agent;
                return;
            }
        }

        throw new InvalidOperationException(
            $"Cached agent '{agent.Id}' was not found in repository order.");
    }
}
}
