using System;
using Dig.Application.Messaging;
using Dig.Domain.Agents;
using Dig.Domain.Core;

namespace Dig.Application.Agents
{

public sealed class SetAgentAutomaticPlanningCommand : ICommand<Result>
{
    public SetAgentAutomaticPlanningCommand(EntityId agentId, bool enabled, long tick)
    {
        AgentId = agentId;
        Enabled = enabled;
        Tick = tick;
    }

    public EntityId AgentId { get; }

    public bool Enabled { get; }

    public long Tick { get; }
}

public sealed class SetAgentAutomaticPlanningCommandHandler
    : ICommandHandler<SetAgentAutomaticPlanningCommand, Result>
{
    private readonly IAgentRepository _repository;
    private readonly IEventSink _eventSink;

    public SetAgentAutomaticPlanningCommandHandler(
        IAgentRepository repository,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(SetAgentAutomaticPlanningCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        AgentState? agent = _repository.Get(command.AgentId);
        if (agent is null)
        {
            return Result.Failure(AgentApplicationErrors.NotFound);
        }

        Result result = agent.SetAutomaticPlanningEnabled(command.Enabled, command.Tick);
        if (result.IsFailure)
        {
            return result;
        }

        _repository.Save(agent);
        _eventSink.Append(agent.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class SetAgentWorkRestWindowCommand : ICommand<Result>
{
    public SetAgentWorkRestWindowCommand(
        EntityId agentId,
        int workStartTickInclusive,
        int workEndTickExclusive,
        long tick)
    {
        AgentId = agentId;
        WorkStartTickInclusive = workStartTickInclusive;
        WorkEndTickExclusive = workEndTickExclusive;
        Tick = tick;
    }

    public EntityId AgentId { get; }

    public int WorkStartTickInclusive { get; }

    public int WorkEndTickExclusive { get; }

    public long Tick { get; }
}

public sealed class SetAgentWorkRestWindowCommandHandler
    : ICommandHandler<SetAgentWorkRestWindowCommand, Result>
{
    private readonly IAgentRepository _repository;
    private readonly IEventSink _eventSink;

    public SetAgentWorkRestWindowCommandHandler(
        IAgentRepository repository,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(SetAgentWorkRestWindowCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        AgentState? agent = _repository.Get(command.AgentId);
        if (agent is null)
        {
            return Result.Failure(AgentApplicationErrors.NotFound);
        }

        Result result = agent.SetWorkRestWindow(
            command.WorkStartTickInclusive,
            command.WorkEndTickExclusive,
            command.Tick);
        if (result.IsFailure)
        {
            return result;
        }

        _repository.Save(agent);
        _eventSink.Append(agent.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}
