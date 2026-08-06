using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Tunnels;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;

namespace Dig.Unity
{
internal sealed partial class DigTerrainWorkSession
{
    private CreateTunnelManualReinforcementHandler? _buildingManualReinforcementCreate;
    private CreateTunnelManualReinforcementHandler? _terrainManualReinforcementCreate;
    private CompleteTunnelManualReinforcementHandler? _buildingManualReinforcementComplete;
    private CompleteTunnelManualReinforcementHandler? _terrainManualReinforcementComplete;
    private CancelTunnelManualReinforcementHandler? _buildingManualReinforcementCancel;
    private CancelTunnelManualReinforcementHandler? _terrainManualReinforcementCancel;
    private readonly Dictionary<EntityId, ManualReinforcementRoutePlan>
        _manualReinforcementRoutes =
            new Dictionary<EntityId, ManualReinforcementRoutePlan>();
    private long _nextManualReinforcementSequence;

    internal Result<TunnelManualReinforcementPlan> ValidateTunnelManualReinforcement(
        string residentId,
        string stackId,
        CellId target)
    {
        EnsureManualTunnelReinforcementRuntime();
        EntityId resident = EntityId.Parse(residentId);
        EntityId stack = EntityId.Parse(stackId);
        InMemoryInventoryRepository? repository = ResolveManualReinforcementRepository(stack);
        ItemStackSnapshot? source = repository?.Get().GetStack(stack);
        if (source == null)
        {
            return Result<TunnelManualReinforcementPlan>.Failure(
                TunnelManualReinforcementErrors.SourceUnavailable);
        }

        Result<TunnelManualReinforcementPlan> plan =
            TunnelManualReinforcementPlanner.Resolve(
                _tunnelInfrastructure!.Get().CaptureSnapshot(),
                source.ItemId,
                target);
        if (plan.IsFailure)
        {
            return plan;
        }

        Result sourceValidation = CreateTunnelManualReinforcementHandler.ValidateSource(
            source,
            resident,
            plan.Value.Kind);
        if (sourceValidation.IsFailure)
        {
            return Result<TunnelManualReinforcementPlan>.Failure(
                sourceValidation.Error!);
        }

        AgentState? actor = _productionAgents?.Get(resident);
        if (actor == null)
        {
            return Result<TunnelManualReinforcementPlan>.Failure(
                TunnelManualReinforcementErrors.SourceUnavailable);
        }

        if (!TryLoadBuildingPlacementNavigation(out NavigationSnapshot navigation))
        {
            return Result<TunnelManualReinforcementPlan>.Failure(
                TunnelManualReinforcementErrors.TargetUnavailable);
        }

        PathResult path = new NavigationPathfinder().FindPath(
            navigation,
            new PathRequest(actor.Position, target, navigation.NavigationVersion));
        return path.Succeeded
            ? plan
            : Result<TunnelManualReinforcementPlan>.Failure(
                TunnelManualReinforcementErrors.TargetUnavailable);
    }

    internal Result CreateTunnelManualReinforcement(
        string residentId,
        string stackId,
        CellId target,
        long tick)
    {
        Result<TunnelManualReinforcementPlan> validated =
            ValidateTunnelManualReinforcement(residentId, stackId, target);
        if (validated.IsFailure)
        {
            return Result.Failure(validated.Error!);
        }

        Result prepared = PrepareResidentsForDirectCommand(
            new[] { residentId },
            tick);
        if (prepared.IsFailure)
        {
            return prepared;
        }

        EntityId stack = EntityId.Parse(stackId);
        InMemoryInventoryRepository? repository = ResolveManualReinforcementRepository(stack);
        if (repository == null)
        {
            return Result.Failure(TunnelManualReinforcementErrors.SourceUnavailable);
        }

        long sequence = checked(++_nextManualReinforcementSequence);
        EntityId jobId = DemoId('r', sequence);
        CreateTunnelManualReinforcementHandler handler = ReferenceEquals(
            repository,
            _buildingInventoryRepository)
                ? _buildingManualReinforcementCreate!
                : _terrainManualReinforcementCreate!;
        Result created = handler.Handle(new CreateTunnelManualReinforcementCommand(
            jobId,
            EntityId.Parse(residentId),
            stack,
            validated.Value,
            tick));
        if (created.IsSuccess)
        {
            PublishTunnelInfrastructureVisuals();
        }

        return created;
    }

    private bool TryPlanTunnelManualReinforcementMovement(
        JobSnapshot job,
        AgentViewModel agent,
        NavigationSnapshot navigation,
        IDictionary<string, CellId> movement,
        long tick)
    {
        if (job.Definition is not TunnelManualReinforcementJobDefinition definition)
        {
            return false;
        }

        PathResult path = new NavigationPathfinder().FindPath(
            navigation,
            new PathRequest(
                new CellId(agent.CellX, agent.CellY, agent.CellZ),
                definition.TargetCell,
                navigation.NavigationVersion));
        _manualReinforcementRoutes[job.Id] =
            new ManualReinforcementRoutePlan(definition.TargetCell, path);
        if (!path.Succeeded || path.Path == null)
        {
            CancelTunnelManualReinforcement(job, "route_unavailable", tick);
            return true;
        }

        movement[agent.Id] = path.Path.Cells.Count > 1
            ? path.Path.Cells[1]
            : definition.TargetCell;
        return true;
    }

