using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
            return Failure(ToDomainError(pathResult));
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

    internal static DomainError ToDomainError(TunnelPathResult result)
    {
        return new DomainError(
            $"agents.tunnel.{result.FailureReason.ToString().ToLowerInvariant()}",
            result.Detail);
    }
}

public sealed class MoveAgentsThroughTunnelCommand : ICommand<MoveAgentsThroughTunnelReport>
{
    public MoveAgentsThroughTunnelCommand(
        IReadOnlyCollection<EntityId> agentIds,
        SpatialCellId destination,
        long tick)
    {
        if (agentIds is null)
        {
            throw new ArgumentNullException(nameof(agentIds));
        }

        EntityId[] copied = agentIds.ToArray();
        if (copied.Length == 0)
        {
            throw new ArgumentException("At least one resident is required.", nameof(agentIds));
        }

        if (new HashSet<EntityId>(copied).Count != copied.Length)
        {
            throw new ArgumentException("Resident ids must be unique.", nameof(agentIds));
        }

        AgentIds = new ReadOnlyCollection<EntityId>(copied);
        Destination = destination;
        Tick = tick;
    }

    public IReadOnlyList<EntityId> AgentIds { get; }

    public SpatialCellId Destination { get; }

    public long Tick { get; }
}

public sealed class MoveAgentThroughTunnelEntry
{
    public MoveAgentThroughTunnelEntry(EntityId agentId, TunnelPath path)
    {
        AgentId = agentId;
        Path = path ?? throw new ArgumentNullException(nameof(path));
    }

    public EntityId AgentId { get; }

    public TunnelPath Path { get; }
}

public sealed class MoveAgentsThroughTunnelReport
{
    private MoveAgentsThroughTunnelReport(
        Result result,
        IReadOnlyList<MoveAgentThroughTunnelEntry> entries)
    {
        Result = result;
        Entries = entries;
    }

    public Result Result { get; }

    public IReadOnlyList<MoveAgentThroughTunnelEntry> Entries { get; }

    internal static MoveAgentsThroughTunnelReport Success(
        IReadOnlyList<MoveAgentThroughTunnelEntry> entries)
    {
        return new MoveAgentsThroughTunnelReport(
            Result.Success(),
            new ReadOnlyCollection<MoveAgentThroughTunnelEntry>(entries.ToArray()));
    }

    internal static MoveAgentsThroughTunnelReport Failure(DomainError error)
    {
        return new MoveAgentsThroughTunnelReport(
            Result.Failure(error),
            Array.Empty<MoveAgentThroughTunnelEntry>());
    }
}

public sealed class MoveAgentsThroughTunnelCommandHandler :
    ICommandHandler<MoveAgentsThroughTunnelCommand, MoveAgentsThroughTunnelReport>
{
    private readonly IAgentRepository _repository;
    private readonly TunnelNavigationVolume _volume;
    private readonly IEventSink _eventSink;

    public MoveAgentsThroughTunnelCommandHandler(
        IAgentRepository repository,
        TunnelNavigationVolume volume,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _volume = volume ?? throw new ArgumentNullException(nameof(volume));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public MoveAgentsThroughTunnelReport Handle(MoveAgentsThroughTunnelCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        List<AgentState> agents = new List<AgentState>(command.AgentIds.Count);
        List<MoveAgentThroughTunnelEntry> entries =
            new List<MoveAgentThroughTunnelEntry>(command.AgentIds.Count);
        for (int index = 0; index < command.AgentIds.Count; index++)
        {
            EntityId agentId = command.AgentIds[index];
            AgentState? agent = _repository.Get(agentId);
            if (agent is null)
            {
                return MoveAgentsThroughTunnelReport.Failure(
                    AgentApplicationErrors.NotFound);
            }

            if (!agent.IsAlive)
            {
                return MoveAgentsThroughTunnelReport.Failure(AgentErrors.AgentDead);
            }

            TunnelPathResult pathResult = _volume.FindPath(
                agent.SpatialPosition,
                command.Destination);
            if (!pathResult.Succeeded)
            {
                return MoveAgentsThroughTunnelReport.Failure(
                    MoveAgentThroughTunnelCommandHandler.ToDomainError(pathResult));
            }

            agents.Add(agent);
            entries.Add(new MoveAgentThroughTunnelEntry(agentId, pathResult.Path!));
        }

        for (int index = 0; index < agents.Count; index++)
        {
            Result movement = agents[index].MoveTo(command.Destination, command.Tick);
            if (movement.IsFailure)
            {
                return MoveAgentsThroughTunnelReport.Failure(movement.Error!);
            }
        }

        for (int index = 0; index < agents.Count; index++)
        {
            AgentState agent = agents[index];
            _repository.Save(agent);
            _eventSink.Append(agent.DequeueUncommittedEvents());
        }

        return MoveAgentsThroughTunnelReport.Success(entries);
    }
}

}
