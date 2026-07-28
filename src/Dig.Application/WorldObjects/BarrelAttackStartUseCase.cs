using System;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
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
        ReservationSnapshot? workerReservation = jobs.GetReservations()
            .FirstOrDefault(reservation =>
                reservation.Key == ReservationKey.ForAgent(command.WorkerId));
        if (workerReservation != null)
        {
            return Result<BarrelAttackStartedResult>.Failure(JobErrors.AgentUnavailable);
        }

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

        Result available = jobs.MakeAvailable(command.JobId, command.Tick);
        if (available.IsFailure)
        {
            return RejectNewAttack(jobs, command.JobId, available.Error!, command.Tick);
        }

        Result canClaim = jobs.CanClaim(
            command.JobId,
            command.WorkerId,
            toolStackId: null,
            command.Tick);
        if (canClaim.IsFailure)
        {
            return RejectNewAttack(jobs, command.JobId, canClaim.Error!, command.Tick);
        }

        Result claimed = jobs.Claim(command.JobId, command.WorkerId, command.Tick);
        if (claimed.IsFailure)
        {
            return RejectNewAttack(jobs, command.JobId, claimed.Error!, command.Tick);
        }

        Result started = jobs.Start(command.JobId, command.Tick);
        if (started.IsFailure)
        {
            return RejectNewAttack(jobs, command.JobId, started.Error!, command.Tick);
        }

        SaveAndPublish(jobs);
        return Result<BarrelAttackStartedResult>.Success(new BarrelAttackStartedResult(
            command.JobId,
            command.BarrelId,
            command.WorkerId,
            barrel.Version,
            barrel.ContentsGeneration));
    }

    private Result<BarrelAttackStartedResult> RejectNewAttack(
        JobSystem jobs,
        EntityId jobId,
        DomainError error,
        long tick)
    {
        JobSnapshot? job = jobs.Get(jobId);
        if (job != null && !job.IsTerminal)
        {
            Result cancelled = jobs.Cancel(
                jobId,
                new JobBlockReason("barrel_start_rejected", error.Message),
                tick);
            if (cancelled.IsFailure)
            {
                error = cancelled.Error!;
            }
        }

        SaveAndPublish(jobs);
        return Result<BarrelAttackStartedResult>.Failure(error);
    }

    private void SaveAndPublish(JobSystem jobs)
    {
        _jobs.Save(jobs);
        _events.Append(jobs.DequeueUncommittedEvents());
    }
}

}
