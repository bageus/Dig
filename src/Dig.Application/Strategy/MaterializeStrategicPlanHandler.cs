using System;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Strategy;

namespace Dig.Application.Strategy
{

public sealed class MaterializeStrategicPlanHandler
    : ICommandHandler<MaterializeStrategicPlanCommand, Result>
{
    private readonly IStrategicAiRepository _strategy;
    private readonly IJobRepository _jobs;
    private readonly IEventSink _eventSink;

    public MaterializeStrategicPlanHandler(
        IStrategicAiRepository strategy,
        IJobRepository jobs,
        IEventSink eventSink)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(MaterializeStrategicPlanCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        StrategicAiState strategy = _strategy.Get();
        StrategicExecutionPlanSnapshot? plan = strategy.GetExecutionPlan(command.PlanId);
        if (plan is null || plan.Status != StrategicExecutionPlanStatus.Proposed)
        {
            return Result.Failure(StrategyApplicationErrors.PlanNotProposed);
        }

        JobSystem jobs = _jobs.Get();
        if (jobs.Get(command.JobId) is not null)
        {
            return Result.Failure(StrategyApplicationErrors.JobAlreadyExists);
        }

        StrategicExecutionJobDefinition definition = new StrategicExecutionJobDefinition(
            command.JobId,
            plan.PlanId,
            plan.FactionId,
            plan.Goal,
            plan.TargetCell,
            plan.TargetFactionId,
            command.Priority,
            command.Tick,
            JobRetryPolicy.Default);
        Result added = jobs.Add(definition);
        if (added.IsFailure)
        {
            return added;
        }

        Result available = jobs.MakeAvailable(command.JobId, command.Tick);
        if (available.IsFailure)
        {
            return available;
        }

        Result materialized = strategy.MarkExecutionPlanMaterialized(
            command.PlanId,
            command.JobId,
            command.Tick);
        if (materialized.IsFailure)
        {
            return materialized;
        }

        _jobs.Save(jobs);
        _strategy.Save(strategy);
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        _eventSink.Append(strategy.DequeueUncommittedEvents());
        return Result.Success();
    }
}
}
