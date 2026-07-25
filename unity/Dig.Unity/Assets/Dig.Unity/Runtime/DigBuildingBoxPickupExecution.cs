using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Inventory;
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
        private readonly Dictionary<EntityId, BuildingBoxPickupRoutePlan>
            _buildingBoxPickupRoutes = new Dictionary<EntityId, BuildingBoxPickupRoutePlan>();
        private CreateBuildingBoxPickupHandler? _buildingBoxPickupCreate;
        private CompleteBuildingBoxPickupHandler? _buildingBoxPickupComplete;
        private NavigationPathfinder? _buildingBoxPickupPathfinder;
        private long _nextPickupSequence;

        private void InitializeBuildingBoxPickupExecution(InMemoryExecutionJournal journal)
        {
            if (_buildingInventoryRepository == null)
            {
                throw new InvalidOperationException("Building inventory must be initialized first.");
            }

            _buildingBoxPickupCreate = new CreateBuildingBoxPickupHandler(
                _buildingInventoryRepository,
                _jobRepository,
                journal);
            _buildingBoxPickupComplete = new CompleteBuildingBoxPickupHandler(
                _buildingInventoryRepository,
                _jobRepository,
                journal);
            _buildingBoxPickupPathfinder = new NavigationPathfinder();
        }

        internal Result CreateBuildingBoxPickup(
            string stackId,
            string residentId,
            CellId sourceCell,
            long tick)
        {
            EnsureBuildingBoxPickupInitialized();
            if (string.IsNullOrWhiteSpace(stackId)
                || string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Stack and resident ids are required.");
            }

            EntityId parsedStackId = EntityId.Parse(stackId);
            ItemStackSnapshot? stack = _buildingInventoryRepository!.Get().GetStack(
                parsedStackId);
            BuildingDefinition? building = stack == null
                ? null
                : ResolveBuildingBoxDefinition(stack.ItemId);
            BuildingBoxPolicy? policy = building?.BoxPolicy;
            if (stack == null
                || policy == null
                || stack.ItemId != policy.BoxItemId
                || !stack.Location.HasCell
                || stack.Location.CellId != sourceCell
                || stack.Quantity != 1
                || stack.AvailableQuantity != 1)
            {
                return Result.Failure(PlacementSourceUnavailable);
            }

            long sequence = checked(_nextPickupSequence + 1);
            _nextPickupSequence = sequence;
            return _buildingBoxPickupCreate!.Handle(new CreateBuildingBoxPickupCommand(
                DemoId('2', sequence),
                parsedStackId,
                EntityId.Parse(residentId),
                policy.BoxItemId,
                sourceCell,
                priority: 700,
                tick: tick));
        }

        internal bool TryPlanBuildingBoxPickupMovement(
            JobSnapshot job,
            AgentViewModel agent,
            NavigationSnapshot navigation,
            IDictionary<string, CellId> movement)
        {
            if (job.Definition is not BuildingBoxPickupJobDefinition pickup)
            {
                return false;
            }

            EnsureBuildingBoxPickupInitialized();
            CellId start = new CellId(agent.CellX, agent.CellY, agent.CellZ);
            PathResult path = _buildingBoxPickupPathfinder!.FindPath(
                navigation,
                new PathRequest(start, pickup.SourceCell, navigation.NavigationVersion));
            _buildingBoxPickupRoutes[job.Id] = new BuildingBoxPickupRoutePlan(
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

        internal Result AdvanceBuildingBoxPickup(
            long tick,
            IReadOnlyList<AgentViewModel> agents)
        {
            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
            }

            EnsureBuildingBoxPickupInitialized();
            Dictionary<string, AgentViewModel> agentsById = agents.ToDictionary(
                agent => agent.Id,
                StringComparer.Ordinal);
            foreach (JobSnapshot job in _jobRepository.Get().GetAll())
            {
                if (!IsActive(job)
                    || job.Definition is not BuildingBoxPickupJobDefinition pickup
                    || !job.AssignedAgentId.HasValue
                    || !agentsById.TryGetValue(
                        job.AssignedAgentId.Value.ToString(),
                        out AgentViewModel? agent))
                {
                    continue;
                }

                if (agent.CellX != pickup.SourceCell.X
                    || agent.CellY != pickup.SourceCell.Y
                    || agent.CellZ != pickup.SourceCell.Z)
                {
                    continue;
                }

                Result result = AdvanceBuildingBoxPickupAtSource(job, tick);
                if (result.IsFailure)
                {
                    return result;
                }
            }

            return Result.Success();
        }

        internal IReadOnlyList<RouteViewModel> LoadBuildingBoxPickupRoutes()
        {
            List<RouteViewModel> routes = new List<RouteViewModel>();
            foreach (KeyValuePair<EntityId, BuildingBoxPickupRoutePlan> pair
                in _buildingBoxPickupRoutes.OrderBy(
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
                    "BuildingBox pickup: " + path.Diagnostics.Detail,
                    path.Path?.TotalCost ?? 0,
                    path.Diagnostics.SnapshotVersion,
                    cells));
            }

            return routes;
        }

        private Result AdvanceBuildingBoxPickupAtSource(JobSnapshot job, long tick)
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

            Result completed = _buildingBoxPickupComplete!.Handle(
                new CompleteBuildingBoxPickupCommand(job.Id, tick));
            if (completed.IsSuccess)
            {
                _buildingBoxPickupRoutes.Remove(job.Id);
            }

            return completed;
        }

        private void EnsureBuildingBoxPickupInitialized()
        {
            if (_buildingBoxDefinition == null
                || _buildingInventoryRepository == null
                || _buildingBoxPickupCreate == null
                || _buildingBoxPickupComplete == null
                || _buildingBoxPickupPathfinder == null)
            {
                throw new InvalidOperationException(
                    "BuildingBox pickup execution is not initialized.");
            }
        }

        private sealed class BuildingBoxPickupRoutePlan
        {
            public BuildingBoxPickupRoutePlan(CellId target, PathResult path)
            {
                Target = target;
                Path = path;
            }

            public CellId Target { get; }
            public PathResult Path { get; }
        }
    }
}
