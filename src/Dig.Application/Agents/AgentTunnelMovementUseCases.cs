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

public sealed class PlanAgentTunnelRouteCommand : ICommand<PlanAgentTunnelRouteReport>
{
    public PlanAgentTunnelRouteCommand(EntityId agentId, CellId destination)
    {
        AgentId = agentId;
        Destination = destination;
    }

    public EntityId AgentId { get; }

    public CellId Destination { get; }
}

public sealed class PlanAgentTunnelRouteReport
{
    public PlanAgentTunnelRouteReport(Result result, TunnelPath? path)
    {
        Result = result;
        Path = path;
    }

    public Result Result { get; }

    public TunnelPath? Path { get; }
}

public sealed class PlanAgentTunnelRouteCommandHandler :
    ICommandHandler<PlanAgentTunnelRouteCommand, PlanAgentTunnelRouteReport>
{
    private readonly IAgentRepository _repository;
    private readonly TunnelNavigationVolume _volume;

    public PlanAgentTunnelRouteCommandHandler(
        IAgentRepository repository,
        TunnelNavigationVolume volume)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _volume = volume ?? throw new ArgumentNullException(nameof(volume));
    }

    public PlanAgentTunnelRouteReport Handle(PlanAgentTunnelRouteCommand command)
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

        if (!agent.IsAlive)
        {
            return Failure(AgentErrors.AgentDead);
        }

        TunnelPathResult pathResult = _volume.FindPath(
            agent.Position,
            command.Destination);
        return pathResult.Succeeded
            ? new PlanAgentTunnelRouteReport(Result.Success(), pathResult.Path)
            : Failure(ToDomainError(pathResult));
    }

    private static PlanAgentTunnelRouteReport Failure(DomainError error)
    {
        return new PlanAgentTunnelRouteReport(Result.Failure(error), null);
    }

    internal static DomainError ToDomainError(TunnelPathResult result)
    {
        return new DomainError(
            $"agents.tunnel.{result.FailureReason.ToString().ToLowerInvariant()}",
            result.Detail);
    }
}

public sealed class PlanAgentsTunnelRoutesCommand :
    ICommand<PlanAgentsTunnelRoutesReport>
{
    public PlanAgentsTunnelRoutesCommand(
        IReadOnlyCollection<EntityId> agentIds,
        CellId destination)
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
    }

    public IReadOnlyList<EntityId> AgentIds { get; }

    public CellId Destination { get; }
}

public sealed class PlannedAgentTunnelRoute
{
    public PlannedAgentTunnelRoute(EntityId agentId, TunnelPath path)
    {
        AgentId = agentId;
        Path = path ?? throw new ArgumentNullException(nameof(path));
    }

    public EntityId AgentId { get; }

    public TunnelPath Path { get; }
}

public sealed class PlanAgentsTunnelRoutesReport
{
    private PlanAgentsTunnelRoutesReport(
        Result result,
        IReadOnlyList<PlannedAgentTunnelRoute> entries)
    {
        Result = result;
        Entries = entries;
    }

    public Result Result { get; }

    public IReadOnlyList<PlannedAgentTunnelRoute> Entries { get; }

    internal static PlanAgentsTunnelRoutesReport Success(
        IReadOnlyList<PlannedAgentTunnelRoute> entries)
    {
        return new PlanAgentsTunnelRoutesReport(
            Result.Success(),
            new ReadOnlyCollection<PlannedAgentTunnelRoute>(entries.ToArray()));
    }

    internal static PlanAgentsTunnelRoutesReport Failure(DomainError error)
    {
        return new PlanAgentsTunnelRoutesReport(
            Result.Failure(error),
            Array.Empty<PlannedAgentTunnelRoute>());
    }
}

public sealed class PlanAgentsTunnelRoutesCommandHandler :
    ICommandHandler<PlanAgentsTunnelRoutesCommand, PlanAgentsTunnelRoutesReport>
{
    private readonly IAgentRepository _repository;
    private readonly TunnelNavigationVolume _volume;

    public PlanAgentsTunnelRoutesCommandHandler(
        IAgentRepository repository,
        TunnelNavigationVolume volume)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _volume = volume ?? throw new ArgumentNullException(nameof(volume));
    }

    public PlanAgentsTunnelRoutesReport Handle(PlanAgentsTunnelRoutesCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        List<PlannedAgentTunnelRoute> entries =
            new List<PlannedAgentTunnelRoute>(command.AgentIds.Count);
        for (int index = 0; index < command.AgentIds.Count; index++)
        {
            EntityId agentId = command.AgentIds[index];
            AgentState? agent = _repository.Get(agentId);
            if (agent is null)
            {
                return PlanAgentsTunnelRoutesReport.Failure(
                    AgentApplicationErrors.NotFound);
            }

            if (!agent.IsAlive)
            {
                return PlanAgentsTunnelRoutesReport.Failure(AgentErrors.AgentDead);
            }

            TunnelPathResult pathResult = _volume.FindPath(
                agent.Position,
                command.Destination);
            if (!pathResult.Succeeded)
            {
                return PlanAgentsTunnelRoutesReport.Failure(
                    PlanAgentTunnelRouteCommandHandler.ToDomainError(pathResult));
            }

            entries.Add(new PlannedAgentTunnelRoute(agentId, pathResult.Path!));
        }

        return PlanAgentsTunnelRoutesReport.Success(entries);
    }
}

}
