using System;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Strategy;

namespace Dig.Application.Strategy
{

public interface IStrategicAiRepository
{
    StrategicAiState Get();

    void Save(StrategicAiState strategy);
}

public static class StrategyApplicationErrors
{
    public static readonly DomainError PlanNotProposed = new DomainError(
        "strategy.application.plan_not_proposed",
        "The strategic plan is missing or is not awaiting materialization.");

    public static readonly DomainError JobAlreadyExists = new DomainError(
        "strategy.application.job_exists",
        "The requested strategic job id is already registered.");

    public static readonly DomainError PlanJobMismatch = new DomainError(
        "strategy.application.plan_job_mismatch",
        "The strategic plan is not linked to the requested job.");
}

public sealed class MaterializeStrategicPlanCommand : ICommand<Result>
{
    public MaterializeStrategicPlanCommand(
        StrategicExecutionPlanId planId,
        EntityId jobId,
        int priority,
        long tick)
    {
        PlanId = planId;
        JobId = jobId;
        Priority = priority;
        Tick = tick;
    }

    public StrategicExecutionPlanId PlanId { get; }
    public EntityId JobId { get; }
    public int Priority { get; }
    public long Tick { get; }
}

public sealed class SynchronizeStrategicPlanJobCommand : ICommand<Result>
{
    public SynchronizeStrategicPlanJobCommand(
        StrategicExecutionPlanId planId,
        EntityId jobId,
        long tick)
    {
        PlanId = planId;
        JobId = jobId;
        Tick = tick;
    }

    public StrategicExecutionPlanId PlanId { get; }
    public EntityId JobId { get; }
    public long Tick { get; }
}
}
