using System.Linq;
using Dig.Domain.Agents;
using Xunit;

namespace Dig.Tests
{

public sealed class AgentInventoryTravelCostTests
{
    [Fact]
    public void Loaded_travel_cost_reduces_movement_intent_scores_only()
    {
        AgentState agent = AgentTestFactory.CreateAgent(
            nutrition: 8_000,
            alertness: 8_000,
            mood: 8_000);
        AgentBehaviorPolicy policy = AgentBehaviorPolicy.CreateDefault();
        AgentDecisionSystem system = new AgentDecisionSystem();
        AgentDecision normal = system.Decide(
            agent.CreateSnapshot(tick: 0),
            AgentDecisionContext.AllAvailable(),
            policy,
            tick: 0);
        AgentDecision loaded = system.Decide(
            agent.CreateSnapshot(tick: 0),
            AgentDecisionContext.AllAvailable().WithTravelCostMultiplier(4d / 3d),
            policy,
            tick: 0);

        UtilityOptionDiagnostic normalWork = normal.Options.Single(
            option => option.IntentKind == AgentIntentKind.Work);
        UtilityOptionDiagnostic loadedWork = loaded.Options.Single(
            option => option.IntentKind == AgentIntentKind.Work);
        UtilityOptionDiagnostic normalIdle = normal.Options.Single(
            option => option.IntentKind == AgentIntentKind.Idle);
        UtilityOptionDiagnostic loadedIdle = loaded.Options.Single(
            option => option.IntentKind == AgentIntentKind.Idle);

        Assert.Equal(normalWork.BaseScore, loadedWork.BaseScore);
        Assert.True(loadedWork.FinalScore < normalWork.FinalScore);
        Assert.Equal(normalIdle.FinalScore, loadedIdle.FinalScore);
    }

    [Fact]
    public void Critical_survival_remains_eligible_with_high_travel_cost()
    {
        AgentState agent = AgentTestFactory.CreateAgent(
            nutrition: 1_000,
            alertness: 8_000,
            mood: 8_000);

        AgentDecision decision = new AgentDecisionSystem().Decide(
            agent.CreateSnapshot(tick: 0),
            AgentDecisionContext.AllAvailable().WithTravelCostMultiplier(20d / 13d),
            AgentBehaviorPolicy.CreateDefault(),
            tick: 0);

        Assert.Equal(AgentIntentKind.Eat, decision.SelectedIntent);
        Assert.True(decision.Critical);
    }
}

}