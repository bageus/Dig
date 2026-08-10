using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Application.World;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal readonly struct SpatialExcavationCommit
{
    internal SpatialExcavationCommit(EntityId jobId, CellId target)
    {
        JobId = jobId;
        Target = target;
    }

    internal EntityId JobId { get; }

    internal CellId Target { get; }
}

internal sealed partial class DigTerrainWorkSession
{
    private readonly Dictionary<CellId, EntityId> _spatialDigJobs =
        new Dictionary<CellId, EntityId>();

    internal Result DesignateSpatialExcavation(
        TunnelDepthExcavationPlan plan,
        IReadOnlyList<AgentViewModel> agents,
        int priority,
        long tick)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        RequireSpatialExcavationInitialized();
        Result designated = EnsureSpatialExcavationDesignation(plan.Target, tick);
        if (designated.IsFailure)
        {
            return designated;
        }

        if (TryGetActiveSpatialJob(plan.Target, out _))
        {
            return Result.Success();
        }

        EntityId jobId = _dynamicIds!.Next();
        SpatialDigJobDefinition definition = new SpatialDigJobDefinition(
            jobId,
            new SpatialDigJobTarget(plan.Target, plan.WorkCell),
            priority,
            tick,
            new JobRetryPolicy(maximumRetries: 2, retryDelayTicks: 3));
        JobSystem jobs = _jobRepository.Get();
        Result added = jobs.Add(definition);
        if (added.IsFailure)
        {
            return added;
        }

        Result available = jobs.MakeAvailable(jobId, tick);
        if (available.IsFailure)
        {
            return available;
        }

