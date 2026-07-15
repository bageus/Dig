using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Factions;

namespace Dig.Domain.Strategy
{

public enum StrategicGoalKind
{
    DevelopResources = 0,
    DevelopHousing = 1,
    ExpandTerritory = 2,
    Defend = 3,
    Attack = 4,
    Retreat = 5,
}

public sealed class StrategicAiPolicy
{
    public StrategicAiPolicy(
        long planningIntervalTicks,
        int minimumResourceReserve,
        int minimumFreeHousing,
        int attackAdvantageRatio,
        int retreatThreatRatio)
    {
        if (planningIntervalTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(planningIntervalTicks));
        }

        if (minimumResourceReserve < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumResourceReserve));
        }

        if (minimumFreeHousing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumFreeHousing));
        }

        if (attackAdvantageRatio < 1_000)
        {
            throw new ArgumentOutOfRangeException(nameof(attackAdvantageRatio));
        }

        if (retreatThreatRatio < 1_000)
        {
            throw new ArgumentOutOfRangeException(nameof(retreatThreatRatio));
        }

        PlanningIntervalTicks = planningIntervalTicks;
        MinimumResourceReserve = minimumResourceReserve;
        MinimumFreeHousing = minimumFreeHousing;
        AttackAdvantageRatio = attackAdvantageRatio;
        RetreatThreatRatio = retreatThreatRatio;
    }

    public long PlanningIntervalTicks { get; }
    public int MinimumResourceReserve { get; }
    public int MinimumFreeHousing { get; }
    public int AttackAdvantageRatio { get; }
    public int RetreatThreatRatio { get; }
}

public sealed class StrategicAiContext
{
    public StrategicAiContext(
        long tick,
        FactionId factionId,
        int resourceReserve,
        int freeHousing,
        int ownStrength,
        int detectedThreatStrength,
        int hostileTargetStrength,
        FactionId? hostileTargetFactionId,
        bool canExpandTerritory)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (factionId.IsEmpty)
        {
            throw new ArgumentException("Faction id cannot be empty.", nameof(factionId));
        }

        ValidateNonNegative(resourceReserve, nameof(resourceReserve));
        ValidateNonNegative(freeHousing, nameof(freeHousing));
        ValidateNonNegative(ownStrength, nameof(ownStrength));
        ValidateNonNegative(detectedThreatStrength, nameof(detectedThreatStrength));
        ValidateNonNegative(hostileTargetStrength, nameof(hostileTargetStrength));

        Tick = tick;
        FactionId = factionId;
        ResourceReserve = resourceReserve;
        FreeHousing = freeHousing;
        OwnStrength = ownStrength;
        DetectedThreatStrength = detectedThreatStrength;
        HostileTargetStrength = hostileTargetStrength;
        HostileTargetFactionId = hostileTargetFactionId;
        CanExpandTerritory = canExpandTerritory;
    }

    public long Tick { get; }
    public FactionId FactionId { get; }
    public int ResourceReserve { get; }
    public int FreeHousing { get; }
    public int OwnStrength { get; }
    public int DetectedThreatStrength { get; }
    public int HostileTargetStrength { get; }
    public FactionId? HostileTargetFactionId { get; }
    public bool CanExpandTerritory { get; }

    private static void ValidateNonNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

public readonly struct StrategicGoalCandidate
{
    public StrategicGoalCandidate(StrategicGoalKind kind, long score, string reasonCode)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException("Strategic candidate reason is required.", nameof(reasonCode));
        }

        Kind = kind;
        Score = score;
        ReasonCode = reasonCode.Trim();
    }

    public StrategicGoalKind Kind { get; }
    public long Score { get; }
    public string ReasonCode { get; }
}

public sealed class StrategicDecisionReport
{
    public StrategicDecisionReport(
        long tick,
        FactionId factionId,
        bool skipped,
        StrategicGoalKind previousGoal,
        StrategicGoalKind currentGoal,
        long nextPlanningTick,
        IEnumerable<StrategicGoalCandidate> candidates)
    {
        if (candidates is null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        Tick = tick;
        FactionId = factionId;
        Skipped = skipped;
        PreviousGoal = previousGoal;
        CurrentGoal = currentGoal;
        NextPlanningTick = nextPlanningTick;
        Candidates = new ReadOnlyCollection<StrategicGoalCandidate>(
            candidates
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Kind)
                .ToArray());
    }

    public long Tick { get; }
    public FactionId FactionId { get; }
    public bool Skipped { get; }
    public StrategicGoalKind PreviousGoal { get; }
    public StrategicGoalKind CurrentGoal { get; }
    public long NextPlanningTick { get; }
    public IReadOnlyList<StrategicGoalCandidate> Candidates { get; }
    public StrategicGoalCandidate? SelectedCandidate =>
        Candidates.Count == 0 ? null : Candidates[0];
}

public readonly struct StrategicPlanSnapshot
{
    public StrategicPlanSnapshot(
        FactionId factionId,
        StrategicGoalKind goal,
        long nextPlanningTick,
        long version)
    {
        FactionId = factionId;
        Goal = goal;
        NextPlanningTick = nextPlanningTick;
        Version = version;
    }

    public FactionId FactionId { get; }
    public StrategicGoalKind Goal { get; }
    public long NextPlanningTick { get; }
    public long Version { get; }
}
}
