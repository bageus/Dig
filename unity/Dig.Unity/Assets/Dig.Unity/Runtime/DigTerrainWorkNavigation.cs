using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Application.Navigation;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Navigation;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        public IReadOnlyDictionary<string, CellId> PlanMovement(
            IReadOnlyList<AgentViewModel> agents,
            long tick)
        {
            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
            }

            if (tick < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tick));
            }

            Result refresh = RefreshNavigation();
            if (refresh.IsFailure)
            {
                throw new InvalidOperationException(refresh.Error!.ToString());
            }

            NavigationMap map = _navigationRepository.Get(_profile.Id)
                ?? throw new InvalidOperationException("Navigation map not available.");
            Result<NavigationSnapshot> snapshotResult = map.GetSnapshot();
            if (snapshotResult.IsFailure)
            {
                throw new InvalidOperationException(snapshotResult.Error!.ToString());
            }

            NavigationSnapshot navigation = snapshotResult.Value;
            WorldSnapshot world = _worldSession.LoadSnapshot();
            Dictionary<string, AgentViewModel> agentsById = agents.ToDictionary(
                agent => agent.Id,
                StringComparer.Ordinal);
            Dictionary<string, CellId> movement =
                new Dictionary<string, CellId>(StringComparer.Ordinal);
            _routePlans.Clear();
            _haulingRoutes.Clear();
            _buildingPackingRoutes.Clear();
            _buildingBoxPickupRoutes.Clear();
            _worldItemPickupRoutes.Clear();
            _residentInventoryPlacementRoutes.Clear();
            _buildingBoxAssemblyRoutes.Clear();
            _buildingProductionRoutes.Clear();
            _buildingSupplyRoutes.Clear();
            JobSnapshot[] activeJobs = _jobRepository.Get().GetAll()
                .Where(job => IsActive(job) && job.AssignedAgentId.HasValue)
                .ToArray();
            HashSet<string> assignedAgentIds = new HashSet<string>(
                activeJobs.Select(job => job.AssignedAgentId!.Value.ToString()),
                StringComparer.Ordinal);
            foreach (JobSnapshot job in activeJobs)
            {
                if (!IsActive(job) || !job.AssignedAgentId.HasValue)
                {
                    continue;
                }

                if (job.Definition is SpatialDigJobDefinition)
                {
                    continue;
                }

                string agentId = job.AssignedAgentId.Value.ToString();
                if (!agentsById.TryGetValue(agentId, out AgentViewModel? agent))
                {
                    continue;
                }

                if (TryPlanResidentInventoryPlacementMovement(
                    job,
                    agent,
                    navigation,
                    movement))
                {
                    continue;
                }

                if (TryPlanBarrelMovement(job, agent, navigation, movement))
                {
                    continue;
                }

                if (TryPlanProductionPackageMovement(
                    job,
                    agent,
                    navigation,
                    movement))
                {
                    continue;
                }

                if (TryPlanMushroomMovement(job, agent, navigation, movement))
                {
                    continue;
                }

                if (TryPlanBuildingBoxPickupMovement(job, agent, navigation, movement))
                {
                    continue;
                }

                if (TryPlanWorldItemPickupMovement(job, agent, navigation, movement))
                {
                    continue;
                }

                if (TryPlanBuildingBoxAssemblyMovement(job, agent, navigation, movement))
                {
                    continue;
                }

                if (TryPlanBuildingSupplyMovement(
                    job,
                    agent,
                    navigation,
                    movement,
                    tick))
                {
                    continue;
                }

                if (TryPlanBuildingProductionMovement(job, agent, navigation, movement))
                {
                    continue;
                }

                if (TryPlanHaulingMovement(job, agent, navigation, movement))
                {
                    continue;
                }

                if (TryPlanBuildingPackingMovement(job, agent, navigation, movement))
                {
                    continue;
                }

                Result<TerrainWorkRoutePlan> planned = _routePlanner.Plan(
                    job,
                    new CellId(agent.CellX, agent.CellY, agent.CellZ),
                    navigation,
                    world);
                if (planned.IsFailure)
                {
                    ReleaseUnroutableExcavationAssignment(job, tick);
                    continue;
                }

                TerrainWorkRoutePlan route = planned.Value;
                if (!route.Succeeded || !route.WorkCell.HasValue)
                {
                    ReleaseUnroutableExcavationAssignment(job, tick);
                    continue;
                }

                _routePlans[job.Id] = route;

                NavigationPath path = route.PathResult.Path!;
                movement[agentId] = path.Cells.Count > 1
                    ? path.Cells[1]
                    : route.WorkCell.Value;
            }

            foreach (AgentViewModel agent in agents.OrderBy(value => value.Id, StringComparer.Ordinal))
            {
                if (assignedAgentIds.Contains(agent.Id) || movement.ContainsKey(agent.Id))
                {
                    continue;
                }

                if (TryPlanResidentSleepMovement(agent, navigation, movement))
                {
                    continue;
                }

                CellId start = new CellId(agent.CellX, agent.CellY, agent.CellZ);
                UnsupportedResidentRecoveryPlan? recovery = _supportRecoveryPlanner.Plan(
                    start,
                    navigation,
                    world);
                if (recovery == null)
                {
                    continue;
                }

                movement[agent.Id] = recovery.Path.Cells.Count > 1
                    ? recovery.Path.Cells[1]
                    : recovery.Destination;
            }

            return movement;
        }


        private void ReleaseUnroutableExcavationAssignment(
            JobSnapshot job,
            long tick)
        {
            if (job.Definition is not DigJobDefinition
                || !job.AssignedAgentId.HasValue
                || _releaseAssignment == null)
            {
                return;
            }

            Result released = _releaseAssignment.Handle(
                new ReleaseJobAssignmentCommand(job.Id, tick));
            if (released.IsFailure)
            {
                return;
            }

            _excavationQuarterWork.Cancel(job.AssignedAgentId.Value);
            _routePlans.Remove(job.Id);
        }

        public IReadOnlyList<RouteViewModel> LoadRoutes()
        {
            List<RouteViewModel> routes = new List<RouteViewModel>();
            foreach (KeyValuePair<EntityId, TerrainWorkRoutePlan> pair in _routePlans
                .OrderBy(item => item.Key.ToString(), StringComparer.Ordinal))
            {
                JobSnapshot? job = _jobRepository.Get().Get(pair.Key);
                if (job is null || !job.AssignedAgentId.HasValue)
                {
                    continue;
                }

                routes.Add(_routePresenter.Present(
                    pair.Value,
                    job.AssignedAgentId.Value));
            }

            routes.AddRange(LoadHaulingRoutes());
            routes.AddRange(LoadBuildingPackingRoutes());
            routes.AddRange(LoadBuildingBoxPickupRoutes());
            routes.AddRange(LoadWorldItemPickupRoutes());
            routes.AddRange(LoadBuildingBoxAssemblyRoutes());
            routes.AddRange(LoadBuildingProductionRoutes());
            return routes;
        }

        internal Result RefreshCommittedTerrainNavigation()
        {
            return RefreshNavigation();
        }

        private Result RefreshNavigation()
        {
            IReadOnlyList<ChunkId> dirty = _worldSession.PeekDirtyChunks();
            if (dirty.Count == 0)
            {
                return Result.Success();
            }

            Result<NavigationUpdateDiagnostics> refreshed =
                new RefreshNavigationCommandHandler(_navigationRepository).Handle(
                    new RefreshNavigationCommand(
                        _profile.Id,
                        _worldSession.LoadSnapshot(),
                        dirty,
                        Array.Empty<TraversalLink>()));
            if (refreshed.IsFailure)
            {
                return Result.Failure(refreshed.Error!);
            }

            _worldSession.DrainDirtyChunks();
            return Result.Success();
        }
    }
}