        _spatialDigJobs[plan.Target] = jobId;
        _jobRepository.Save(jobs);
        _journal.Append(jobs.DequeueUncommittedEvents());
        SynchronizeSpatialExcavations(tick, agents);
        return Result.Success();
    }

    internal void SynchronizeSpatialExcavations(
        long tick,
        IReadOnlyList<AgentViewModel> agents)
    {
        if (agents == null)
        {
            throw new ArgumentNullException(nameof(agents));
        }

        RequireSpatialExcavationInitialized();
        foreach (JobSnapshot job in LoadActiveSpatialJobs())
        {
            if (job.Status != JobStatus.Available)
            {
                continue;
            }

            CellId work = ((SpatialDigJobDefinition)job.Definition).Target.WorkCell;
            _candidateProvider!.SetCandidates(
                job.Id,
                CreateSpatialCandidates(agents, work));
        }

        AssignNearestAutomaticSpatialJobs(agents, tick);
        _assignmentHandler!.Handle(new AssignAvailableJobsCommand(tick));
    }

    internal bool TryAssignSpatialExcavation(
        CellId workCell,
        IReadOnlyList<string> residentIds,
        long tick,
        out Result result)
    {
        return TryAssignSpatialExcavationGroup(
            workCell,
            residentIds,
            tick,
            out result);
    }

    internal Result AdvanceSpatialExcavationWork(
        long tick,
        IReadOnlyList<AgentViewModel> agents)
    {
        Dictionary<string, AgentViewModel> byId = agents.ToDictionary(
            value => value.Id,
            StringComparer.Ordinal);
        JobSystem jobs = _jobRepository.Get();
        bool changed = false;
        foreach (JobSnapshot snapshot in LoadActiveSpatialJobs())
        {
            if (!snapshot.AssignedAgentId.HasValue
                || !byId.TryGetValue(
                    snapshot.AssignedAgentId.Value.ToString(),
                    out AgentViewModel? agent))
            {
                continue;
            }

            if (!IsAtPreciseWorkPose(snapshot, agent))
            {
                continue;
            }

            Result advanced;
            if (snapshot.Status == JobStatus.Claimed)
            {
                advanced = jobs.Start(snapshot.Id, tick);
            }
            else if (snapshot.Stage == JobStageKind.TravelToTarget)
            {
                advanced = jobs.AdvanceStage(snapshot.Id, tick);
            }
            else if (snapshot.Stage == JobStageKind.PerformWork)
            {
                SpatialDigJobDefinition definition =
                    (SpatialDigJobDefinition)snapshot.Definition;
                CellSnapshot target = RequireExcavationCell(
                    definition.Target.TargetCell);
                if (target.IsSolid
                    && target.State.Designation != CellDesignation.Dig)
                {
                    Result cancelled = jobs.Cancel(
                        snapshot.Id,
                        new JobBlockReason(
                            "designation_erased",
                            "The spatial excavation designation is no longer active."),
                        tick);
                    if (cancelled.IsFailure)
                    {
                        return cancelled;
                    }

                    _spatialDigJobs.Remove(definition.Target.TargetCell);
                    CompleteExcavationQuarterTarget(definition.Target.TargetCell);
                    changed = true;
                    continue;
                }

                TerrainWorkPosture posture = _routePlans.TryGetValue(
                        snapshot.Id,
                        out TerrainWorkRoutePlan? route)
                    ? route.Posture
                    : TerrainWorkPosture.Standing;
                bool quartersComplete = AdvanceExcavationQuarterWork(
                    snapshot.AssignedAgentId.Value,
                    new ExcavationWorkTarget(
                        definition.Target.TargetCell,
                        definition.Target.TargetCell.Z),
                    new CellId(agent.CellX, agent.CellY, agent.CellZ),
                    posture,
                    tick);
                if (!quartersComplete)
                {
                    continue;
                }

                advanced = jobs.AdvanceStage(snapshot.Id, tick);
            }
            else
            {
                continue;
            }

            if (advanced.IsFailure)
            {
                return advanced;
            }

            changed = true;
        }

        if (changed)
        {
            _jobRepository.Save(jobs);
            _journal.Append(jobs.DequeueUncommittedEvents());
        }

        return Result.Success();
    }

    internal IReadOnlyList<SpatialExcavationCommit> LoadSpatialExcavationsToFinalize()
    {
        return LoadActiveSpatialJobs()
            .Where(value => value.Status == JobStatus.InProgress
                && value.Stage == JobStageKind.Finalize)
            .Select(value => new SpatialExcavationCommit(
                value.Id,
                ((SpatialDigJobDefinition)value.Definition).Target.TargetCell))
            .ToArray();
    }

    internal Result CompleteSpatialExcavationJob(EntityId jobId, long tick)
    {
        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(jobId);
        if (job == null || job.Definition is not SpatialDigJobDefinition spatial)
        {
            return Result.Failure(JobErrors.NotFound);
        }

        Result completed = jobs.Complete(jobId, tick);
        if (completed.IsFailure)
        {
            return completed;
        }

        _spatialDigJobs.Remove(spatial.Target.TargetCell);
        CompleteExcavationQuarterTarget(spatial.Target.TargetCell);
        _jobRepository.Save(jobs);
        _journal.Append(jobs.DequeueUncommittedEvents());
        _worldChanged = true;
        return Result.Success();
    }

    private IReadOnlyList<JobSnapshot> LoadActiveSpatialJobs()
    {
        return _jobRepository.Get().GetAll()
            .Where(value => value.Definition is SpatialDigJobDefinition && !value.IsTerminal)
            .ToArray();
    }

    private bool TryGetActiveSpatialJob(
        CellId target,
        out JobSnapshot? job)
    {
        job = null;
        if (!_spatialDigJobs.TryGetValue(target, out EntityId jobId))
        {
            return false;
        }

        job = _jobRepository.Get().Get(jobId);
        return job != null && !job.IsTerminal;
    }

    private IReadOnlyList<JobCandidate> CreateSpatialCandidates(
        IReadOnlyList<AgentViewModel> agents,
        CellId workCell)
    {
        return agents.Select((agent, index) => new JobCandidate(
                EntityId.Parse(agent.Id),
                skillLevel: 5_000 - (index * 250),
                distanceCost: Math.Abs(agent.CellX - workCell.X)
                    + Math.Abs(agent.CellY - workCell.Y)
                    + Math.Abs(agent.CellZ - workCell.Z),
                isAvailable: IsAvailableForAutomaticWork(agent)
                    && string.Equals(
                        agent.ScheduledActivity,
                        ScheduleActivity.Work.ToString(),
                        StringComparison.Ordinal)))
            .ToArray();
    }

    private void RequireSpatialExcavationInitialized()
    {
        if (_dynamicIds == null
            || _candidateProvider == null
            || _assignmentHandler == null
            || _specificAssignment == null
            || _releaseAssignment == null)
        {
            throw new InvalidOperationException(
                "Spatial excavation requires initialized dynamic designations.");
        }
    }
}

}
