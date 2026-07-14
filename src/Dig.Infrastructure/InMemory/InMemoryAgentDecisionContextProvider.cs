using System;
using System.Collections.Generic;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;

namespace Dig.Infrastructure.InMemory
{

public sealed class InMemoryAgentDecisionContextProvider
    : IAgentDecisionContextProvider
{
    private readonly Dictionary<EntityId, AgentDecisionContext> _contexts =
        new Dictionary<EntityId, AgentDecisionContext>();
    private AgentDecisionContext _defaultContext;

    public InMemoryAgentDecisionContextProvider(AgentDecisionContext defaultContext)
    {
        _defaultContext = defaultContext
            ?? throw new ArgumentNullException(nameof(defaultContext));
    }

    public void SetDefault(AgentDecisionContext context)
    {
        _defaultContext = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Set(EntityId agentId, AgentDecisionContext context)
    {
        if (agentId.IsEmpty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(agentId));
        }

        _contexts[agentId] = context ?? throw new ArgumentNullException(nameof(context));
    }

    public AgentDecisionContext GetContext(AgentSnapshot agent, long tick)
    {
        if (agent is null)
        {
            throw new ArgumentNullException(nameof(agent));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        return _contexts.TryGetValue(agent.Id, out AgentDecisionContext? context)
            ? context
            : _defaultContext;
    }
}
}
