using System;
using Dig.Application.Messaging;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Application.Agents
{

public sealed class MoveAgentCommand : ICommand<Result>
{
    public MoveAgentCommand(EntityId agentId, CellId targetPosition, long tick)
    {
        AgentId = agentId;
        TargetPosition = targetPosition;
        Tick = tick;
    }

    public EntityId AgentId { get; }

    public CellId TargetPosition { get; }

    public long Tick { get; }
}

public sealed class MoveAgentOnSurfaceCommand : ICommand<Result>
{
    public MoveAgentOnSurfaceCommand(EntityId agentId, SurfacePose targetPose, long tick)
    {
        AgentId = agentId;
        TargetPose = targetPose;
        Tick = tick;
    }

    public EntityId AgentId { get; }
    public SurfacePose TargetPose { get; }
    public long Tick { get; }
}

public sealed class MoveAgentOnSurfaceCommandHandler :
    ICommandHandler<MoveAgentOnSurfaceCommand, Result>
{
    private readonly IAgentRepository _repository;
    private readonly IEventSink _eventSink;

    public MoveAgentOnSurfaceCommandHandler(
        IAgentRepository repository,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(MoveAgentOnSurfaceCommand command)
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

        Result result = agent.MoveOnSurface(command.TargetPose, command.Tick);
        if (result.IsFailure)
        {
            return result;
        }

        _repository.Save(agent);
        _eventSink.Append(agent.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class MoveAgentCommandHandler : ICommandHandler<MoveAgentCommand, Result>
{
    private readonly IAgentRepository _repository;
    private readonly IEventSink _eventSink;

    public MoveAgentCommandHandler(IAgentRepository repository, IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(MoveAgentCommand command)
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

        Result result = agent.MoveTo(command.TargetPosition, command.Tick);
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
