using System;
using Dig.Application.Messaging;
using Dig.Domain.Agents;
using Dig.Domain.Core;

namespace Dig.Application.Agents
{

public sealed class ExpandAgentSkillCapacityCommand : ICommand<Result>
{
    public ExpandAgentSkillCapacityCommand(
        EntityId agentId,
        int completedUniversityUnits,
        long tick)
    {
        if (agentId.IsEmpty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(agentId));
        }

        if (completedUniversityUnits < 0
            || completedUniversityUnits
                > AgentSkillCatalog.UniversityCapacityUnits
                    - AgentSkillCatalog.BaseCapacityUnits)
        {
            throw new ArgumentOutOfRangeException(nameof(completedUniversityUnits));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        AgentId = agentId;
        CompletedUniversityUnits = completedUniversityUnits;
        Tick = tick;
    }

    public EntityId AgentId { get; }
    public int CompletedUniversityUnits { get; }
    public long Tick { get; }
}

public sealed class ExpandAgentSkillCapacityHandler
    : ICommandHandler<ExpandAgentSkillCapacityCommand, Result>
{
    private readonly IAgentRepository _agents;
    private readonly IEventSink _events;

    public ExpandAgentSkillCapacityHandler(IAgentRepository agents, IEventSink events)
    {
        _agents = agents ?? throw new ArgumentNullException(nameof(agents));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result Handle(ExpandAgentSkillCapacityCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        AgentState? agent = _agents.Get(command.AgentId);
        if (agent is null)
        {
            return Result.Failure(AgentApplicationErrors.NotFound);
        }

        int capacity = checked(
            AgentSkillCatalog.BaseCapacityUnits + command.CompletedUniversityUnits);
        int current = agent.CreateSkillProgressionSnapshot().TotalCapacityUnits;
        if (capacity <= current)
        {
            return Result.Success();
        }

        Result expanded = agent.ExpandTotalSkillCapacity(capacity, command.Tick);
        if (expanded.IsFailure)
        {
            return expanded;
        }

        _agents.Save(agent);
        _events.Append(agent.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}
