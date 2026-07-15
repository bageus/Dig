using System.Linq;
using Dig.Domain.Factions;
using Dig.Domain.Strategy;
using Xunit;

namespace Dig.Tests
{

public sealed class StrategicAiTests
{
    private static readonly FactionId Player = new FactionId("faction.player");
    private static readonly FactionId Raiders = new FactionId("faction.raiders");

    [Fact]
    public void Planner_skips_expensive_recalculation_between_sparse_ticks()
    {
        StrategicAiState strategy = new StrategicAiState(CreatePolicy());
        StrategicDecisionReport first = strategy.Evaluate(Context(
            tick: 10,
            resources: 2,
            housing: 5));
        long version = strategy.Version;

        StrategicDecisionReport skipped = strategy.Evaluate(Context(
            tick: 11,
            resources: 100,
            housing: 100,
            threat: 50_000));

        Assert.False(first.Skipped);
        Assert.Equal(StrategicGoalKind.DevelopResources, first.CurrentGoal);
        Assert.True(skipped.Skipped);
        Assert.Equal(first.CurrentGoal, skipped.CurrentGoal);
        Assert.Equal(version, strategy.Version);
        Assert.Empty(skipped.Candidates);
    }

    [Fact]
    public void Overwhelming_threat_selects_retreat_with_explanation()
    {
        StrategicAiState strategy = new StrategicAiState(CreatePolicy());

        StrategicDecisionReport decision = strategy.Evaluate(Context(
            tick: 10,
            resources: 100,
            housing: 10,
            ownStrength: 5_000,
            threat: 12_000));

        Assert.Equal(StrategicGoalKind.Retreat, decision.CurrentGoal);
        Assert.Equal(
            "detected_threat_overwhelming",
            decision.SelectedCandidate!.Value.ReasonCode);
        Assert.Contains(decision.Candidates, item => item.Kind == StrategicGoalKind.Defend);
    }

    [Fact]
    public void Strong_faction_attacks_weaker_hostile_target()
    {
        StrategicAiState strategy = new StrategicAiState(CreatePolicy());

        StrategicDecisionReport decision = strategy.Evaluate(Context(
            tick: 10,
            resources: 100,
            housing: 10,
            ownStrength: 20_000,
            hostileStrength: 5_000,
            hostileTarget: Raiders));

        Assert.Equal(StrategicGoalKind.Attack, decision.CurrentGoal);
        Assert.Equal("hostile_target_weaker", decision.SelectedCandidate!.Value.ReasonCode);
    }

    [Fact]
    public void Stable_tie_break_prefers_lower_goal_kind()
    {
        StrategicAiPolicy policy = new StrategicAiPolicy(
            planningIntervalTicks: 5,
            minimumResourceReserve: 10,
            minimumFreeHousing: 10,
            attackAdvantageRatio: 1_500,
            retreatThreatRatio: 2_000);
        StrategicAiState strategy = new StrategicAiState(policy);

        StrategicDecisionReport decision = strategy.Evaluate(Context(
            tick: 0,
            resources: 9,
            housing: 9,
            canExpand: false));

        StrategicGoalCandidate[] ordered = decision.Candidates.Take(2).ToArray();
        Assert.Equal(StrategicGoalKind.DevelopResources, ordered[0].Kind);
        Assert.Equal(StrategicGoalKind.DevelopHousing, ordered[1].Kind);
    }

    private static StrategicAiPolicy CreatePolicy()
    {
        return new StrategicAiPolicy(
            planningIntervalTicks: 10,
            minimumResourceReserve: 20,
            minimumFreeHousing: 2,
            attackAdvantageRatio: 1_500,
            retreatThreatRatio: 1_500);
    }

    private static StrategicAiContext Context(
        long tick,
        int resources,
        int housing,
        int ownStrength = 10_000,
        int threat = 0,
        int hostileStrength = 0,
        FactionId? hostileTarget = null,
        bool canExpand = false)
    {
        return new StrategicAiContext(
            tick,
            Player,
            resources,
            housing,
            ownStrength,
            threat,
            hostileStrength,
            hostileTarget,
            canExpand);
    }
}
}
