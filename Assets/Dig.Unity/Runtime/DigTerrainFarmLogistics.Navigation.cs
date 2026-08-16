using System.Collections.Generic;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private bool TryPlanFarmLogisticsMovement(
        JobSnapshot job,
        AgentViewModel agent,
        NavigationSnapshot navigation,
        IDictionary<string, CellId> movement,
        long tick)
    {
        if (!IsFarmLogisticsJob(job.Id)) return false;
        CellId? destination = ResolveFarmLogisticsDestination(job);
        if (!destination.HasValue) return true;
        CellId start = new CellId(agent.CellX, agent.CellY, agent.CellZ);
        PathResult path = new NavigationPathfinder().FindPath(
            navigation,
            new PathRequest(start, destination.Value, navigation.NavigationVersion));
        if (!path.Succeeded || path.Path == null)
        {
            if (job.AssignedAgentId.HasValue && _releaseAssignment != null)
            {
                Result released = _releaseAssignment.Handle(
                    new ReleaseJobAssignmentCommand(job.Id, tick));
                if (released.IsSuccess)
                {
                    _farmSlotClaims?.Release(job.Id, tick);
                    _routePlans.Remove(job.Id);
                }
            }
            return true;
        }

        _routePlans[job.Id] = new TerrainWorkRoutePlan(
            job.Id, destination.Value, destination, path, candidateCount: 1);
        movement[agent.Id] = path.Path.Cells.Count > 1
            ? path.Path.Cells[1]
            : destination.Value;
        return true;
    }
}

}
