using System;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.WorldObjects;

namespace Dig.Application.WorldObjects
{

public sealed class StartDirectBarrelAttackCommandHandler
    : ICommandHandler<StartDirectBarrelAttackCommand, Result<BarrelAttackStartedResult>>
{
    private readonly IBarrelRepository _barrels;
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;

    public StartDirectBarrelAttackCommandHandler(
        IBarrelRepository barrels,
        IJobRepository jobs,
        IEventSink events)
    {
        _barrels = barrels ?? throw new ArgumentNullException(nameof(barrels));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result<BarrelAttackStartedResult> Handle(StartDirectBarrelAttackCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BarrelSnapshot? barrel = _barrels.Get().Get(command.BarrelId);
        if (barrel is null)
        {
            return Result<BarrelAttackStartedResult>.Failure(BarrelErrors.NotFound);
        }

        if (!barrel.IsAttackable)
        {
            return Result<BarrelAttackStartedResult>.Failure(BarrelErrors.NotAttackable);
        }

        JobSystem jobs = _jobs.Get();
        BarrelAttackJobDefinition definition = new BarrelAttackJobDefinition(
            command.JobId,
            command.BarrelId,
            barrel.Cell,
            command.WorkPosition,
            barrel.Version,
            barrel.ContentsGeneration,
            command.Priority,
            command.Tick,
            JobRetryPolicy.Default);
        Result added = jobs.Add(definition);
        if (added.IsFailure)
        {
            return Result<BarrelAttackStartedResult>.Failure(added.Error!);
        }

        EnsureCommitStep(jobs.MakeAvailable(command.JobId, command.Tick));
        EnsureCommitStep(jobs.Claim(command.JobId, command.WorkerId, command.Tick));
        EnsureCommitStep(jobs.Start(command.JobId, command.Tick));
        _jobs.Save(jobs);
        _events.Append(jobs.DequeueUncommittedEvents());
        return Result<BarrelAttackStartedResult>.Success(new BarrelAttackStartedResult(
            command.JobId,
            command.BarrelId,
            command.WorkerId,
            barrel.Version,
            barrel.ContentsGeneration));
    }

    private static void EnsureCommitStep(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Validated barrel attack start failed: {result.Error}");
        }
    }
}

public sealed class ArriveAtBarrelCommandHandler
    : ICommandHandler<ArriveAtBarrelCommand, Result>
{
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;

    public ArriveAtBarrelCommandHandler(IJobRepository jobs, IEventSink events)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result Handle(ArriveAtBarrelCommand command)
    {
        JobSystem jobs = _jobs.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not BarrelAttackJobDefinition
            || job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.TravelToTarget)
        {
            return Result.Failure(job is null ? JobErrors.NotFound : BarrelApplicationErrors.JobNotReady);
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

public sealed class CompleteBarrelHitCommandHandler
    : ICommandHandler<CompleteBarrelHitCommand, Result>
{
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;

    public CompleteBarrelHitCommandHandler(IJobRepository jobs, IEventSink events)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result Handle(CompleteBarrelHitCommand command)
    {
        JobSystem jobs = _jobs.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not BarrelAttackJobDefinition)
        {
            return Result.Failure(job is null ? JobErrors.NotFound : BarrelApplicationErrors.JobTypeUnsupported);
        }

        if (job.Status != JobStatus.InProgress || job.Stage != JobStageKind.PerformWork)
        {
            return Result.Failure(BarrelApplicationErrors.JobNotReady);
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

public sealed class CompleteBarrelDestructionCommandHandler
    : ICommandHandler<CompleteBarrelDestructionCommand, Result<BarrelDestructionResult>>
{
    private readonly IBarrelRepository _barrels;
    private readonly IJobRepository _jobs;
    private readonly IInventoryRepository _inventory;
    private readonly IEventSink _events;

    public CompleteBarrelDestructionCommandHandler(
        IBarrelRepository barrels,
        IJobRepository jobs,
        IInventoryRepository inventory,
        IEventSink events)
    {
        _barrels = barrels ?? throw new ArgumentNullException(nameof(barrels));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result<BarrelDestructionResult> Handle(CompleteBarrelDestructionCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        JobSystem jobs = _jobs.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not BarrelAttackJobDefinition definition)
        {
            return Result<BarrelDestructionResult>.Failure(job is null
                ? JobErrors.NotFound
                : BarrelApplicationErrors.JobTypeUnsupported);
        }

        if (job.Status != JobStatus.InProgress || job.Stage != JobStageKind.Finalize)
        {
            return Result<BarrelDestructionResult>.Failure(BarrelApplicationErrors.JobNotReady);
        }

        EntityId workerId = job.AssignedAgentId
            ?? throw new InvalidOperationException("An in-progress barrel attack must retain its worker.");
        BarrelState barrels = _barrels.Get();
        BarrelSnapshot? barrel = barrels.Get(definition.BarrelId);
        if (barrel is null)
        {
            return Result<BarrelDestructionResult>.Failure(BarrelErrors.NotFound);
        }

        if (!barrel.IsAttackable
            || barrel.Version != definition.BarrelVersion
            || barrel.ContentsGeneration != definition.ContentsGeneration)
        {
            return Result<BarrelDestructionResult>.Failure(BarrelApplicationErrors.GenerationConflict);
        }

        InventoryState inventory = _inventory.Get();
        if (!inventory.Catalog.Contains(barrel.ContentsItemId))
        {
            return Result<BarrelDestructionResult>.Failure(BarrelApplicationErrors.UnknownContentsItem);
        }

        if (inventory.GetStack(command.OutputUnitId) is not null)
        {
            return Result<BarrelDestructionResult>.Failure(InventoryErrors.StackAlreadyExists);
        }

        Result<BarrelDestructionCommit> committed = barrels.Destroy(
            definition.BarrelId,
            definition.BarrelVersion,
            command.JobId,
            workerId,
            command.Tick);
        EnsureCommitStep(committed.IsSuccess, committed.Error);
        BarrelDestructionCommit destruction = committed.Value;
        EnsureCommitStep(inventory.AddUnit(
            command.OutputUnitId,
            destruction.ContentsItemId,
            ItemLocation.InWorld(destruction.Cell),
            command.Tick));
        EnsureCommitStep(jobs.AdvanceStage(command.JobId, command.Tick));
        barrels.RecordContentsMaterialized(destruction, command.OutputUnitId, command.Tick);
        _barrels.Save(barrels);
        _inventory.Save(inventory);
        _jobs.Save(jobs);
        _events.Append(barrels.DequeueUncommittedEvents());
        _events.Append(inventory.DequeueUncommittedEvents());
        _events.Append(jobs.DequeueUncommittedEvents());
        return Result<BarrelDestructionResult>.Success(new BarrelDestructionResult(
            command.JobId,
            definition.BarrelId,
            command.OutputUnitId,
            destruction.ContentsGeneration));
    }

    private static void EnsureCommitStep(bool succeeded, DomainError? error)
    {
        if (!succeeded)
        {
            throw new InvalidOperationException($"Validated barrel destruction failed: {error}");
        }
    }

    private static void EnsureCommitStep(Result result) =>
        EnsureCommitStep(result.IsSuccess, result.Error);
}

public sealed class CancelBarrelAttackCommandHandler
    : ICommandHandler<CancelBarrelAttackCommand, Result>
{
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;

    public CancelBarrelAttackCommandHandler(IJobRepository jobs, IEventSink events)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result Handle(CancelBarrelAttackCommand command)
    {
        JobSystem jobs = _jobs.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not BarrelAttackJobDefinition)
        {
            return Result.Failure(job is null ? JobErrors.NotFound : BarrelApplicationErrors.JobTypeUnsupported);
        }

        if (job.IsTerminal)
        {
            return Result.Failure(BarrelApplicationErrors.JobNotReady);
        }

        Result cancelled = jobs.Cancel(
            command.JobId,
            new JobBlockReason(command.ReasonCode, "Barrel attack was interrupted."),
            command.Tick);
        if (cancelled.IsSuccess)
        {
            _jobs.Save(jobs);
            _events.Append(jobs.DequeueUncommittedEvents());
        }

        return cancelled;
    }
}

public sealed class SettleBarrelAfterSupportLossCommandHandler
    : ICommandHandler<SettleBarrelAfterSupportLossCommand, Result>
{
    private readonly IBarrelRepository _barrels;
    private readonly IEventSink _events;

    public SettleBarrelAfterSupportLossCommandHandler(
        IBarrelRepository barrels,
        IEventSink events)
    {
        _barrels = barrels ?? throw new ArgumentNullException(nameof(barrels));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result Handle(SettleBarrelAfterSupportLossCommand command)
    {
        BarrelState barrels = _barrels.Get();
        Result falling = barrels.BeginFall(command.BarrelId, command.LandingCell, command.Tick);
        if (falling.IsFailure)
        {
            return falling;
        }

        Result landed = barrels.Land(command.BarrelId, command.Tick);
        if (landed.IsFailure)
        {
            throw new InvalidOperationException($"Validated barrel landing failed: {landed.Error}");
        }

        _barrels.Save(barrels);
        _events.Append(barrels.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}