using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
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
        private CreateResidentInventoryPlacementHandler? _buildingInventoryPlacementCreate;
        private CreateResidentInventoryPlacementHandler? _terrainInventoryPlacementCreate;
        private CompleteResidentInventoryPlacementHandler? _buildingInventoryPlacementComplete;
        private CompleteResidentInventoryPlacementHandler? _terrainInventoryPlacementComplete;
        private ResidentInventoryPlacementQueue? _buildingInventoryPlacementQueue;
        private ResidentInventoryPlacementQueue? _terrainInventoryPlacementQueue;
        private NavigationPathfinder? _residentInventoryPlacementPathfinder;
        private readonly Dictionary<EntityId, ResidentInventoryPlacementRoutePlan>
            _residentInventoryPlacementRoutes =
                new Dictionary<EntityId, ResidentInventoryPlacementRoutePlan>();
        private long _nextResidentInventoryPlacementSequence;

        internal Result CreateResidentInventoryPlacement(
            string residentId,
            string stackId,
            CellId destination,
            long tick)
        {
            EnsureResidentInventoryPlacementInitialized();
            EntityId resident = EntityId.Parse(residentId);
            EntityId stack = EntityId.Parse(stackId);
            InMemoryInventoryRepository? repository =
                ResolveResidentInventoryPlacementRepository(stack);
            ItemStackSnapshot? snapshot = repository?.Get().GetStack(stack);
            if (repository == null || snapshot == null)
            {
                return Result.Failure(InventoryErrors.StackNotFound);
            }

            bool alreadyQueued = _jobRepository.Get().GetAll().Any(value =>
                !value.IsTerminal
                && value.Definition is ResidentInventoryPlacementJobDefinition placement
                && placement.ResidentId == resident);
            if (!alreadyQueued)
            {
                Result prepared = PrepareResidentsForDirectCommand(
                    new[] { residentId },
                    tick);
                if (prepared.IsFailure)
                {
                    return prepared;
                }
            }

            long sequence = checked(_nextResidentInventoryPlacementSequence + 1);
            _nextResidentInventoryPlacementSequence = sequence;
            CreateResidentInventoryPlacementHandler handler = ReferenceEquals(
                repository,
                _buildingInventoryRepository)
                    ? _buildingInventoryPlacementCreate!
                    : _terrainInventoryPlacementCreate!;
            return handler.Handle(new CreateResidentInventoryPlacementCommand(
                DemoId('d', sequence),
                resident,
                stack,
                snapshot.Quantity,
                destination,
                GetBuildingPlacementReachableCells(),
                priority: 700,
                tick));
        }

        internal Result SynchronizeResidentInventoryPlacement(long tick)
        {
            EnsureResidentInventoryPlacementInitialized();
            Result building = _buildingInventoryPlacementQueue!.Synchronize(tick);
            return building.IsFailure
                ? building
                : _terrainInventoryPlacementQueue!.Synchronize(tick);
        }

        internal bool TryPlanResidentInventoryPlacementMovement(
            JobSnapshot job,
            AgentViewModel agent,
            NavigationSnapshot navigation,
            IDictionary<string, CellId> movement)
        {
            if (job.Definition is not ResidentInventoryPlacementJobDefinition placement)
            {
                return false;
            }

            EnsureResidentInventoryPlacementInitialized();
            CellId start = new CellId(agent.CellX, agent.CellY, agent.CellZ);
            PathResult path = _residentInventoryPlacementPathfinder!.FindPath(
                navigation,
                new PathRequest(
                    start,
                    placement.DestinationCell,
                    navigation.NavigationVersion));
            _residentInventoryPlacementRoutes[job.Id] =
                new ResidentInventoryPlacementRoutePlan(placement.DestinationCell, path);
            if (path.Succeeded)
            {
                movement[agent.Id] = path.Path!.Cells.Count > 1
                    ? path.Path.Cells[1]
                    : placement.DestinationCell;
            }

            return true;
        }

        internal Result AdvanceResidentInventoryPlacement(
            long tick,
            IReadOnlyList<AgentViewModel> agents)
        {
            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
            }

            EnsureResidentInventoryPlacementInitialized();
            Dictionary<string, AgentViewModel> agentsById = agents.ToDictionary(
                value => value.Id,
                StringComparer.Ordinal);
            foreach (JobSnapshot job in _jobRepository.Get().GetAll())
            {
                if (!IsActive(job)
                    || job.Definition is not ResidentInventoryPlacementJobDefinition placement
                    || !job.AssignedAgentId.HasValue
                    || !agentsById.TryGetValue(
                        job.AssignedAgentId.Value.ToString(),
                        out AgentViewModel? agent))
                {
                    continue;
                }

                if (_residentInventoryPlacementRoutes.TryGetValue(
                        job.Id,
                        out ResidentInventoryPlacementRoutePlan? route)
                    && !route.Path.Succeeded)
                {
                    Result blocked = BlockResidentInventoryPlacement(job.Id, tick);
                    if (blocked.IsFailure)
                    {
                        return blocked;
                    }

                    continue;
                }

                CellId workerCell = new CellId(agent.CellX, agent.CellY, agent.CellZ);
                if (workerCell != placement.DestinationCell)
                {
                    continue;
                }

                Result advanced = AdvanceResidentInventoryPlacementAtTarget(
                    job.Id,
                    workerCell,
                    tick);
                if (advanced.IsFailure)
                {
                    return advanced;
                }
            }

            return Result.Success();
        }

        private Result AdvanceResidentInventoryPlacementAtTarget(
            EntityId jobId,
            CellId workerCell,
            long tick)
        {
            for (int index = 0; index < 3; index++)
            {
                JobSnapshot? current = _jobRepository.Get().Get(jobId);
                if (current == null || current.IsTerminal)
                {
                    _residentInventoryPlacementRoutes.Remove(jobId);
                    return Result.Success();
                }

                if (current.Status == JobStatus.Claimed
                    || current.Stage == JobStageKind.TravelToDestination)
                {
                    Result stage = _advanceHandler.Handle(
                        new AdvanceJobCommand(current.Id, tick));
                    if (stage.IsFailure)
                    {
                        return stage;
                    }

                    continue;
                }

                if (current.Stage != JobStageKind.DepositItem
                    || current.Definition is not ResidentInventoryPlacementJobDefinition placement)
                {
                    return Result.Failure(JobErrors.InvalidStatus);
                }

                InMemoryInventoryRepository? repository =
                    ResolveResidentInventoryPlacementRepository(placement.StackId);
                CompleteResidentInventoryPlacementHandler? handler = ReferenceEquals(
                    repository,
                    _buildingInventoryRepository)
                        ? _buildingInventoryPlacementComplete
                        : _terrainInventoryPlacementComplete;
                if (repository == null || handler == null)
                {
                    return Result.Failure(InventoryErrors.StackNotFound);
                }

                Result completed = handler.Handle(
                    new CompleteResidentInventoryPlacementCommand(
                        current.Id,
                        workerCell,
                        tick));
                if (completed.IsSuccess)
                {
                    _residentInventoryPlacementRoutes.Remove(current.Id);
                }

                return completed;
            }

            return Result.Failure(JobErrors.InvalidStatus);
        }

        private Result BlockResidentInventoryPlacement(EntityId jobId, long tick)
        {
            JobSystem jobs = _jobRepository.Get();
            Result blocked = jobs.Block(
                jobId,
                new JobBlockReason(
                    "inventory.placement.route_unavailable",
                    "The selected resident cannot reach the item placement target."),
                tick);
            if (blocked.IsFailure)
            {
                return blocked;
            }

            _jobRepository.Save(jobs);
            _worldSession.Journal.Append(jobs.DequeueUncommittedEvents());
            _residentInventoryPlacementRoutes.Remove(jobId);
            return Result.Success();
        }

        private InMemoryInventoryRepository? ResolveResidentInventoryPlacementRepository(
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

        private void EnsureResidentInventoryPlacementInitialized()
        {
            if (_buildingInventoryPlacementCreate != null
                && _terrainInventoryPlacementCreate != null
                && _buildingInventoryPlacementComplete != null
                && _terrainInventoryPlacementComplete != null
                && _buildingInventoryPlacementQueue != null
                && _terrainInventoryPlacementQueue != null
                && _residentInventoryPlacementPathfinder != null)
            {
                return;
            }

            if (_buildingInventoryRepository == null)
            {
                throw new InvalidOperationException(
                    "Resident inventory placement requires building inventory state.");
            }

            _buildingInventoryPlacementCreate = new CreateResidentInventoryPlacementHandler(
                _worldSession.Repository,
                _buildingInventoryRepository,
                _jobRepository,
                _worldSession.Journal);
            _terrainInventoryPlacementCreate = new CreateResidentInventoryPlacementHandler(
                _worldSession.Repository,
                _inventoryRepository,
                _jobRepository,
                _worldSession.Journal);
            _buildingInventoryPlacementComplete =
                new CompleteResidentInventoryPlacementHandler(
                    _worldSession.Repository,
                    _buildingInventoryRepository,
                    _jobRepository,
                    _worldSession.Journal);
            _terrainInventoryPlacementComplete =
                new CompleteResidentInventoryPlacementHandler(
                    _worldSession.Repository,
                    _inventoryRepository,
                    _jobRepository,
                    _worldSession.Journal);
            _buildingInventoryPlacementQueue = new ResidentInventoryPlacementQueue(
                _buildingInventoryRepository,
                _jobRepository,
                _worldSession.Journal);
            _terrainInventoryPlacementQueue = new ResidentInventoryPlacementQueue(
                _inventoryRepository,
                _jobRepository,
                _worldSession.Journal);
            _residentInventoryPlacementPathfinder = new NavigationPathfinder();
        }

        private sealed class ResidentInventoryPlacementRoutePlan
        {
            internal ResidentInventoryPlacementRoutePlan(CellId target, PathResult path)
            {
                Target = target;
                Path = path;
            }

            internal CellId Target { get; }
            internal PathResult Path { get; }
        }
    }
}