    private Result AdvanceTunnelManualReinforcement(
        JobSnapshot job,
        AgentViewModel agent,
        long tick)
    {
        if (job.Definition is not TunnelManualReinforcementJobDefinition definition)
        {
            return Result.Success();
        }

        CellId currentCell = new CellId(agent.CellX, agent.CellY, agent.CellZ);
        if (currentCell != definition.TargetCell)
        {
            return Result.Success();
        }

        for (int index = 0; index < 4; index++)
        {
            JobSnapshot? current = _jobRepository.Get().Get(job.Id);
            if (current == null || current.IsTerminal)
            {
                _manualReinforcementRoutes.Remove(job.Id);
                return Result.Success();
            }

            if (current.Stage == JobStageKind.Finalize)
            {
                Result completed = CompleteTunnelManualReinforcement(current, tick);
                if (completed.IsSuccess)
                {
                    _manualReinforcementRoutes.Remove(current.Id);
                    PublishTunnelInfrastructureVisuals();
                }

                return completed;
            }

            Result advanced = _advanceHandler.Handle(
                new AdvanceJobCommand(current.Id, tick));
            if (advanced.IsFailure)
            {
                return advanced;
            }
        }

        return Result.Failure(JobErrors.InvalidStatus);
    }

    private Result CompleteTunnelManualReinforcement(JobSnapshot job, long tick)
    {
        TunnelManualReinforcementJobDefinition definition =
            (TunnelManualReinforcementJobDefinition)job.Definition;
        InMemoryInventoryRepository? repository =
            ResolveManualReinforcementRepository(definition.SourceStackId);
        CompleteTunnelManualReinforcementHandler? handler = ReferenceEquals(
            repository,
            _buildingInventoryRepository)
                ? _buildingManualReinforcementComplete
                : _terrainManualReinforcementComplete;
        return repository == null || handler == null
            ? Result.Failure(TunnelManualReinforcementErrors.SourceUnavailable)
            : handler.Handle(new CompleteTunnelManualReinforcementCommand(
                job.Id,
                tick));
    }

    private Result CancelTunnelManualReinforcement(
        JobSnapshot job,
        string reason,
        long tick)
    {
        if (job.Definition is not TunnelManualReinforcementJobDefinition definition)
        {
            return Result.Failure(TunnelManualReinforcementErrors.JobMismatch);
        }

        InMemoryInventoryRepository? repository =
            ResolveManualReinforcementRepository(definition.SourceStackId);
        CancelTunnelManualReinforcementHandler? handler = ReferenceEquals(
            repository,
            _buildingInventoryRepository)
                ? _buildingManualReinforcementCancel
                : _terrainManualReinforcementCancel;
        Result result = repository == null || handler == null
            ? Result.Failure(TunnelManualReinforcementErrors.SourceUnavailable)
            : handler.Handle(new CancelTunnelManualReinforcementCommand(
                job.Id,
                reason,
                tick));
        if (result.IsSuccess)
        {
            _manualReinforcementRoutes.Remove(job.Id);
        }

        return result;
    }

    private InMemoryInventoryRepository? ResolveManualReinforcementRepository(
        EntityId stackId)
    {
        if (_buildingInventoryRepository?.Get().GetStack(stackId) != null)
        {
            return _buildingInventoryRepository;
        }

        return _inventoryRepository.Get().GetStack(stackId) != null
            ? _inventoryRepository
            : null;
    }

    private void EnsureManualTunnelReinforcementRuntime()
    {
        EnsureTunnelInfrastructureRuntime();
        if (_buildingManualReinforcementCreate != null)
        {
            return;
        }

        if (_buildingInventoryRepository == null)
        {
            throw new InvalidOperationException(
                "Manual tunnel reinforcement requires building inventory state.");
        }

        _buildingManualReinforcementCreate = new CreateTunnelManualReinforcementHandler(
            _tunnelInfrastructure!, _buildingInventoryRepository, _jobRepository, _journal);
        _terrainManualReinforcementCreate = new CreateTunnelManualReinforcementHandler(
            _tunnelInfrastructure!, _inventoryRepository, _jobRepository, _journal);
        _buildingManualReinforcementComplete = new CompleteTunnelManualReinforcementHandler(
            _tunnelInfrastructure!, _buildingInventoryRepository, _jobRepository, _journal,
            _skillGrants);
        _terrainManualReinforcementComplete = new CompleteTunnelManualReinforcementHandler(
            _tunnelInfrastructure!, _inventoryRepository, _jobRepository, _journal,
            _skillGrants);
        _buildingManualReinforcementCancel = new CancelTunnelManualReinforcementHandler(
            _buildingInventoryRepository, _jobRepository, _journal);
        _terrainManualReinforcementCancel = new CancelTunnelManualReinforcementHandler(
            _inventoryRepository, _jobRepository, _journal);
    }

    private sealed class ManualReinforcementRoutePlan
    {
        internal ManualReinforcementRoutePlan(CellId target, PathResult path)
        {
            Target = target;
            Path = path;
        }

        internal CellId Target { get; }
        internal PathResult Path { get; }
    }
}
}
