using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Buildings;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using Dig.Presentation.Navigation;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private readonly Dictionary<EntityId, BuildingPackingRoutePlan> _buildingPackingRoutes =
        new Dictionary<EntityId, BuildingPackingRoutePlan>();
    private InMemoryJobCandidateProvider? _buildingPackingCandidates;
    private AssignAvailableJobsHandler? _buildingPackingAssignment;
    private AddBuildingBoxPackingWorkHandler? _buildingPackingWork;
    private CompleteBuildingBoxPackingHandler? _buildingPackingCompletion;
    private InventoryWorldPresenter? _buildingInventoryPresenter;
    private NavigationPathfinder? _buildingPackingPathfinder;

    private void InitializeBuildingPackingExecution(InMemoryExecutionJournal journal)
    {
        if (_buildingsRepository == null || _buildingInventoryRepository == null)
        {
            throw new InvalidOperationException("Building demo state must be initialized first.");
        }

        _packableBuildingExecutions ??= new PackableBuildingExecutionRegistry();
        _buildingPackingCandidates = new InMemoryJobCandidateProvider();
        _buildingPackingAssignment = new AssignAvailableJobsHandler(
            _jobRepository,
            _buildingPackingCandidates,
            journal);
        _buildingPackingWork = new AddBuildingBoxPackingWorkHandler(
            _buildingsRepository,
            _jobRepository,
            journal);
        _buildingPackingCompletion = new CompleteBuildingBoxPackingHandler(
            _buildingsRepository,
            _buildingInventoryRepository,
            _jobRepository,
            journal,
            _skillGrants);
        _buildingInventoryPresenter = new InventoryWorldPresenter(
            new GetInventorySnapshotQueryHandler(_buildingInventoryRepository));
        _buildingPackingPathfinder = new NavigationPathfinder();
    }

    public void SynchronizeBuildingPacking(
        long tick,
        IReadOnlyList<AgentViewModel> agents)
    {
        if (agents == null)
        {
            throw new ArgumentNullException(nameof(agents));
        }

        EnsureBuildingPackingExecutionInitialized();
        bool hasAvailablePacking = false;
        foreach (BuildingSnapshot building in _buildingsRepository!.Get().GetAll())
        {
            BuildingPackingPlanSnapshot? packing = building.PackingPlan;
            if (packing?.CommitState != BuildingPackingCommitState.Active)
            {
                continue;
            }

            JobSnapshot? job = _jobRepository.Get().Get(packing.JobId);
            if (job?.Status != JobStatus.Available
                || job.Definition is not BuildingBoxPackingJobDefinition definition)
            {
                continue;
            }

            _buildingPackingCandidates!.SetCandidates(
                job.Id,
                CreateBuildingPackingCandidates(agents, definition.WorkPosition));
            hasAvailablePacking = true;
        }

        if (hasAvailablePacking)
        {
            _buildingPackingAssignment!.Handle(new AssignAvailableJobsCommand(tick));
        }
    }

    public Result AdvanceBuildingPacking(
        long tick,
        IReadOnlyList<AgentViewModel> agents)
    {
        if (agents == null)
        {
            throw new ArgumentNullException(nameof(agents));
        }

        Dictionary<string, AgentViewModel> agentsById = agents.ToDictionary(
            agent => agent.Id,
            StringComparer.Ordinal);
        foreach (JobSnapshot job in _jobRepository.Get().GetAll())
        {
            if (!IsActive(job)
                || job.Definition is not BuildingBoxPackingJobDefinition
                || !job.AssignedAgentId.HasValue
                || !agentsById.TryGetValue(
                    job.AssignedAgentId.Value.ToString(),
                    out AgentViewModel? agent))
            {
                continue;
            }

            TryAdvanceBuildingPacking(job, agent, tick, out Result result);
            if (result.IsFailure)
            {
                return result;
            }
        }

        return Result.Success();
    }

    internal bool TryPlanBuildingPackingMovement(
        JobSnapshot job,
        AgentViewModel agent,
        NavigationSnapshot navigation,
        IDictionary<string, CellId> movement)
    {
        if (job.Definition is not BuildingBoxPackingJobDefinition packing)
        {
            return false;
        }

        EnsureBuildingPackingExecutionInitialized();
        CellId start = new CellId(agent.CellX, agent.CellY, agent.CellZ);
        PathResult path = _buildingPackingPathfinder!.FindPath(
            navigation,
            new PathRequest(start, packing.WorkPosition, navigation.NavigationVersion));
        _buildingPackingRoutes[job.Id] = new BuildingPackingRoutePlan(
            packing.WorkPosition,
            path);
        if (path.Succeeded)
        {
            movement[agent.Id] = path.Path!.Cells.Count > 1
                ? path.Path.Cells[1]
                : packing.WorkPosition;
        }

        return true;
    }

    internal bool TryAdvanceBuildingPacking(
        JobSnapshot job,
        AgentViewModel agent,
        long tick,
        out Result result)
    {
        if (job.Definition is not BuildingBoxPackingJobDefinition packing)
        {
            result = Result.Success();
            return false;
        }

        EnsureBuildingPackingExecutionInitialized();
        BuildingSnapshot? building = _buildingsRepository!.Get().Get(packing.BuildingId);
        Result<BuildingBoxPackingExecutionStepKind> evaluated =
            BuildingBoxPackingExecutionPolicy.Evaluate(
                job,
                building,
                new CellId(agent.CellX, agent.CellY, agent.CellZ));
        if (evaluated.IsFailure)
        {
            result = Result.Failure(evaluated.Error!);
            return true;
        }

        result = ExecuteBuildingPackingStep(
            evaluated.Value,
            building!,
            job.AssignedAgentId!.Value,
            packing.BuildingId,
            job.Id,
            tick);
        if (result.IsSuccess
            && evaluated.Value == BuildingBoxPackingExecutionStepKind.CompletePacking)
        {
            _buildingPackingRoutes.Remove(job.Id);
        }

        return true;
    }

    internal IReadOnlyList<RouteViewModel> LoadBuildingPackingRoutes()
    {
        List<RouteViewModel> routes = new List<RouteViewModel>();
        foreach (KeyValuePair<EntityId, BuildingPackingRoutePlan> pair
            in _buildingPackingRoutes.OrderBy(
                value => value.Key.ToString(),
                StringComparer.Ordinal))
        {
            JobSnapshot? job = _jobRepository.Get().Get(pair.Key);
            if (job == null || !job.AssignedAgentId.HasValue)
            {
                continue;
            }

            PathResult path = pair.Value.Path;
            RouteCellViewModel[] cells = path.Path == null
                ? Array.Empty<RouteCellViewModel>()
                : path.Path.Cells
                    .Select(cell => new RouteCellViewModel(cell.X, cell.Y, cell.Z))
                    .ToArray();
            routes.Add(new RouteViewModel(
                pair.Key.ToString(),
                job.AssignedAgentId.Value.ToString(),
                pair.Value.Target.X,
                pair.Value.Target.Y,
                path.Succeeded,
                "Building packing: " + path.Diagnostics.Detail,
                path.Path?.TotalCost ?? 0,
                path.Diagnostics.SnapshotVersion,
                cells));
        }

        return routes;
    }

    internal IReadOnlyList<WorldItemViewModel> LoadAllWorldItems()
    {
        IReadOnlyList<WorldItemViewModel> terrainItems = _inventoryPresenter.Load();
        if (_buildingInventoryPresenter == null)
        {
            return terrainItems;
        }

        return terrainItems
            .Concat(_buildingInventoryPresenter.Load())
            .OrderBy(item => item.CellY)
            .ThenBy(item => item.CellX)
            .ThenBy(item => item.StackId, StringComparer.Ordinal)
            .ToArray();
    }

    private Result ExecuteBuildingPackingStep(
        BuildingBoxPackingExecutionStepKind step,
        BuildingSnapshot building,
        EntityId workerId,
        EntityId buildingId,
        EntityId jobId,
        long tick)
    {
        if (step == BuildingBoxPackingExecutionStepKind.None)
        {
            return Result.Success();
        }

        Result<PackableBuildingExecutionState> execution =
            GetOrCreatePackableBuildingExecution(
                jobId,
                buildingId,
                building.Definition.Id,
                PackableBuildingOperationKind.Pack,
                building.Definition.BoxPolicy!.PackingWork);
        if (execution.IsFailure)
        {
            return Result.Failure(execution.Error!);
        }

        if (step == BuildingBoxPackingExecutionStepKind.StartJob)
        {
            Result started = _packableBuildingExecutions!.StartOrResume(jobId, workerId);
            return started.IsFailure
                ? started
                : _advanceHandler.Handle(new AdvanceJobCommand(jobId, tick));
        }

        if (step == BuildingBoxPackingExecutionStepKind.AddWork)
        {
            Result added = _buildingPackingWork!.Handle(new AddBuildingBoxPackingWorkCommand(
                buildingId,
                jobId,
                workAmount: 1,
                tick: tick));
            if (added.IsFailure)
            {
                return added;
            }

            return _packableBuildingExecutions!.CompleteIteration(jobId, workerId);
        }

        return step switch
        {
            BuildingBoxPackingExecutionStepKind.AdvanceStage =>
                _advanceHandler.Handle(new AdvanceJobCommand(jobId, tick)),
            BuildingBoxPackingExecutionStepKind.CompletePacking =>
                _buildingPackingCompletion!.Handle(new CompleteBuildingBoxPackingCommand(
                    buildingId,
                    jobId,
                    tick)),
            _ => throw new ArgumentOutOfRangeException(nameof(step)),
        };
    }

    private static IReadOnlyList<JobCandidate> CreateBuildingPackingCandidates(
        IReadOnlyList<AgentViewModel> agents,
        CellId target)
    {
        JobCandidate[] candidates = new JobCandidate[agents.Count];
        for (int index = 0; index < agents.Count; index++)
        {
            AgentViewModel agent = agents[index];
            int distance = Math.Abs(agent.CellX - target.X)
                + Math.Abs(agent.CellY - target.Y);
            candidates[index] = new JobCandidate(
                EntityId.Parse(agent.Id),
                skillLevel: 4_500 - (index * 200),
                distanceCost: distance,
                isAvailable: agent.IsAvailableForAutomaticPlanning);
        }

        return candidates;
    }

    private void EnsureBuildingPackingExecutionInitialized()
    {
        if (_buildingsRepository == null
            || _buildingInventoryRepository == null
            || _buildingPackingCandidates == null
            || _buildingPackingAssignment == null
            || _buildingPackingWork == null
            || _buildingPackingCompletion == null
            || _buildingInventoryPresenter == null
            || _buildingPackingPathfinder == null
            || _packableBuildingExecutions == null)
        {
            throw new InvalidOperationException(
                "Building packing execution is not initialized.");
        }
    }
}
}