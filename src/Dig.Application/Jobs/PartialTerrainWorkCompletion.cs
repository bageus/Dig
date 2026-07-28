using System;
using Dig.Application.Agents;
using Dig.Application.Messaging;
using Dig.Application.World;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Jobs
{

public sealed class CompletePartialTerrainWorkCommand : ICommand<Result>
{
    public CompletePartialTerrainWorkCommand(
        EntityId jobId,
        ExcavationQuarter requiredQuarters,
        long tick)
    {
        if (requiredQuarters == ExcavationQuarter.None
            || requiredQuarters == ExcavationQuarter.All)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredQuarters));
        }

        JobId = jobId;
        RequiredQuarters = requiredQuarters;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public ExcavationQuarter RequiredQuarters { get; }
    public long Tick { get; }
}

public sealed class CompletePartialTerrainWorkCommandHandler
    : ICommandHandler<CompletePartialTerrainWorkCommand, Result>
{
    private readonly IJobRepository _jobs;
    private readonly IWorldRepository _world;
    private readonly IEventSink _events;
    private readonly IAgentSkillGrantService _skills;

    public CompletePartialTerrainWorkCommandHandler(
        IJobRepository jobs,
        IWorldRepository world,
        IEventSink events,
        IAgentSkillGrantService skills)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _skills = skills ?? throw new ArgumentNullException(nameof(skills));
    }

    public Result Handle(CompletePartialTerrainWorkCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (command.Tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command.Tick));
        }

        JobSystem jobs = _jobs.Get();
        JobSnapshot? snapshot = jobs.Get(command.JobId);
        if (snapshot == null)
        {
            return Result.Failure(JobErrors.NotFound);
        }

        if (snapshot.Definition is not DigJobDefinition definition)
        {
            return Result.Failure(TerrainWorkCompletionErrors.JobTypeUnsupported);
        }

        if (snapshot.Status != JobStatus.InProgress
            || snapshot.Stage != JobStageKind.Finalize)
        {
            return Result.Failure(TerrainWorkCompletionErrors.JobNotReady);
        }

        EntityId worker = snapshot.AssignedAgentId
            ?? throw new InvalidOperationException(
                "An in-progress terrain job must retain its worker.");
        SkillGrantBundle skill = new SkillGrantBundle(
            worker,
            SkillGrantSourceKind.JobCompleted,
            definition.Id.ToString(),
            command.Tick,
            definition.SkillGrantProfile.Multiply(1));
        Result skillValidation = _skills.Validate(skill);
        if (skillValidation.IsFailure)
        {
            return skillValidation;
        }

        WorldState world = _world.Get();
        Result<CellSnapshot> targetResult = world.GetCell(definition.Target.CellId);
        if (targetResult.IsFailure)
        {
            return Result.Failure(targetResult.Error!);
        }

        CellSnapshot target = targetResult.Value;
        if (!target.IsSolid)
        {
            return Result.Failure(TerrainWorkCompletionErrors.TargetNotSolid);
        }

        if (target.State.Designation != CellDesignation.Dig)
        {
            return Result.Failure(TerrainWorkCompletionErrors.TargetNotDesignated);
        }

        if ((target.State.CompletedExcavationQuarters & command.RequiredQuarters)
            != command.RequiredQuarters)
        {
            return Result.Failure(TerrainWorkCompletionErrors.JobNotReady);
        }

        Result<WorldMutationResult> undesignated = world.SetDigDesignation(
            definition.Target.CellId,
            designated: false,
            command.Tick);
        EnsureCommit(undesignated.IsSuccess, undesignated.Error);
        Result completed = jobs.Complete(command.JobId, command.Tick);
        EnsureCommit(completed.IsSuccess, completed.Error);
        Result<SkillRedistributionReport> applied = _skills.ApplyConfirmed(skill);
        EnsureCommit(applied.IsSuccess, applied.Error);

        _world.Save(world);
        _jobs.Save(jobs);
        _events.Append(world.DequeueUncommittedEvents());
        _events.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }

    private static void EnsureCommit(bool succeeded, DomainError? error)
    {
        if (!succeeded)
        {
            throw new InvalidOperationException(
                $"A validated partial terrain work commit failed: {error}");
        }
    }
}

}
