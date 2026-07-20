using System;
using Dig.Application.Messaging;
using Dig.Domain.Agents;
using Dig.Domain.Core;

namespace Dig.Application.Agents
{

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