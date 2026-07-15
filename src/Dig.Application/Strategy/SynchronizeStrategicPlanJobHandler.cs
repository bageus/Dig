using System;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Strategy;

namespace Dig.Application.Strategy
{

public sealed class SynchronizeStrategicPlanJobHandler
    : ICommandHandler<SynchronizeStrategicPlanJobCommand, Result>
{
    private readonly IStrategicAiRepository _strategy;
    private readonly IJobRepository _jobs;
    private readonly IEventSink _eventSink;

    public SynchronizeStrategicPlanJobHandler(
        IStrategicAiRepository strategy,
        IJobRepository jobs,
        IEventSink eventSink)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(SynchronizeStrategicPlanJobCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        StrategicAiState strategy = _strategy.Get();
        StrategicExecutionPlanSnapshot? plan = strategy.GetExecutionPlan(command.PlanId);
        JobSnapshot? job = _jobs.Get().Get(command.JobId);
        if (plan is null || plan.JobId != command.JobId || job is null)
        {
            return Result.Failure(StrategyApplicationErrors.PlanJobMismatch);
        }

        Result synchronized;
        if (job.Status == JobStatus.Completed)
        {
            synchronized = strategy.CompleteExecutionPlan(command.PlanId, command.Tick);
        }
        else if (job.Status == JobStatus.Cancelled || job.Status == JobStatus.Failed)
        {
            synchronized = strategy.CancelExecutionPlan(
                command.PlanId,
                "job_" + job.Status.ToString().ToLowerInvariant(),
                command.Tick);
        }
        else
        {
            return Result.Success();
        }

        if (synchronized.IsFailure)
        {
            return synchronized;
        }

        _strategy.Save(strategy);
        _eventSink.Append(strategy.DequeueUncommittedEvents());
        return Result.Success();
    }
}
}
