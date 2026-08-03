using Dig.Domain.Agents;
using Xunit;

namespace Dig.Tests
{

public sealed class AgentDecisionIdleFallbackTests
{
    [Fact]
    public void Unavailable_current_intent_during_cooldown_falls_back_to_idle()
    {
        AgentBehaviorPolicy policy = AgentBehaviorPolicy.CreateDefault();
        AgentDecisionSystem decisions = new AgentDecisionSystem();
        AgentState agent = AgentTestFactory.CreateAgent(
            nutrition: 8_000,
            alertness: 8_000,
            mood: 8_000);
        Assert.True(agent.ApplyDecision(
            AgentTestFactory.CreateForcedDecision(AgentIntentKind.Flee, tick: 0),
            policy,
            tick: 0).IsSuccess);
        AgentDecisionContext unavailable = new AgentDecisionContext(
            foodAvailable: false,
            bedAvailable: false,
            workAvailable: false,
            restAvailable: false,
            escapeRouteAvailable: false,
            threatLevel: 0);

        AgentDecision decision = decisions.Decide(
            agent.CreateSnapshot(tick: 1),
            unavailable,
            policy,
            tick: 1);

        Assert.Equal(AgentIntentKind.Idle, decision.SelectedIntent);
        UtilityOptionDiagnostic flee = Assert.Single(
            decision.Options,
            option => option.IntentKind == AgentIntentKind.Flee);
        Assert.False(flee.Available);
        Assert.Equal("rejected.unavailable", flee.ReasonCode);
        UtilityOptionDiagnostic idle = Assert.Single(
            decision.Options,
            option => option.IntentKind == AgentIntentKind.Idle);
        Assert.True(idle.Selected);
        Assert.Equal("selected.utility", idle.ReasonCode);
    }
}

}