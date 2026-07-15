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
            IReadOnlyList<AgentViewModel> agents)
        {
            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
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
            Dictionary<string, AgentViewModel> agentsById = agents.ToDictionary(
                agent => agent.Id,
                StringComparer.Ordinal);
            Dictionary<string, CellId> movement =
                new Dictionary<string, CellId>(StringComparer.Ordinal);
            _routePlans.Clear();
            _haulingRoutes.Clear();
            _buildingPackingRoutes.Clear();
            foreach (JobSnapshot job in _jobRepository.Get().GetAll())
            {
                if (!IsActive(job) || !job.AssignedAgentId.HasValue)
                {
                    continue;
                }

                string agentId = job.AssignedAgentId.Value.ToString();
                if (!agentsById.TryGetValue(agentId, out AgentViewModel? agent))
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
                    new CellId(agent.CellX, agent.CellY),
                    navigation);
                if (planned.IsFailure)
                {
                    continue;
                }

                TerrainWorkRoutePlan route = planned.Value;
                _routePlans[job.Id] = route;
                if (!route.Succeeded || !route.WorkCell.HasValue)
                {
                    continue;
                }

                NavigationPath path = route.PathResult.Path!;
                movement[agentId] = path.Cells.Count > 1
                    ? path.Cells[1]
                    : route.WorkCell.Value;
            }

            return movement;
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
            return routes;
        }

        private Result RefreshNavigation()
        {
            IReadOnlyList<ChunkId> dirty = _worldSession.DrainDirtyChunks();
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
            return refreshed.IsFailure
                ? Result.Failure(refreshed.Error!)
                : Result.Success();
        }
    }
}
