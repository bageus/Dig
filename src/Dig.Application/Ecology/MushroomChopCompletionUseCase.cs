using System;
using System.Collections.ObjectModel;
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

public sealed class CompleteMushroomChopCommandHandler
    : ICommandHandler<CompleteMushroomChopCommand, Result<MushroomChopCompletionResult>>
{
    private readonly IMushroomRepository _mushrooms;
    private readonly IJobRepository _jobs;
    private readonly IInventoryRepository _inventory;
    private readonly IAgentSkillGrantService _skills;
    private readonly IEventSink _events;

    public CompleteMushroomChopCommandHandler(
        IMushroomRepository mushrooms,
        IJobRepository jobs,
        IInventoryRepository inventory,
        IAgentSkillGrantService skills,
        IEventSink events)
    {
        _mushrooms = mushrooms ?? throw new ArgumentNullException(nameof(mushrooms));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _skills = skills ?? throw new ArgumentNullException(nameof(skills));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result<MushroomChopCompletionResult> Handle(CompleteMushroomChopCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        JobSystem jobs = _jobs.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not MushroomChopJobDefinition definition)
        {
            return Result<MushroomChopCompletionResult>.Failure(job is null
                ? JobErrors.NotFound
                : MushroomApplicationErrors.JobTypeUnsupported);
        }

        if (job.Status != JobStatus.InProgress || job.Stage != JobStageKind.Finalize)
        {
            return Result<MushroomChopCompletionResult>.Failure(
                MushroomApplicationErrors.JobNotReady);
        }

        EntityId workerId = job.AssignedAgentId
            ?? throw new InvalidOperationException("An in-progress mushroom job must retain its worker.");
        MushroomState mushrooms = _mushrooms.Get();
        MushroomSiteSnapshot? site = mushrooms.Get(definition.SiteId);
        if (site is null)
        {
            return Result<MushroomChopCompletionResult>.Failure(MushroomErrors.NotFound);
        }

        if (site.GrowthGeneration != definition.GrowthGeneration
            || site.ActiveChopJobId != command.JobId
            || site.CompletedSwings < site.RequiredSwings)
        {
            return Result<MushroomChopCompletionResult>.Failure(
                MushroomApplicationErrors.GenerationConflict);
        }

        MushroomDefinition mushroomDefinition = mushrooms.Catalog.Get(site.DefinitionId);
        MushroomDropProfile drops = mushroomDefinition.GetDrops(site.Stage);
        MushroomOutputUnits output = MushroomOutputUnits.Create(command.OutputSeedId, drops);
        InventoryState inventory = _inventory.Get();
        Result outputValidation = output.Validate(
            inventory,
            mushroomDefinition.CapItemId,
            mushroomDefinition.LegItemId);
        if (outputValidation.IsFailure)
        {
            return Result<MushroomChopCompletionResult>.Failure(outputValidation.Error!);
        }

        string sourceId = $"mushroom:{site.SiteId}:{site.GrowthGeneration + 1}";
        SkillGrantBundle skillBundle = new SkillGrantBundle(
            workerId,
            SkillGrantSourceKind.JobCompleted,
            sourceId,
            command.Tick,
            new[]
            {
                new SkillGrant(
                    AgentSkillCatalog.Woodworking,
                    MushroomDefinition.WoodworkingGrantUnits),
            });
        Result skillValidation = _skills.Validate(skillBundle);
        if (skillValidation.IsFailure)
        {
            return Result<MushroomChopCompletionResult>.Failure(skillValidation.Error!);
        }

        Result<MushroomChopCommit> committed = mushrooms.CommitChop(
            definition.SiteId,
            command.JobId,
            workerId,
            command.Tick);
        EnsureCommitStep(committed.IsSuccess, committed.Error);
        MushroomChopCommit chop = committed.Value;
        if (output.Caps.Length > 0)
        {
            EnsureCommitStep(inventory.AddUnits(
                output.Caps,
                chop.CapItemId,
                ItemLocation.InWorld(chop.Cell),
                command.Tick));
        }

        if (output.Legs.Length > 0)
        {
            EnsureCommitStep(inventory.AddUnits(
                output.Legs,
                chop.LegItemId,
                ItemLocation.InWorld(chop.Cell),
                command.Tick));
        }

        EnsureCommitStep(jobs.AdvanceStage(command.JobId, command.Tick));
        Result<SkillRedistributionReport> skill = _skills.ApplyConfirmed(skillBundle);
        EnsureCommitStep(skill.IsSuccess, skill.Error);
        _mushrooms.Save(mushrooms);
        _inventory.Save(inventory);
        _jobs.Save(jobs);
        _events.Append(mushrooms.DequeueUncommittedEvents());
        _events.Append(inventory.DequeueUncommittedEvents());
        _events.Append(jobs.DequeueUncommittedEvents());
        return Result<MushroomChopCompletionResult>.Success(
            new MushroomChopCompletionResult(
                command.JobId,
                definition.SiteId,
                chop.ChoppedStage,
                new ReadOnlyCollection<EntityId>(output.Caps),
                new ReadOnlyCollection<EntityId>(output.Legs),
                chop.SkillSourceId));
    }

    private static void EnsureCommitStep(bool succeeded, DomainError? error)
    {
        if (!succeeded)
        {
            throw new InvalidOperationException($"Validated mushroom completion failed: {error}");
        }
    }

    private static void EnsureCommitStep(Result result) =>
        EnsureCommitStep(result.IsSuccess, result.Error);
}

public sealed class CancelMushroomChopCommandHandler
    : ICommandHandler<CancelMushroomChopCommand, Result>
{
    private readonly IMushroomRepository _mushrooms;
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;

    public CancelMushroomChopCommandHandler(
        IMushroomRepository mushrooms,
        IJobRepository jobs,
        IEventSink events)
    {
        _mushrooms = mushrooms ?? throw new ArgumentNullException(nameof(mushrooms));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result Handle(CancelMushroomChopCommand command)
    {
        JobSystem jobs = _jobs.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not MushroomChopJobDefinition definition)
        {
            return Result.Failure(job is null
                ? JobErrors.NotFound
                : MushroomApplicationErrors.JobTypeUnsupported);
        }

        if (job.IsTerminal || !job.AssignedAgentId.HasValue)
        {
            return Result.Failure(MushroomApplicationErrors.JobNotReady);
        }

        MushroomState mushrooms = _mushrooms.Get();
        Result cancelled = jobs.Cancel(
            command.JobId,
            new JobBlockReason(command.ReasonCode, "Mushroom chopping was interrupted."),
            command.Tick);
        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        Result released = mushrooms.ReleaseChop(
            definition.SiteId,
            command.JobId,
            job.AssignedAgentId.Value,
            command.Tick);
        if (released.IsFailure)
        {
            throw new InvalidOperationException(
                $"Cancelled mushroom job could not release its target: {released.Error}");
        }

        _mushrooms.Save(mushrooms);
        _jobs.Save(jobs);
        _events.Append(mushrooms.DequeueUncommittedEvents());
        _events.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}
