using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Core;
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
