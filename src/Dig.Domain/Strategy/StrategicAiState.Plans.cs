using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Factions;

namespace Dig.Domain.Strategy
{

public sealed partial class StrategicAiState
{
    private readonly Dictionary<StrategicExecutionPlanId, StrategicExecutionPlanRecord>
        _executionPlans = new Dictionary<StrategicExecutionPlanId, StrategicExecutionPlanRecord>();
    private readonly Dictionary<FactionId, StrategicExecutionPlanId> _activeExecutionPlans =
        new Dictionary<FactionId, StrategicExecutionPlanId>();

    public Result<StrategicExecutionPlanSnapshot> CreateExecutionPlan(
        StrategicExecutionPlanRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (_executionPlans.TryGetValue(
            request.PlanId,
            out StrategicExecutionPlanRecord? existing))
        {
            return Result<StrategicExecutionPlanSnapshot>.Success(existing.CreateSnapshot());
        }

        StrategicPlanSnapshot? currentGoal = GetPlan(request.FactionId);
        if (!currentGoal.HasValue || currentGoal.Value.Goal != request.Goal)
        {
            return Result<StrategicExecutionPlanSnapshot>.Failure(new DomainError(
                "strategy.plan.goal_mismatch",
                "The execution plan does not match the faction's current strategic goal."));
        }

        StrategicExecutionPlanSnapshot? previous = null;
        if (_activeExecutionPlans.TryGetValue(
            request.FactionId,
            out StrategicExecutionPlanId activeId))
        {
            StrategicExecutionPlanRecord active = _executionPlans[activeId];
            active.Cancel(request.CreatedTick, "replaced_by_new_plan");
            previous = active.CreateSnapshot();
        }

        StrategicExecutionPlanRecord created = new StrategicExecutionPlanRecord(request);
        _executionPlans.Add(request.PlanId, created);
        _activeExecutionPlans[request.FactionId] = request.PlanId;
        Version = checked(Version + 1);
        StrategicExecutionPlanSnapshot snapshot = created.CreateSnapshot();
        Raise(new StrategicExecutionPlanChanged(
            request.CreatedTick,
            previous,
            snapshot));
        return Result<StrategicExecutionPlanSnapshot>.Success(snapshot);
    }

    public Result MarkExecutionPlanMaterialized(
        StrategicExecutionPlanId planId,
        EntityId jobId,
        long tick)
    {
        ValidatePlanTick(tick);
        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
        }

        if (!_executionPlans.TryGetValue(
            planId,
            out StrategicExecutionPlanRecord? plan))
        {
            return UnknownExecutionPlan();
        }

        if (plan.Status == StrategicExecutionPlanStatus.Materialized
            && plan.JobId == jobId)
        {
            return Result.Success();
        }

        if (plan.Status != StrategicExecutionPlanStatus.Proposed)
        {
            return InvalidExecutionPlanTransition();
        }

        plan.Materialize(jobId);
        Version = checked(Version + 1);
        Raise(new StrategicExecutionPlanChanged(tick, null, plan.CreateSnapshot()));
        return Result.Success();
    }

    public Result CompleteExecutionPlan(StrategicExecutionPlanId planId, long tick)
    {
        return FinishExecutionPlan(
            planId,
            StrategicExecutionPlanStatus.Completed,
            tick,
            "completed");
    }

