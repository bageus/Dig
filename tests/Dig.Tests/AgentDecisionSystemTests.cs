using Dig.Domain.Agents;
using Xunit;

namespace Dig.Tests;

public sealed class AgentDecisionSystemTests
{
    private readonly AgentBehaviorPolicy _policy = AgentBehaviorPolicy.CreateDefault();
    private readonly AgentDecisionSystem _decisionSystem = new AgentDecisionSystem();

    [Fact]
    public void Critical_hunger_overrides_maximum_player_order()
    {
        AgentState agent = AgentTestFactory.CreateAgent(
            nutrition: 1_000,
            alertness: 8_000,
            mood: 8_000);
        PlayerOrder order = new PlayerOrder(
            "order-build",
            "Build a workshop",
            priority: 10_000,
            issuedTick: 0,
            expiresTick: 20);
        Assert.True(agent.SetPlayerOrder(order, tick: 0).IsSuccess);

        AgentDecision decision = Decide(agent, tick: 0);

        Assert.Equal(AgentIntentKind.Eat, decision.SelectedIntent);
        Assert.True(decision.Critical);
        Assert.Equal("selected.critical_survival", decision.ReasonCode);
        UtilityOptionDiagnostic playerOption = Assert.Single(
            decision.Options,
            option => option.IntentKind == AgentIntentKind.PlayerOrder);
        Assert.Equal("rejected.lower_utility", playerOption.ReasonCode);
    }

    [Fact]
    public void Player_order_overrides_ordinary_work()
    {
        AgentState agent = AgentTestFactory.CreateAgent(
            nutrition: 9_000,
            alertness: 9_000,
            mood: 9_000);
        PlayerOrder order = new PlayerOrder(
            "order-haul",
            "Move selected crate",
            priority: 8_000,
            issuedTick: 0,
            expiresTick: 20);
        Assert.True(agent.SetPlayerOrder(order, tick: 0).IsSuccess);

        AgentDecision decision = Decide(agent, tick: 0);

        Assert.Equal(AgentIntentKind.PlayerOrder, decision.SelectedIntent);
        Assert.Equal(order.Id, decision.SelectedPlayerOrderId);
        Assert.Equal("selected.player_override", decision.ReasonCode);
    }

    [Fact]
    public void Critical_sleep_interrupts_work_during_cooldown()
    {
        AgentState agent = AgentTestFactory.CreateAgent(
            nutrition: 8_000,
            alertness: 1_000,
            mood: 8_000);
        Assert.True(agent.ApplyDecision(
            AgentTestFactory.CreateForcedDecision(AgentIntentKind.Work, tick: 0),
            _policy,
            tick: 0).IsSuccess);

        AgentDecision decision = Decide(agent, tick: 1);

        Assert.Equal(AgentIntentKind.Sleep, decision.SelectedIntent);
        Assert.True(decision.Critical);
    }

    [Fact]
    public void Cooldown_blocks_noncritical_oscillation_then_expires()
    {
        AgentState agent = AgentTestFactory.CreateAgent(
            nutrition: 8_000,
            alertness: 8_000,
            mood: 1_000);
        Assert.True(agent.ApplyDecision(
            AgentTestFactory.CreateForcedDecision(AgentIntentKind.Work, tick: 0),
            _policy,
            tick: 0).IsSuccess);

        AgentDecision duringCooldown = Decide(agent, tick: 1);
        AgentDecision afterCooldown = Decide(agent, tick: 2);

        Assert.Equal(AgentIntentKind.Work, duringCooldown.SelectedIntent);
        UtilityOptionDiagnostic blockedRest = Assert.Single(
            duringCooldown.Options,
            option => option.IntentKind == AgentIntentKind.Rest);
        Assert.Equal("rejected.cooldown", blockedRest.ReasonCode);
        Assert.Equal(AgentIntentKind.Rest, afterCooldown.SelectedIntent);
    }

    [Fact]
    public void Sleep_schedule_changes_noncritical_choice()
    {
        AgentState agent = AgentTestFactory.CreateAgent(
            nutrition: 8_000,
            alertness: 5_500,
            mood: 8_000,
            schedule: AgentTestFactory.CreateSleepSchedule());

        AgentDecision decision = Decide(agent, tick: 0);

        Assert.Equal(AgentIntentKind.Sleep, decision.SelectedIntent);
        Assert.False(decision.Critical);
    }

    [Fact]
    public void Decision_explains_selected_and_rejected_alternatives()
    {
        AgentState agent = AgentTestFactory.CreateAgent();

        AgentDecision decision = Decide(agent, tick: 0);

        Assert.Equal(7, decision.Options.Count);
        Assert.Single(decision.Options, option => option.Selected);
        Assert.All(
            decision.Options,
            option => Assert.False(string.IsNullOrWhiteSpace(option.ReasonCode)));
        Assert.Contains(
            decision.Options,
            option => !option.Selected && option.ReasonCode.StartsWith(
                "rejected.",
                StringComparison.Ordinal));
    }

    private AgentDecision Decide(AgentState agent, long tick)
    {
        return _decisionSystem.Decide(
            agent.CreateSnapshot(tick),
            AgentDecisionContext.AllAvailable(),
            _policy,
            tick);
    }
}
