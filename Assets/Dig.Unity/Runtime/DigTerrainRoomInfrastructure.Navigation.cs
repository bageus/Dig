using System;
using System.Collections.Generic;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private bool TryPlanRoomUpgradeMovement(
        JobSnapshot job,
        AgentViewModel agent,
        NavigationSnapshot navigation,
        IDictionary<string, CellId> movement,
        long tick)
    {
        if (!IsRoomUpgradeJob(job.Id))
        {
            return false;
        }

        CellId? destination = ResolveRoomUpgradeDestination(job);
        if (!destination.HasValue)
        {
            return true;
        }

        CellId start = new CellId(agent.CellX, agent.CellY, agent.CellZ);
        PathResult path = new NavigationPathfinder().FindPath(
            navigation,
            new PathRequest(start, destination.Value, navigation.NavigationVersion));
        if (!path.Succeeded || path.Path == null)
        {
            ReleaseRoomUpgradeAssignment(job, tick);
            return true;
        }

        _routePlans[job.Id] = new TerrainWorkRoutePlan(
            job.Id,
            destination.Value,
            destination,
            path,
            candidateCount: 1);
        movement[agent.Id] = path.Path.Cells.Count > 1
            ? path.Path.Cells[1]
            : destination.Value;
        return true;
    }

    private CellId? ResolveRoomUpgradeDestination(JobSnapshot job)
    {
        if (job.Definition is RoomUpgradeWorkJobDefinition work)
        {
            return work.WorkCell;
        }

        if (job.Definition is not HaulJobDefinition hauling)
        {
            return null;
        }

        if (job.Status == JobStatus.Claimed
            || job.Stage == JobStageKind.AcquireItem)
        {
            ItemStackSnapshot? source =
                _inventoryRepository.Get().GetStack(hauling.SourceStackId);
            return source?.Location.HasCell == true
                ? source.Location.CellId
                : (CellId?)null;
        }

        return hauling.Destination.HasCell
            ? hauling.Destination.CellId
            : (CellId?)null;
    }

    private void ReleaseRoomUpgradeAssignment(JobSnapshot job, long tick)
    {
        if (!job.AssignedAgentId.HasValue || _releaseAssignment == null)
        {
            return;
        }

        Result released = _releaseAssignment.Handle(
            new ReleaseJobAssignmentCommand(job.Id, tick));
        if (released.IsSuccess)
        {
            _haulingSlotClaims?.Release(job.Id, tick);
            _routePlans.Remove(job.Id);
        }
    }
}

}
