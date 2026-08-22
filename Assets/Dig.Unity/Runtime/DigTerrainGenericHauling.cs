using System;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private AcquireHaulingItemHandler? _genericHaulingAcquisition;
    private CompleteHaulingJobHandler? _genericHaulingCompletion;
    private HaulingResidentSlotClaimService? _genericHaulingSlotClaims;
    private ulong _genericHaulingRuntimeSequence = 1UL;

    private Result AdvanceGenericHaulingAtTarget(
        JobSnapshot job,
        AgentViewModel agent,
        long tick)
    {
        CellId? destination = ResolveGenericHaulingDestination(job);
        if (!destination.HasValue
            || agent.CellX != destination.Value.X
            || agent.CellY != destination.Value.Y
            || agent.CellZ != destination.Value.Z)
        {
            return Result.Success();
        }

        EnsureGenericHaulingRuntime();
        if (job.Status == JobStatus.Claimed)
        {
            return _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
        }

        if (job.Stage == JobStageKind.AcquireItem)
        {
            return _genericHaulingAcquisition!.Handle(new AcquireHaulingItemCommand(
                job.Id,
                NextGenericHaulingStackId(),
                tick));
        }

        if (job.Stage == JobStageKind.TravelToDestination)
        {
            return _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
        }

        if (job.Stage != JobStageKind.DepositItem)
        {
            return Result.Success();
        }

        Result completed = _genericHaulingCompletion!.Handle(new CompleteHaulingJobCommand(
            job.Id,
            NextGenericHaulingStackId(),
            tick));
        if (completed.IsSuccess)
        {
            _routePlans.Remove(job.Id);
        }

        return completed;
    }

    private CellId? ResolveGenericHaulingDestination(JobSnapshot job)
    {
        if (job.Definition is not HaulJobDefinition hauling
            || IsFarmLogisticsJob(job.Id)
            || IsRoomUpgradeJob(job.Id))
        {
            return null;
        }

        if (job.Status == JobStatus.Claimed
            || job.Stage == JobStageKind.AcquireItem)
        {
            ItemStackSnapshot? source = _inventoryRepository.Get().GetStack(
                hauling.SourceStackId);
            return source?.Location.HasCell == true
                ? source.Location.CellId
                : (CellId?)null;
        }

        return hauling.Destination.HasCell
            ? hauling.Destination.CellId
            : null;
    }

    private bool TryPlanGenericHaulingMovement(
        JobSnapshot job,
        AgentViewModel agent,
        NavigationSnapshot navigation,
        IDictionary<string, CellId> movement,
        long tick)
    {
        if (job.Definition is not HaulJobDefinition
            || IsFarmLogisticsJob(job.Id)
            || IsRoomUpgradeJob(job.Id))
        {
            return false;
        }

        CellId? destination = ResolveGenericHaulingDestination(job);
        if (!destination.HasValue)
        {
            return true;
        }

        PathResult path = new NavigationPathfinder().FindPath(
            navigation,
            new PathRequest(
                new CellId(agent.CellX, agent.CellY, agent.CellZ),
                destination.Value,
                navigation.NavigationVersion));
        if (!path.Succeeded || path.Path == null)
        {
            ReleaseGenericHaulingAssignment(job, tick);
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

    private void EnsureGenericHaulingRuntime()
    {
        _genericHaulingSlotClaims ??= new HaulingResidentSlotClaimService(
            _inventoryRepository,
            _journal);
        _genericHaulingAcquisition ??= new AcquireHaulingItemHandler(
            _inventoryRepository,
            _jobRepository,
            _journal);
        _genericHaulingCompletion ??= new CompleteHaulingJobHandler(
            _inventoryRepository,
            new InMemoryStorageRepository(),
            _jobRepository,
            _journal,
            _skillGrants);
    }

    private void ReleaseGenericHaulingAssignment(JobSnapshot job, long tick)
    {
        if (!job.AssignedAgentId.HasValue || _releaseAssignment == null)
        {
            return;
        }

        Result released = _releaseAssignment.Handle(
            new ReleaseJobAssignmentCommand(job.Id, tick));
        if (released.IsSuccess)
        {
            _genericHaulingSlotClaims?.Release(job.Id, tick);
            _routePlans.Remove(job.Id);
        }
    }

    private EntityId NextGenericHaulingStackId()
    {
        return EntityId.Parse(
            "7340000000000000" + (_genericHaulingRuntimeSequence++).ToString("x16"));
    }
}

}
