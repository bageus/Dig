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
using Dig.Presentation.Navigation;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private readonly Dictionary<EntityId, WorldItemPickupRoutePlan>
            _worldItemPickupRoutes = new Dictionary<EntityId, WorldItemPickupRoutePlan>();
        private CreateWorldItemPickupHandler? _terrainItemPickupCreate;
        private CreateWorldItemPickupHandler? _buildingItemPickupCreate;
        private CompleteWorldItemPickupHandler? _terrainItemPickupComplete;
        private CompleteWorldItemPickupHandler? _buildingItemPickupComplete;
        private NavigationPathfinder? _worldItemPickupPathfinder;
        private long _nextWorldItemPickupSequence;

        private void InitializeWorldItemPickupExecution(InMemoryExecutionJournal journal)
        {
            if (_buildingInventoryRepository == null)
            {
                throw new InvalidOperationException("Building inventory must be initialized first.");
            }

            _terrainItemPickupCreate = new CreateWorldItemPickupHandler(
                _inventoryRepository,
                _jobRepository,
                journal);
            _buildingItemPickupCreate = new CreateWorldItemPickupHandler(
                _buildingInventoryRepository,
                _jobRepository,
                journal);
            _terrainItemPickupComplete = new CompleteWorldItemPickupHandler(
                _inventoryRepository,
                _jobRepository,
                journal);
            _buildingItemPickupComplete = new CompleteWorldItemPickupHandler(
                _buildingInventoryRepository,
                _jobRepository,
                journal);
            _worldItemPickupPathfinder = new NavigationPathfinder();
        }

        internal Result CreateWorldItemPickup(
            string stackId,
            string residentId,
            CellId sourceCell,
            long tick)
        {
            EnsureWorldItemPickupInitialized();
            if (string.IsNullOrWhiteSpace(stackId)
                || string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Stack and resident ids are required.");
            }

            EntityId stack = EntityId.Parse(stackId);
            InMemoryInventoryRepository? repository = ResolveWorldItemRepository(stack);
            if (repository == null)
            {
                return Result.Failure(WorldItemPickupErrors.StackMissing);
            }

            long sequence = checked(_nextWorldItemPickupSequence + 1);
            _nextWorldItemPickupSequence = sequence;
            CreateWorldItemPickupHandler handler = ReferenceEquals(
                repository,
                _buildingInventoryRepository)
                    ? _buildingItemPickupCreate!
                    : _terrainItemPickupCreate!;
            return handler.Handle(new CreateWorldItemPickupCommand(
                DemoId('9', sequence),
                stack,
                EntityId.Parse(residentId),
                sourceCell,
                priority: 675,
                tick));
        }

        internal bool TryPlanWorldItemPickupMovement(
            JobSnapshot job,
            AgentViewModel agent,
            NavigationSnapshot navigation,
            IDictionary<string, CellId> movement)
        {
            if (job.Definition is not WorldItemPickupJobDefinition pickup)
            {
                return false;
            }

            EnsureWorldItemPickupInitialized();
            CellId start = new CellId(agent.CellX, agent.CellY);
            PathResult path = _worldItemPickupPathfinder!.FindPath(
                navigation,
                new PathRequest(start, pickup.SourceCell, navigation.NavigationVersion));
            _worldItemPickupRoutes[job.Id] = new WorldItemPickupRoutePlan(
                pickup.SourceCell,
                path);
            if (path.Succeeded)
            {
                movement[agent.Id] = path.Path!.Cells.Count > 1
                    ? path.Path.Cells[1]
                    : pickup.SourceCell;
            }

            return true;
        }

        internal Result AdvanceWorldItemPickup(
            long tick,
            IReadOnlyList<AgentViewModel> agents)
        {
            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
            }

            EnsureWorldItemPickupInitialized();
            Dictionary<string, AgentViewModel> agentsById = agents.ToDictionary(
                agent => agent.Id,
                StringComparer.Ordinal);
            foreach (JobSnapshot job in _jobRepository.Get().GetAll())
            {
                if (!IsActive(job)
                    || job.Definition is not WorldItemPickupJobDefinition pickup
                    || !job.AssignedAgentId.HasValue
                    || !agentsById.TryGetValue(
                        job.AssignedAgentId.Value.ToString(),
                        out AgentViewModel? agent))
                {
                    continue;
                }

                if (agent.CellX != pickup.SourceCell.X || agent.CellY != pickup.SourceCell.Y)
                {
                    continue;
                }

                Result result = AdvanceWorldItemPickupAtSource(job, pickup, tick);
                if (result.IsFailure)
                {
                    return result;
                }
            }

            return Result.Success();
        }

        internal IReadOnlyList<RouteViewModel> LoadWorldItemPickupRoutes()
        {
            List<RouteViewModel> routes = new List<RouteViewModel>();
            foreach (KeyValuePair<EntityId, WorldItemPickupRoutePlan> pair
                in _worldItemPickupRoutes.OrderBy(
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
                        .Select(cell => new RouteCellViewModel(cell.X, cell.Y))
                        .ToArray();
                routes.Add(new RouteViewModel(
                    pair.Key.ToString(),
                    job.AssignedAgentId.Value.ToString(),
                    pair.Value.Target.X,
                    pair.Value.Target.Y,
                    path.Succeeded,
                    "World item pickup: " + path.Diagnostics.Detail,
                    path.Path?.TotalCost ?? 0,
                    path.Diagnostics.SnapshotVersion,
                    cells));
            }

            return routes;
        }

        private Result AdvanceWorldItemPickupAtSource(
            JobSnapshot job,
            WorldItemPickupJobDefinition pickup,
            long tick)
        {
            if (job.Status == JobStatus.Claimed
                || job.Stage == JobStageKind.TravelToTarget)
            {
                return _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
            }

            if (job.Stage != JobStageKind.AcquireItem)
            {
                return Result.Success();
            }

            InMemoryInventoryRepository? repository = ResolveWorldItemRepository(pickup.StackId);
            if (repository == null)
            {
                return Result.Failure(WorldItemPickupErrors.StackMissing);
            }

            CompleteWorldItemPickupHandler handler = ReferenceEquals(
                repository,
                _buildingInventoryRepository)
                    ? _buildingItemPickupComplete!
                    : _terrainItemPickupComplete!;
            Result completed = handler.Handle(
                new CompleteWorldItemPickupCommand(job.Id, tick));
            if (completed.IsSuccess)
            {
                _worldItemPickupRoutes.Remove(job.Id);
            }

            return completed;
        }

        private InMemoryInventoryRepository? ResolveWorldItemRepository(EntityId stackId)
        {
            if (_buildingInventoryRepository?.Get().GetStack(stackId) != null)
            {
                return _buildingInventoryRepository;
            }

            return _inventoryRepository.Get().GetStack(stackId) != null
                ? _inventoryRepository
                : null;
        }

        private void EnsureWorldItemPickupInitialized()
        {
            if (_buildingInventoryRepository == null
                || _terrainItemPickupCreate == null
                || _buildingItemPickupCreate == null
                || _terrainItemPickupComplete == null
                || _buildingItemPickupComplete == null
                || _worldItemPickupPathfinder == null)
            {
                throw new InvalidOperationException(
                    "World item pickup execution is not initialized.");
            }
        }

        private sealed class WorldItemPickupRoutePlan
        {
            public WorldItemPickupRoutePlan(CellId target, PathResult path)
            {
                Target = target;
                Path = path;
            }

            public CellId Target { get; }
            public PathResult Path { get; }
        }
    }
}
