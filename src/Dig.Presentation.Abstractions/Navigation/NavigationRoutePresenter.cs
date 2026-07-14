using System;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Core;

namespace Dig.Presentation.Navigation
{

public sealed class NavigationRoutePresenter
{
    public RouteViewModel Present(TerrainWorkRoutePlan plan, EntityId agentId)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        RouteCellViewModel[] cells = plan.PathResult.Path is null
            ? Array.Empty<RouteCellViewModel>()
            : plan.PathResult.Path.Cells
                .Select(cell => new RouteCellViewModel(cell.X, cell.Y))
                .ToArray();
        int workX = plan.WorkCell?.X ?? plan.TargetCell.X;
        int workY = plan.WorkCell?.Y ?? plan.TargetCell.Y;
        return new RouteViewModel(
            plan.JobId.ToString(),
            agentId.ToString(),
            workX,
            workY,
            plan.Succeeded,
            plan.PathResult.Diagnostics.Detail,
            plan.PathResult.Path?.TotalCost ?? 0,
            plan.PathResult.Diagnostics.SnapshotVersion,
            cells);
    }
}

}
