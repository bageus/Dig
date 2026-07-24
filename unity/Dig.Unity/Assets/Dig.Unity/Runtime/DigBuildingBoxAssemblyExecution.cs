using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Buildings;
using Dig.Application.Jobs;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.Navigation;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private readonly Dictionary<EntityId, BuildingBoxAssemblyRoutePlan>
            _buildingBoxAssemblyRoutes = new Dictionary<EntityId, BuildingBoxAssemblyRoutePlan>();
        private InMemoryJobCandidateProvider? _buildingBoxAssemblyCandidates;
        private AssignAvailableJobsHandler? _buildingBoxAssemblyAssignment;
        private AcquireBuildingBoxForAssemblyHandler? _buildingBoxAssemblyAcquire;
        private CommitBuildingBoxToSiteHandler? _buildingBoxAssemblyCommit;
        private AddBuildingBoxAssemblyWorkHandler? _buildingBoxAssemblyWork;
        private CompleteBuildingBoxAssemblyHandler? _buildingBoxAssemblyComplete;
        private NavigationPathfinder? _buildingBoxAssemblyPathfinder;

        private void InitializeBuildingBoxAssemblyExecution(InMemoryExecutionJournal journal)
        {
            if (_buildingsRepository == null || _buildingInventoryRepository == null)
            {
                throw new InvalidOperationException("BuildingBox state must be initialized first.");
            }

            _packableBuildingExecutions ??= new PackableBuildingExecutionRegistry();
            _buildingBoxAssemblyCandidates = new InMemoryJobCandidateProvider();
            _buildingBoxAssemblyAssignment = new AssignAvailableJobsHandler(
                _jobRepository,
                _buildingBoxAssemblyCandidates,
                journal);
            _buildingBoxAssemblyAcquire = new AcquireBuildingBoxForAssemblyHandler(
                _buildingsRepository,
                _buildingInventoryRepository,
                _jobRepository,
                journal);
            _buildingBoxAssemblyCommit = new CommitBuildingBoxToSiteHandler(
                _buildingsRepository,
                _buildingInventoryRepository,
                _jobRepository,
                journal);
            _buildingBoxAssemblyWork = new AddBuildingBoxAssemblyWorkHandler(
                _buildingsRepository,
                _jobRepository,
                journal);
            _buildingBoxAssemblyComplete = new CompleteBuildingBoxAssemblyHandler(
                _buildingsRepository,
                _buildingInventoryRepository,
                _jobRepository,
                journal,
                _skillGrants);
            _buildingBoxAssemblyPathfinder = new NavigationPathfinder();
        }

        internal void SynchronizeBuildingBoxAssembly(
            long tick,
            IReadOnlyList<AgentViewModel> agents)
        {
            EnsureBuildingBoxAssemblyInitialized();
            InventoryState inventory = _buildingInventoryRepository!.Get();
            bool hasAvailable = false;
            foreach (JobSnapshot job in _jobRepository.Get().GetAll())
            {
                if (job.Status != JobStatus.Available
                    || job.Definition is not BuildingBoxAssemblyJobDefinition assembly)
                {
                    continue;
                }

                BuildingSnapshot? building = _buildingsRepository!.Get().Get(assembly.BuildingId);
                ItemStackSnapshot? box = inventory.GetStack(assembly.SourceStackId);
                if (building?.BoxPlan?.CommitState != BuildingBoxCommitState.Reserved
                    || box is null)
                {
                    continue;
                }

                _buildingBoxAssemblyCandidates!.SetCandidates(
                    job.Id,
                    CreateBuildingBoxAssemblyCandidates(agents, box));
                hasAvailable = true;
            }

            if (hasAvailable)
            {
                _buildingBoxAssemblyAssignment!.Handle(new AssignAvailableJobsCommand(tick));
            }
        }

        internal bool TryPlanBuildingBoxAssemblyMovement(
            JobSnapshot job,
            AgentViewModel agent,
            NavigationSnapshot navigation,
            IDictionary<string, CellId> movement)
        {
            if (job.Definition is not BuildingBoxAssemblyJobDefinition assembly)
            {
                return false;
            }

            EnsureBuildingBoxAssemblyInitialized();
            ItemStackSnapshot? box = _buildingInventoryRepository!.Get().GetStack(
                assembly.SourceStackId);
            CellId target = ResolveBuildingBoxAssemblyTarget(job, assembly, box);
            CellId start = new CellId(agent.CellX, agent.CellY, agent.CellZ);
            PathResult path = _buildingBoxAssemblyPathfinder!.FindPath(
                navigation,
                new PathRequest(start, target, navigation.NavigationVersion));
            _buildingBoxAssemblyRoutes[job.Id] = new BuildingBoxAssemblyRoutePlan(target, path);
            if (path.Succeeded)
            {
                movement[agent.Id] = path.Path!.Cells.Count > 1
                    ? path.Path.Cells[1]
                    : target;
            }

            return true;
        }

        internal Result AdvanceBuildingBoxAssembly(
            long tick,
            IReadOnlyList<AgentViewModel> agents)
        {
            EnsureBuildingBoxAssemblyInitialized();
            Dictionary<string, AgentViewModel> agentsById = agents.ToDictionary(
                agent => agent.Id,
                StringComparer.Ordinal);
            foreach (JobSnapshot job in _jobRepository.Get().GetAll())
            {
                if (!IsActive(job)
                    || job.Definition is not BuildingBoxAssemblyJobDefinition assembly
                    || !job.AssignedAgentId.HasValue
                    || !agentsById.TryGetValue(
                        job.AssignedAgentId.Value.ToString(),
                        out AgentViewModel? agent))
                {
                    continue;
                }

                BuildingSnapshot? building = _buildingsRepository!.Get().Get(assembly.BuildingId);
                ItemStackSnapshot? box = _buildingInventoryRepository!.Get().GetStack(
                    assembly.SourceStackId);
                Result<BuildingBoxAssemblyExecutionStepKind> evaluated =
                    BuildingBoxAssemblyExecutionPolicy.Evaluate(
                        job,
                        building,
                        box,
                        new CellId(agent.CellX, agent.CellY, agent.CellZ));
                if (evaluated.IsFailure)
                {
                    return Result.Failure(evaluated.Error!);
                }

                Result executed = ExecuteBuildingBoxAssemblyStep(
                    evaluated.Value,
                    assembly,
                    building!,
                    job.AssignedAgentId.Value,
                    new CellId(agent.CellX, agent.CellY, agent.CellZ),
                    tick);
                if (executed.IsFailure)
                {
                    return executed;
                }

                if (evaluated.Value == BuildingBoxAssemblyExecutionStepKind.CompleteAssembly)
                {
                    _buildingBoxAssemblyRoutes.Remove(job.Id);
                }
            }

            return Result.Success();
        }

        internal IReadOnlyList<RouteViewModel> LoadBuildingBoxAssemblyRoutes()
        {
            List<RouteViewModel> routes = new List<RouteViewModel>();
            foreach (KeyValuePair<EntityId, BuildingBoxAssemblyRoutePlan> pair
                in _buildingBoxAssemblyRoutes.OrderBy(
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
                    pair.Value.Target.Z,
                    path.Succeeded,
                    "BuildingBox assembly: " + path.Diagnostics.Detail,
                    path.Path?.TotalCost ?? 0,
                    path.Diagnostics.SnapshotVersion,
                    cells));
            }

            return routes;
        }

        private Result ExecuteBuildingBoxAssemblyStep(
            BuildingBoxAssemblyExecutionStepKind step,
            BuildingBoxAssemblyJobDefinition assembly,
            BuildingSnapshot building,
            EntityId workerId,
            CellId workerCell,
            long tick)
        {
            if (step == BuildingBoxAssemblyExecutionStepKind.None)
            {
                return Result.Success();
            }

            Result<PackableBuildingExecutionState> execution =
                GetOrCreatePackableBuildingExecution(
                    assembly.Id,
                    assembly.BuildingId,
                    building.Definition.Id,
                    PackableBuildingOperationKind.Unpack,
                    building.Definition.RequiredWork);
            if (execution.IsFailure)
            {
                return Result.Failure(execution.Error!);
            }

            if (step == BuildingBoxAssemblyExecutionStepKind.StartJob)
            {
                Result started = StartOrResumePackableBuildingExecution(assembly.Id, workerId);
                return started.IsFailure
                    ? started
                    : _advanceHandler.Handle(new AdvanceJobCommand(assembly.Id, tick));
            }

            if (step == BuildingBoxAssemblyExecutionStepKind.AddWork)
            {
                Result added = _buildingBoxAssemblyWork!.Handle(
                    new AddBuildingBoxAssemblyWorkCommand(
                        assembly.BuildingId,
                        assembly.Id,
                        workAmount: 1,
                        tick: tick));
                if (added.IsFailure)
                {
                    return added;
                }

                return CompletePackableBuildingIteration(assembly.Id, workerId);
            }

            return ExecuteBuildingBoxAssemblyTransition(step, assembly, workerCell, tick);
        }

        private Result ExecuteBuildingBoxAssemblyTransition(
            BuildingBoxAssemblyExecutionStepKind step,
            BuildingBoxAssemblyJobDefinition assembly,
            CellId workerCell,
            long tick)
        {
            return step switch
            {
                BuildingBoxAssemblyExecutionStepKind.AcquireBox =>
                    _buildingBoxAssemblyAcquire!.Handle(
                        new AcquireBuildingBoxForAssemblyCommand(
                            assembly.BuildingId,
                            assembly.Id,
                            workerCell,
                            tick)),
                BuildingBoxAssemblyExecutionStepKind.AdvanceStage =>
                    _advanceHandler.Handle(new AdvanceJobCommand(assembly.Id, tick)),
                BuildingBoxAssemblyExecutionStepKind.CommitBoxToSite =>
                    _buildingBoxAssemblyCommit!.Handle(new CommitBuildingBoxToSiteCommand(
                        assembly.BuildingId,
                        assembly.Id,
                        tick)),
                BuildingBoxAssemblyExecutionStepKind.CompleteAssembly =>
                    _buildingBoxAssemblyComplete!.Handle(
                        new CompleteBuildingBoxAssemblyCommand(
                            assembly.BuildingId,
                            assembly.Id,
                            tick)),
                _ => throw new ArgumentOutOfRangeException(nameof(step)),
            };
        }

        private static CellId ResolveBuildingBoxAssemblyTarget(
            JobSnapshot job,
            BuildingBoxAssemblyJobDefinition assembly,
            ItemStackSnapshot? box)
        {
            bool acquiring = job.Status == JobStatus.Claimed
                || job.Stage == JobStageKind.AcquireItem;
            if (acquiring
                && box?.Location.Kind == ItemLocationKind.World
                && box.Location.HasCell)
            {
                return box.Location.CellId;
            }

            return assembly.WorkPosition;
        }

        private static IReadOnlyList<JobCandidate> CreateBuildingBoxAssemblyCandidates(
            IReadOnlyList<AgentViewModel> agents,
            ItemStackSnapshot box)
        {
            if (box.Location.Kind == ItemLocationKind.AgentInventory && box.Location.HasOwner)
            {
                return agents
                    .Where(agent => string.Equals(
                        agent.Id,
                        box.Location.OwnerId.ToString(),
                        StringComparison.Ordinal))
                    .Select(agent => new JobCandidate(
                        EntityId.Parse(agent.Id),
                        skillLevel: 5_000,
                        distanceCost: 0,
                        isAvailable: agent.IsAvailableForAutomaticPlanning))
                    .ToArray();
            }

            if (box.Location.Kind != ItemLocationKind.World || !box.Location.HasCell)
            {
                return Array.Empty<JobCandidate>();
            }

            CellId source = box.Location.CellId;
            return agents.Select((agent, index) => new JobCandidate(
                EntityId.Parse(agent.Id),
                skillLevel: 4_800 - (index * 150),
                distanceCost: Math.Abs(agent.CellX - source.X) + Math.Abs(agent.CellY - source.Y),
                isAvailable: agent.IsAvailableForAutomaticPlanning)).ToArray();
        }

        private void EnsureBuildingBoxAssemblyInitialized()
        {
            if (_buildingsRepository == null
                || _buildingInventoryRepository == null
                || _buildingBoxAssemblyCandidates == null
                || _buildingBoxAssemblyAssignment == null
                || _buildingBoxAssemblyAcquire == null
                || _buildingBoxAssemblyCommit == null
                || _buildingBoxAssemblyWork == null
                || _buildingBoxAssemblyComplete == null
                || _buildingBoxAssemblyPathfinder == null
                || _packableBuildingExecutions == null)
            {
                throw new InvalidOperationException(
                    "BuildingBox assembly execution is not initialized.");
            }
        }
    }
}
