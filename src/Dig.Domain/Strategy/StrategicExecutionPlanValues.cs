using System;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.World;

namespace Dig.Domain.Strategy
{

public readonly struct StrategicExecutionPlanId
    : IEquatable<StrategicExecutionPlanId>, IComparable<StrategicExecutionPlanId>
{
    private readonly string? _value;

    public StrategicExecutionPlanId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Strategic plan id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public int CompareTo(StrategicExecutionPlanId other)
    {
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public bool Equals(StrategicExecutionPlanId other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is StrategicExecutionPlanId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);
    }

    public override string ToString()
    {
        return _value ?? string.Empty;
    }

    public static bool operator ==(
        StrategicExecutionPlanId left,
        StrategicExecutionPlanId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        StrategicExecutionPlanId left,
        StrategicExecutionPlanId right)
    {
        return !left.Equals(right);
    }
}

public enum StrategicExecutionPlanStatus
{
    Proposed = 0,
    Materialized = 1,
    Completed = 2,
    Cancelled = 3,
}

public sealed class StrategicExecutionPlanRequest
{
    public StrategicExecutionPlanRequest(
        StrategicExecutionPlanId planId,
        FactionId factionId,
        StrategicGoalKind goal,
        string reasonCode,
        long createdTick,
        CellId? targetCell = null,
        FactionId? targetFactionId = null)
    {
        if (planId.IsEmpty)
        {
            throw new ArgumentException("Strategic plan id cannot be empty.", nameof(planId));
        }

        if (factionId.IsEmpty)
        {
            throw new ArgumentException("Faction id cannot be empty.", nameof(factionId));
        }

        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException("Strategic plan reason is required.", nameof(reasonCode));
        }

        if (createdTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(createdTick));
        }

        ValidateTargets(goal, targetCell, targetFactionId);
        PlanId = planId;
        FactionId = factionId;
        Goal = goal;
        ReasonCode = reasonCode.Trim();
        CreatedTick = createdTick;
        TargetCell = targetCell;
        TargetFactionId = targetFactionId;
    }

    public StrategicExecutionPlanId PlanId { get; }
    public FactionId FactionId { get; }
    public StrategicGoalKind Goal { get; }
    public string ReasonCode { get; }
    public long CreatedTick { get; }
    public CellId? TargetCell { get; }
    public FactionId? TargetFactionId { get; }

    private static void ValidateTargets(
        StrategicGoalKind goal,
        CellId? targetCell,
        FactionId? targetFactionId)
    {
        if ((goal == StrategicGoalKind.DevelopResources
            || goal == StrategicGoalKind.DevelopHousing
            || goal == StrategicGoalKind.ExpandTerritory
            || goal == StrategicGoalKind.Defend
            || goal == StrategicGoalKind.Retreat)
            && !targetCell.HasValue)
        {
            throw new ArgumentException("This strategic goal requires a target cell.");
        }

        if (goal == StrategicGoalKind.Attack && !targetFactionId.HasValue)
        {
            throw new ArgumentException("Attack plans require a target faction.");
        }
    }
}

public sealed class StrategicExecutionPlanSnapshot
{
    public StrategicExecutionPlanSnapshot(
        StrategicExecutionPlanId planId,
        FactionId factionId,
        StrategicGoalKind goal,
        StrategicExecutionPlanStatus status,
        string reasonCode,
        long createdTick,
        long? finishedTick,
        CellId? targetCell,
        FactionId? targetFactionId,
        EntityId? jobId,
        string? finishReason)
    {
        PlanId = planId;
        FactionId = factionId;
        Goal = goal;
        Status = status;
        ReasonCode = reasonCode;
        CreatedTick = createdTick;
        FinishedTick = finishedTick;
        TargetCell = targetCell;
        TargetFactionId = targetFactionId;
        JobId = jobId;
        FinishReason = finishReason;
    }

    public StrategicExecutionPlanId PlanId { get; }
    public FactionId FactionId { get; }
    public StrategicGoalKind Goal { get; }
    public StrategicExecutionPlanStatus Status { get; }
    public string ReasonCode { get; }
    public long CreatedTick { get; }
    public long? FinishedTick { get; }
    public CellId? TargetCell { get; }
    public FactionId? TargetFactionId { get; }
    public EntityId? JobId { get; }
    public string? FinishReason { get; }
    public bool IsTerminal => Status == StrategicExecutionPlanStatus.Completed
        || Status == StrategicExecutionPlanStatus.Cancelled;
}

public sealed class StrategicExecutionPlanChanged : IDomainEvent
{
    public StrategicExecutionPlanChanged(
        long tick,
        StrategicExecutionPlanSnapshot? previous,
        StrategicExecutionPlanSnapshot current)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        Tick = tick;
        Previous = previous;
        Current = current ?? throw new ArgumentNullException(nameof(current));
    }

    public long Tick { get; }
    public StrategicExecutionPlanSnapshot? Previous { get; }
    public StrategicExecutionPlanSnapshot Current { get; }
}
}
