using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Factions;

namespace Dig.Domain.Strategy
{

public sealed class StrategicGoalChanged : IDomainEvent
{
    public StrategicGoalChanged(
        long tick,
        FactionId factionId,
        StrategicGoalKind previousGoal,
        StrategicGoalKind currentGoal,
        string reasonCode)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException("Strategic goal reason is required.", nameof(reasonCode));
        }

        Tick = tick;
        FactionId = factionId;
        PreviousGoal = previousGoal;
        CurrentGoal = currentGoal;
        ReasonCode = reasonCode.Trim();
    }

    public long Tick { get; }
    public FactionId FactionId { get; }
    public StrategicGoalKind PreviousGoal { get; }
    public StrategicGoalKind CurrentGoal { get; }
    public string ReasonCode { get; }
}

public sealed class StrategicAiState : AggregateRoot
{
    private readonly Dictionary<FactionId, StrategicPlanState> _plans =
        new Dictionary<FactionId, StrategicPlanState>();

    public StrategicAiState(StrategicAiPolicy policy)
    {
        Policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public StrategicAiPolicy Policy { get; }
    public long Version { get; private set; }

    public StrategicDecisionReport Evaluate(StrategicAiContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        StrategicPlanState plan = GetOrCreatePlan(context.FactionId);
        if (context.Tick < plan.NextPlanningTick)
        {
            return new StrategicDecisionReport(
                context.Tick,
                context.FactionId,
                skipped: true,
                plan.Goal,
                plan.Goal,
                plan.NextPlanningTick,
                Array.Empty<StrategicGoalCandidate>());
        }

        StrategicGoalCandidate[] candidates = BuildCandidates(context);
        StrategicGoalCandidate selected = candidates
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Kind)
            .First();
        StrategicGoalKind previous = plan.Goal;
        long nextPlanningTick = checked(context.Tick + Policy.PlanningIntervalTicks);
        plan.Update(selected.Kind, nextPlanningTick);
        Version = checked(Version + 1);
        if (previous != selected.Kind)
        {
            Raise(new StrategicGoalChanged(
                context.Tick,
                context.FactionId,
                previous,
                selected.Kind,
                selected.ReasonCode));
        }

        return new StrategicDecisionReport(
            context.Tick,
            context.FactionId,
            skipped: false,
            previous,
            selected.Kind,
            nextPlanningTick,
            candidates);
    }

    public StrategicPlanSnapshot? GetPlan(FactionId factionId)
    {
        return _plans.TryGetValue(factionId, out StrategicPlanState? plan)
            ? plan.CreateSnapshot(factionId)
            : null;
    }

    public IReadOnlyList<StrategicPlanSnapshot> CreateSnapshot()
    {
        StrategicPlanSnapshot[] plans = _plans
            .OrderBy(item => item.Key)
            .Select(item => item.Value.CreateSnapshot(item.Key))
            .ToArray();
        return new ReadOnlyCollection<StrategicPlanSnapshot>(plans);
    }

    private StrategicPlanState GetOrCreatePlan(FactionId factionId)
    {
        if (_plans.TryGetValue(factionId, out StrategicPlanState? plan))
        {
            return plan;
        }

        StrategicPlanState created = new StrategicPlanState(
            StrategicGoalKind.DevelopResources,
            nextPlanningTick: 0);
        _plans.Add(factionId, created);
        return created;
    }

    private StrategicGoalCandidate[] BuildCandidates(StrategicAiContext context)
    {
        List<StrategicGoalCandidate> candidates = new List<StrategicGoalCandidate>();
        long ownStrength = Math.Max(1, context.OwnStrength);
        bool retreat = context.DetectedThreatStrength * 1_000L
            > ownStrength * Policy.RetreatThreatRatio;
        if (retreat)
        {
            candidates.Add(new StrategicGoalCandidate(
                StrategicGoalKind.Retreat,
                100_000L + context.DetectedThreatStrength,
                "detected_threat_overwhelming"));
        }

        if (context.DetectedThreatStrength > 0)
        {
            candidates.Add(new StrategicGoalCandidate(
                StrategicGoalKind.Defend,
                90_000L + context.DetectedThreatStrength,
                "hostile_threat_detected"));
        }

        if (context.HostileTargetFactionId.HasValue
            && context.HostileTargetStrength > 0
            && context.OwnStrength * 1_000L
                >= context.HostileTargetStrength * Policy.AttackAdvantageRatio)
        {
            candidates.Add(new StrategicGoalCandidate(
                StrategicGoalKind.Attack,
                80_000L + context.OwnStrength - context.HostileTargetStrength,
                "hostile_target_weaker"));
        }

        if (context.ResourceReserve < Policy.MinimumResourceReserve)
        {
            candidates.Add(new StrategicGoalCandidate(
                StrategicGoalKind.DevelopResources,
                70_000L + Policy.MinimumResourceReserve - context.ResourceReserve,
                "resource_reserve_below_target"));
        }

        if (context.FreeHousing < Policy.MinimumFreeHousing)
        {
            candidates.Add(new StrategicGoalCandidate(
                StrategicGoalKind.DevelopHousing,
                60_000L + Policy.MinimumFreeHousing - context.FreeHousing,
                "free_housing_below_target"));
        }

        if (context.CanExpandTerritory)
        {
            candidates.Add(new StrategicGoalCandidate(
                StrategicGoalKind.ExpandTerritory,
                50_000L,
                "safe_frontier_available"));
        }

        if (candidates.Count == 0)
        {
            candidates.Add(new StrategicGoalCandidate(
                StrategicGoalKind.DevelopResources,
                score: 1,
                "maintain_resource_growth"));
        }

        return candidates.ToArray();
    }

    private sealed class StrategicPlanState
    {
        public StrategicPlanState(StrategicGoalKind goal, long nextPlanningTick)
        {
            Goal = goal;
            NextPlanningTick = nextPlanningTick;
        }

        public StrategicGoalKind Goal { get; private set; }
        public long NextPlanningTick { get; private set; }
        public long Version { get; private set; }

        public void Update(StrategicGoalKind goal, long nextPlanningTick)
        {
            Goal = goal;
            NextPlanningTick = nextPlanningTick;
            Version = checked(Version + 1);
        }

        public StrategicPlanSnapshot CreateSnapshot(FactionId factionId)
        {
            return new StrategicPlanSnapshot(
                factionId,
                Goal,
                NextPlanningTick,
                Version);
        }
    }
}
}