    public Result CancelExecutionPlan(
        StrategicExecutionPlanId planId,
        string reasonCode,
        long tick)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException("Plan cancellation reason is required.", nameof(reasonCode));
        }

        return FinishExecutionPlan(
            planId,
            StrategicExecutionPlanStatus.Cancelled,
            tick,
            reasonCode.Trim());
    }

    public StrategicExecutionPlanSnapshot? GetExecutionPlan(
        StrategicExecutionPlanId planId)
    {
        return _executionPlans.TryGetValue(
            planId,
            out StrategicExecutionPlanRecord? plan)
            ? plan.CreateSnapshot()
            : null;
    }

    public StrategicExecutionPlanSnapshot? GetActiveExecutionPlan(FactionId factionId)
    {
        return _activeExecutionPlans.TryGetValue(
            factionId,
            out StrategicExecutionPlanId planId)
            ? _executionPlans[planId].CreateSnapshot()
            : null;
    }

    public IReadOnlyList<StrategicExecutionPlanSnapshot> CreateExecutionPlanSnapshot()
    {
        StrategicExecutionPlanSnapshot[] plans = _executionPlans.Values
            .OrderBy(plan => plan.PlanId)
            .Select(plan => plan.CreateSnapshot())
            .ToArray();
        return new ReadOnlyCollection<StrategicExecutionPlanSnapshot>(plans);
    }

    private Result FinishExecutionPlan(
        StrategicExecutionPlanId planId,
        StrategicExecutionPlanStatus status,
        long tick,
        string reasonCode)
    {
        ValidatePlanTick(tick);
        if (!_executionPlans.TryGetValue(
            planId,
            out StrategicExecutionPlanRecord? plan))
        {
            return UnknownExecutionPlan();
        }

        if (plan.IsTerminal)
        {
            return Result.Success();
        }

        if (status == StrategicExecutionPlanStatus.Completed
            && plan.Status != StrategicExecutionPlanStatus.Materialized)
        {
            return InvalidExecutionPlanTransition();
        }

        StrategicExecutionPlanSnapshot previous = plan.CreateSnapshot();
        if (status == StrategicExecutionPlanStatus.Completed)
        {
            plan.Complete(tick);
        }
        else
        {
            plan.Cancel(tick, reasonCode);
        }

        _activeExecutionPlans.Remove(plan.FactionId);
        Version = checked(Version + 1);
        Raise(new StrategicExecutionPlanChanged(
            tick,
            previous,
            plan.CreateSnapshot()));
        return Result.Success();
    }

    private static Result UnknownExecutionPlan()
    {
        return Result.Failure(new DomainError(
            "strategy.plan.unknown",
            "The strategic execution plan is not registered."));
    }

    private static Result InvalidExecutionPlanTransition()
    {
        return Result.Failure(new DomainError(
            "strategy.plan.invalid_transition",
            "The strategic execution plan cannot make the requested transition."));
    }

    private static void ValidatePlanTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }

    private sealed class StrategicExecutionPlanRecord
    {
        public StrategicExecutionPlanRecord(StrategicExecutionPlanRequest request)
        {
            PlanId = request.PlanId;
            FactionId = request.FactionId;
            Goal = request.Goal;
            ReasonCode = request.ReasonCode;
            CreatedTick = request.CreatedTick;
            TargetCell = request.TargetCell;
            TargetFactionId = request.TargetFactionId;
        }

        public StrategicExecutionPlanId PlanId { get; }
        public FactionId FactionId { get; }
        public StrategicGoalKind Goal { get; }
        public string ReasonCode { get; }
        public long CreatedTick { get; }
        public Dig.Domain.World.CellId? TargetCell { get; }
        public FactionId? TargetFactionId { get; }
        public StrategicExecutionPlanStatus Status { get; private set; }
        public long? FinishedTick { get; private set; }
        public EntityId? JobId { get; private set; }
        public string? FinishReason { get; private set; }
        public bool IsTerminal => Status == StrategicExecutionPlanStatus.Completed
            || Status == StrategicExecutionPlanStatus.Cancelled;

        public void Materialize(EntityId jobId)
        {
            Status = StrategicExecutionPlanStatus.Materialized;
            JobId = jobId;
        }

        public void Complete(long tick)
        {
            Status = StrategicExecutionPlanStatus.Completed;
            FinishedTick = tick;
            FinishReason = "completed";
        }

        public void Cancel(long tick, string reasonCode)
        {
            if (IsTerminal)
            {
                return;
            }

            Status = StrategicExecutionPlanStatus.Cancelled;
            FinishedTick = tick;
            FinishReason = reasonCode;
        }

        public StrategicExecutionPlanSnapshot CreateSnapshot()
        {
            return new StrategicExecutionPlanSnapshot(
                PlanId,
                FactionId,
                Goal,
                Status,
                ReasonCode,
                CreatedTick,
                FinishedTick,
                TargetCell,
                TargetFactionId,
                JobId,
                FinishReason);
        }
    }
}
}
