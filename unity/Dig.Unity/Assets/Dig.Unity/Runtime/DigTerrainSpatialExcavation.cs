using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Application.World;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal readonly struct SpatialExcavationCommit
{
    internal SpatialExcavationCommit(EntityId jobId, SpatialCellId target)
    {
        JobId = jobId;
        Target = target;
    }

    internal EntityId JobId { get; }

    internal SpatialCellId Target { get; }
}

internal sealed partial class DigTerrainWorkSession
{
    private const int SpatialExcavationWorkCadence = 3;
    private readonly Dictionary<SpatialCellId, EntityId> _spatialDigJobs =
        new Dictionary<SpatialCellId, EntityId>();

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
        if (TryGetActiveSpatialJob(plan.Target, out _))
        {
            return Result.Success();
        }

        EntityId jobId = _dynamicIds!.Next();
        SpatialDigJobDefinition definition = new SpatialDigJobDefinition(
            jobId,
            new SpatialDigJobTarget(plan.Target, plan.Source),
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
        _candidateProvider!.SetCandidates(
            jobId,
            CreateSpatialCandidates(agents, plan.Source));
        _jobRepository.Save(jobs);
        _journal.Append(jobs.DequeueUncommittedEvents());
        _assignmentHandler!.Handle(new AssignAvailableJobsCommand(tick));
        return Result.Success();
    }

    internal bool TryAssignSpatialExcavation(
        SpatialCellId workCell,
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

    internal IReadOnlyDictionary<string, SpatialCellId> PlanSpatialExcavationMovement(
        IReadOnlyList<AgentViewModel> agents)
    {
        Dictionary<string, AgentViewModel> byId = agents.ToDictionary(
            value => value.Id,
            StringComparer.Ordinal);
        Dictionary<string, SpatialCellId> result =
            new Dictionary<string, SpatialCellId>(StringComparer.Ordinal);
        foreach (JobSnapshot job in LoadActiveSpatialJobs())
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

            SpatialCellId work = ((SpatialDigJobDefinition)job.Definition).Target.WorkCell;
            if (agent.CellX != work.X || agent.CellY != work.Y || agent.CellZ != work.Z)
            {
                result[residentId] = work;
            }
        }

        return result;
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

            SpatialCellId work = ((SpatialDigJobDefinition)snapshot.Definition)
                .Target.WorkCell;
            if (agent.CellX != work.X || agent.CellY != work.Y || agent.CellZ != work.Z)
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
            else if (snapshot.Stage == JobStageKind.PerformWork
                && tick % SpatialExcavationWorkCadence == 0)
            {
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
        SpatialCellId target,
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

    private static IReadOnlyList<JobCandidate> CreateSpatialCandidates(
        IReadOnlyList<AgentViewModel> agents,
        SpatialCellId workCell)
    {
        return agents.Select((agent, index) => new JobCandidate(
                EntityId.Parse(agent.Id),
                skillLevel: 5_000 - (index * 250),
                distanceCost: Math.Abs(agent.CellX - workCell.X)
                    + Math.Abs(agent.CellY - workCell.Y)
                    + Math.Abs(agent.CellZ - workCell.Z),
                isAvailable: agent.IsAlive
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