using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private AcquireHaulingItemHandler? _genericHaulingAcquisition;
    private CompleteHaulingJobHandler? _genericHaulingCompletion;
    private HaulingResidentSlotClaimService? _genericHaulingSlotClaims;
    private ReconcileHaulingHandler? _genericHaulingReconciliation;
    private ulong _genericHaulingRuntimeSequence = 1UL;

    internal Result SynchronizeGenericHauling(
        IReadOnlyList<AgentViewModel> agents,
        long tick,
        int maximumJobs = 8,
        int priority = 500)
    {
        if (agents == null) throw new ArgumentNullException(nameof(agents));
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        EnsureGenericHaulingRuntime();
        _genericHaulingCandidates ??= new InMemoryJobCandidateProvider();
        _genericHaulingJobIds ??= new RuntimeGenericHaulingJobIds(this);
        _genericHaulingPlanner ??= new PlanHaulingHandler(
            _inventoryRepository,
            _storageRepository,
            _jobRepository,
            _genericHaulingJobIds,
            _journal);
        _genericHaulingAssignment ??= new AssignAvailableJobsHandler(
            _jobRepository,
            new InventoryTravelCostJobCandidateProvider(
                _genericHaulingCandidates,
                _inventoryRepository),
            _journal,
            haulingResidentSlotClaims: _genericHaulingSlotClaims);

        HaulingPlanningReport planning = _genericHaulingPlanner.Handle(
            new PlanHaulingCommand(maximumJobs, priority, tick));
        foreach (PlannedHaulingJob planned in planning.Created)
        {
            JobSnapshot? job = _jobRepository.Get().Get(planned.JobId);
            if (job?.Definition is HaulJobDefinition haul)
            {
                ItemStackSnapshot? source = _inventoryRepository.Get().GetStack(haul.SourceStackId);
                if (source?.Location.HasCell == true)
                {
                    _genericHaulingCandidates.SetCandidates(
                        job.Id,
                        CreateGenericHaulingCandidates(agents, source.Location.CellId));
                }
            }
        }
        _genericHaulingAssignment.Handle(new AssignAvailableJobsCommand(tick));
        return Result.Success();
    }

    private IReadOnlyList<JobCandidate> CreateGenericHaulingCandidates(
        IReadOnlyList<AgentViewModel> agents,
        CellId target)
    {
        return agents.Select((agent, index) => new JobCandidate(
            EntityId.Parse(agent.Id),
            5_000 - (index * 250),
            Math.Abs(agent.CellX - target.X)
                + Math.Abs(agent.CellY - target.Y)
                + Math.Abs(agent.CellZ - target.Z),
            IsAvailableForAutomaticWork(agent))).ToArray();
    }

    private sealed class RuntimeGenericHaulingJobIds : IHaulingJobIdSource
    {
        private readonly DigTerrainWorkSession _owner;

        public RuntimeGenericHaulingJobIds(DigTerrainWorkSession owner)
        {
            _owner = owner;
        }

        public EntityId Next() => _owner.NextGenericHaulingJobId();
    }

    private EntityId NextGenericHaulingJobId()
    {
        while (true)
        {
            EntityId candidate = EntityId.Parse(
                "7330000000000000" + (_genericHaulingRuntimeSequence++).ToString("x16"));
            if (_jobRepository.Get().Get(candidate) == null) return candidate;
        }
    }

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
            _storageRepository,
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
        while (true)
        {
            EntityId candidate = EntityId.Parse(
                "7340000000000000" + (_genericHaulingRuntimeSequence++).ToString("x16"));
            if (_inventoryRepository.Get().GetStack(candidate) == null) return candidate;
        }
    }
}

}
