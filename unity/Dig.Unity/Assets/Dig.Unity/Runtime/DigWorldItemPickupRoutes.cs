using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Presentation.Navigation;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
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
    }
}
