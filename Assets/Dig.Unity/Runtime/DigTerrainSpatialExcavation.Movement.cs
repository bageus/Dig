using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    internal IReadOnlyDictionary<string, SurfacePose> PlanPreciseWorkMovement(
        IReadOnlyList<AgentViewModel> agents)
    {
        Dictionary<string, AgentViewModel> byId = agents.ToDictionary(
            value => value.Id,
            StringComparer.Ordinal);
        Dictionary<string, SurfacePose> result =
            new Dictionary<string, SurfacePose>(StringComparer.Ordinal);
        foreach (JobSnapshot job in _jobRepository.Get().GetAll()
            .Where(IsActive))
        {
            if (!job.AssignedAgentId.HasValue)
            {
                continue;
            }

            string residentId = job.AssignedAgentId.Value.ToString();
            if (!byId.TryGetValue(residentId, out AgentViewModel? agent))
            {
                continue;
            }

            if (!TryResolvePreciseWorkPose(job, out SurfacePose workPose))
            {
                continue;
            }
            if (!WorkSurfacePositioning.IsAt(ToSurfacePose(agent), workPose))
            {
                result[residentId] = workPose;
            }
        }

        return result;
    }

    private bool TryResolvePreciseWorkPose(
        JobSnapshot job,
        out SurfacePose pose)
    {
        if (job.Definition is SpatialDigJobDefinition spatial)
        {
            pose = WorkSurfacePositioning.Resolve(
                spatial.Target.WorkCell,
                spatial.Target.TargetCell);
            return true;
        }

        if (job.Definition is MushroomChopJobDefinition mushroom)
        {
            pose = WorkSurfacePositioning.Resolve(
                mushroom.WorkPosition,
                mushroom.TargetCell);
            return true;
        }

        if (job.Definition is WorldItemPickupJobDefinition pickup)
        {
            pose = WorkSurfacePositioning.Resolve(
                pickup.SourceCell,
                pickup.SourceCell);
            return true;
        }

        if (job.Definition is BuildingBoxAssemblyJobDefinition assembly)
        {
            ItemStackSnapshot? box = _buildingInventoryRepository?.Get().GetStack(
                assembly.SourceStackId);
            if ((job.Status == JobStatus.Claimed
                    || job.Stage == JobStageKind.AcquireItem)
                && box?.Location.HasCell == true)
            {
                pose = WorkSurfacePositioning.Resolve(
                    box.Location.CellId,
                    box.Location.CellId);
                return true;
            }

            pose = WorkSurfacePositioning.Resolve(
                assembly.WorkPosition,
                assembly.SiteCell);
            return true;
        }

        if (job.Definition is BuildingBoxPackingJobDefinition packing)
        {
            pose = WorkSurfacePositioning.Resolve(
                packing.WorkPosition,
                packing.WorkPosition);
            return true;
        }

        if (job.Definition is BuildingBoxPickupJobDefinition buildingBox)
        {
            CellId target = buildingBox.DestinationCell.HasValue
                && job.Stage != JobStageKind.TravelToTarget
                && job.Stage != JobStageKind.AcquireItem
                    ? buildingBox.DestinationCell.Value
                    : buildingBox.SourceCell;
            pose = WorkSurfacePositioning.Resolve(target, target);
            return true;
        }

        if (job.Definition is HaulJobDefinition hauling)
        {
            CellId? target = ResolveHaulingTarget(job, hauling);
            if (target.HasValue)
            {
                pose = WorkSurfacePositioning.Resolve(target.Value, target.Value);
                return true;
            }
        }

        if (job.Definition is ProductionWorkJobDefinition production)
        {
            return TryResolveProductionWorkPose(job, production, out pose);
        }

        if (job.Definition is BuildingSupplyJobDefinition supply)
        {
            return TryResolveBuildingSupplyPose(job, supply, out pose);
        }

        pose = default;
        return false;
    }

    private bool IsAtPreciseWorkPose(JobSnapshot job, AgentViewModel agent)
    {
        if (!TryResolvePreciseWorkPose(job, out SurfacePose required))
        {
            return true;
        }

        SurfacePose actual = ToSurfacePose(agent);
        if (WorkSurfacePositioning.IsAt(actual, required))
        {
            return true;
        }

        if ((job.Definition is WorldItemPickupJobDefinition
                || job.Definition is HaulJobDefinition)
            && actual.Cell == required.Cell)
        {
            return true;
        }

        return job.Definition is SpatialDigJobDefinition
            && actual.Cell == required.Cell
            && actual.IsVertical
            && !HasFullStandingSupport(required.Cell);
    }

    private static SurfacePose ToSurfacePose(AgentViewModel agent)
    {
        return new SurfacePose(
            new CellId(agent.CellX, agent.CellY, agent.CellZ),
            agent.SurfaceFace,
            agent.SurfaceU,
            agent.SurfaceV);
    }
}

}
