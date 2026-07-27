using System;
using Dig.Application.Agents;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;

namespace Dig.Application.Ecology
{

public sealed class StartDirectMushroomChopCommandHandler
    : ICommandHandler<StartDirectMushroomChopCommand, Result<MushroomChopStartedResult>>
{
    private readonly IMushroomRepository _mushrooms;
    private readonly IJobRepository _jobs;
    private readonly IAgentSkillLevelReader _skills;
    private readonly IMushroomSwingRandom _random;
    private readonly IEventSink _events;

    public StartDirectMushroomChopCommandHandler(
        IMushroomRepository mushrooms,
        IJobRepository jobs,
        IAgentSkillLevelReader skills,
        IMushroomSwingRandom random,
        IEventSink events)
    {
        _mushrooms = mushrooms ?? throw new ArgumentNullException(nameof(mushrooms));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _skills = skills ?? throw new ArgumentNullException(nameof(skills));
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result<MushroomChopStartedResult> Handle(StartDirectMushroomChopCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        MushroomState mushrooms = _mushrooms.Get();
        MushroomSiteSnapshot? site = mushrooms.Get(command.SiteId);
        if (site is null)
        {
            return Result<MushroomChopStartedResult>.Failure(MushroomErrors.NotFound);
        }

        if (!site.IsVisible)
        {
            return Result<MushroomChopStartedResult>.Failure(MushroomErrors.NotVisible);
        }

        Result<int> skill = _skills.GetSkillUnits(command.WorkerId, AgentSkillCatalog.Woodworking);
        if (skill.IsFailure)
        {
            return Result<MushroomChopStartedResult>.Failure(skill.Error!);
        }

        (int minimum, int maximum) = MushroomDefinition.GetRequiredSwingBand(skill.Value);
        int requiredSwings = _random.SelectRequiredSwings(
            command.SiteId,
            command.WorkerId,
            minimum,
            maximum);
        JobSystem jobs = _jobs.Get();
        EntityId? replacedJobId = site.ActiveChopJobId;
        if (replacedJobId.HasValue)
        {
            Result takeover = CancelExistingAttempt(
                mushrooms,
                jobs,
                site,
                command.Tick);
            if (takeover.IsFailure)
            {
                return Result<MushroomChopStartedResult>.Failure(takeover.Error!);
            }
        }

        MushroomChopJobDefinition definition = new MushroomChopJobDefinition(
            command.JobId,
            command.SiteId,
            site.Cell,
            command.WorkPosition,
            site.GrowthGeneration,
            requiredSwings,
            command.Priority,
            command.Tick,
            JobRetryPolicy.Default);
        Result added = jobs.Add(definition);
        if (added.IsFailure)
        {
            return Result<MushroomChopStartedResult>.Failure(added.Error!);
        }

        EnsureCommitStep(jobs.MakeAvailable(command.JobId, command.Tick));
        EnsureCommitStep(jobs.Claim(command.JobId, command.WorkerId, command.Tick));
        EnsureCommitStep(jobs.Start(command.JobId, command.Tick));
        EnsureCommitStep(mushrooms.BeginChop(
            command.SiteId,
            command.JobId,
            command.WorkerId,
            requiredSwings,
            command.Tick));
        SaveAndPublish(mushrooms, jobs);
        return Result<MushroomChopStartedResult>.Success(new MushroomChopStartedResult(
            command.JobId,
            command.SiteId,
            command.WorkerId,
            requiredSwings,
            replacedJobId));
    }

    private static Result CancelExistingAttempt(
        MushroomState mushrooms,
        JobSystem jobs,
        MushroomSiteSnapshot site,
        long tick)
    {
        EntityId oldJobId = site.ActiveChopJobId!.Value;
        EntityId oldWorkerId = site.ActiveWorkerId!.Value;
        JobSnapshot? oldJob = jobs.Get(oldJobId);
        if (oldJob is null || oldJob.IsTerminal)
        {
            return Result.Failure(MushroomApplicationErrors.GenerationConflict);
        }

        Result cancelled = jobs.Cancel(
            oldJobId,
            new JobBlockReason(
                "mushroom_direct_takeover",
                "A new direct mushroom order replaced this worker."),
            tick);
        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        return mushrooms.ReleaseChop(site.SiteId, oldJobId, oldWorkerId, tick);
    }

    private void SaveAndPublish(MushroomState mushrooms, JobSystem jobs)
    {
        _mushrooms.Save(mushrooms);
        _jobs.Save(jobs);
        _events.Append(mushrooms.DequeueUncommittedEvents());
        _events.Append(jobs.DequeueUncommittedEvents());
    }

    private static void EnsureCommitStep(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Validated mushroom start failed: {result.Error}");
        }
    }
}

public sealed class ArriveAtMushroomCommandHandler
    : ICommandHandler<ArriveAtMushroomCommand, Result>
{
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;

    public ArriveAtMushroomCommandHandler(IJobRepository jobs, IEventSink events)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result Handle(ArriveAtMushroomCommand command)
    {
        JobSystem jobs = _jobs.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not MushroomChopJobDefinition
            || job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.TravelToTarget)
        {
            return Result.Failure(job is null
                ? JobErrors.NotFound
                : MushroomApplicationErrors.JobNotReady);
        }

        Result result = jobs.AdvanceStage(command.JobId, command.Tick);
        if (result.IsSuccess)
        {
            _jobs.Save(jobs);
            _events.Append(jobs.DequeueUncommittedEvents());
        }

        return result;
    }
}

public sealed class CompleteMushroomSwingCommandHandler
    : ICommandHandler<CompleteMushroomSwingCommand, Result<bool>>
{
    private readonly IMushroomRepository _mushrooms;
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;

    public CompleteMushroomSwingCommandHandler(
        IMushroomRepository mushrooms,
        IJobRepository jobs,
        IEventSink events)
    {
        _mushrooms = mushrooms ?? throw new ArgumentNullException(nameof(mushrooms));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result<bool> Handle(CompleteMushroomSwingCommand command)
    {
        JobSystem jobs = _jobs.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not MushroomChopJobDefinition definition)
        {
            return Result<bool>.Failure(job is null
                ? JobErrors.NotFound
                : MushroomApplicationErrors.JobTypeUnsupported);
        }

        if (job.Status != JobStatus.InProgress || job.Stage != JobStageKind.PerformWork)
        {
            return Result<bool>.Failure(MushroomApplicationErrors.JobNotReady);
        }

        EntityId workerId = job.AssignedAgentId
            ?? throw new InvalidOperationException("An in-progress mushroom job must retain its worker.");
        MushroomState mushrooms = _mushrooms.Get();
        Result<bool> swing = mushrooms.CompleteSwing(
            definition.SiteId,
            command.JobId,
            workerId,
            command.Tick);
        if (swing.IsFailure)
        {
            return swing;
        }

        if (swing.Value)
        {
            EnsureCommitStep(jobs.AdvanceStage(command.JobId, command.Tick));
        }

        _mushrooms.Save(mushrooms);
        _jobs.Save(jobs);
        _events.Append(mushrooms.DequeueUncommittedEvents());
        _events.Append(jobs.DequeueUncommittedEvents());
        return swing;
    }

    private static void EnsureCommitStep(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Validated mushroom swing failed: {result.Error}");
        }
    }
}

}
