using System.Collections.ObjectModel;
using Dig.Domain.Agents;
using Dig.Domain.Core;

namespace Dig.Application.Agents;

public static class AgentApplicationErrors
{
    public static readonly DomainError AlreadyExists = new DomainError(
        "agents.repository.already_exists",
        "An agent with the same id is already registered.");

    public static readonly DomainError NotFound = new DomainError(
        "agents.repository.not_found",
        "The requested agent is not registered.");
}

public interface IAgentRepository
{
    Result Add(AgentState agent);

    AgentState? Get(EntityId agentId);

    IReadOnlyList<AgentState> GetAll();

    void Save(AgentState agent);
}

public interface IAgentDecisionContextProvider
{
    AgentDecisionContext GetContext(AgentSnapshot agent, long tick);
}

public readonly struct AgentTickDecision
{
    public AgentTickDecision(EntityId agentId, AgentDecision decision)
    {
        AgentId = agentId;
        Decision = decision ?? throw new ArgumentNullException(nameof(decision));
    }

    public EntityId AgentId { get; }

    public AgentDecision Decision { get; }
}

public sealed class AgentTickReport
{
    public AgentTickReport(long tick, IReadOnlyCollection<AgentTickDecision> decisions)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (decisions is null)
        {
            throw new ArgumentNullException(nameof(decisions));
        }

        Tick = tick;
        Decisions = new ReadOnlyCollection<AgentTickDecision>(
            decisions.OrderBy(item => item.AgentId.ToString(), StringComparer.Ordinal).ToArray());
    }

    public long Tick { get; }

    public IReadOnlyList<AgentTickDecision> Decisions { get; }
}
