using System;
using Dig.Application.Messaging;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Application.Agents
{

public sealed class MoveAgentThroughTunnelCommand : ICommand<MoveAgentThroughTunnelReport>
{
    public MoveAgentThroughTunnelCommand(
        EntityId agentId,
        SpatialCellId destination,
        long tick)
    {
        AgentId = agentId;
        Destination = destination;
        Tick = tick;
    }

    public EntityId AgentId { get; }

    public SpatialCellId Destination { get; }

    public long Tick { get; }
}

public sealed class MoveAgentThroughTunnelReport
{
    public MoveAgentThroughTunnelReport(Result result, TunnelPath? path)
    {
        Result = result;
        Path = path;
    }

    public Result Result { get; }

    public TunnelPath? Path { get; }
}

public sealed class MoveAgentThroughTunnelCommandHandler :
    ICommandHandler<MoveAgentThroughTunnelCommand, MoveAgentThroughTunnelReport>
{
    private readonly IAgentRepository _repository;
    private readonly TunnelNavigationVolume _volume;
    private readonly IEventSink _eventSink;

    public MoveAgentThroughTunnelCommandHandler(
        IAgentRepository repository,
        TunnelNavigationVolume volume,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _volume = volume ?? throw new ArgumentNullException(nameof(volume));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public MoveAgentThroughTunnelReport Handle(MoveAgentThroughTunnelCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        AgentState? agent = _repository.Get(command.AgentId);
        if (agent is null)
        {
            return Failure(AgentApplicationErrors.NotFound);
        }

        TunnelPathResult pathResult = _volume.FindPath(
            agent.SpatialPosition,
            command.Destination);
        if (!pathResult.Succeeded)
        {
            return Failure(new DomainError(
                $"agents.tunnel.{pathResult.FailureReason.ToString().ToLowerInvariant()}",
                pathResult.Detail));
        }

        Result movement = agent.MoveTo(command.Destination, command.Tick);
        if (movement.IsFailure)
        {
            return new MoveAgentThroughTunnelReport(movement, null);
        }

        _repository.Save(agent);
        _eventSink.Append(agent.DequeueUncommittedEvents());
        return new MoveAgentThroughTunnelReport(
            Result.Success(),
            pathResult.Path);
    }

    private static MoveAgentThroughTunnelReport Failure(DomainError error)
    {
        return new MoveAgentThroughTunnelReport(Result.Failure(error), null);
    }
}

}
