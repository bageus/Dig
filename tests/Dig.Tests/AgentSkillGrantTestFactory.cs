using Dig.Application.Agents;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Infrastructure.InMemory;

namespace Dig.Tests
{

internal static class AgentSkillGrantTestFactory
{
    public static IAgentSkillGrantService Create(
        EntityId agentId,
        IEventSink events)
    {
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        Result added = agents.Add(AgentTestFactory.CreateAgent(id: agentId));
        if (added.IsFailure)
        {
            throw new System.InvalidOperationException(added.Error!.ToString());
        }

        return new AgentSkillGrantService(agents, events);
    }
}

}
