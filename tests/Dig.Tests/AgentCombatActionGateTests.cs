using System;
using Dig.Application.Agents;
using Dig.Application.Runtime;
using Dig.Domain.Agents;
using Dig.Domain.Runtime;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class AgentCombatActionGateTests
{
    [Fact]
    public void Active_action_can_be_interrupted_without_applying_another_interval()
    {
        AgentState agent = AgentTestFactory.CreateAgent(
            nutrition: 8_000,
            alertness: 8_000,
            mood: 8_000);
        AgentBehaviorPolicy policy = AgentBehaviorPolicy.CreateDefault();
        Assert.True(agent.ApplyDecision(
            AgentTestFactory.CreateForcedDecision(AgentIntentKind.Work, tick: 0),
            policy,
            tick: 0).IsSuccess);
        AgentNeedsSnapshot before = agent.CreateSnapshot(0).Needs;

        Assert.True(agent.InterruptActiveAction("combat_preempted", tick: 1).IsSuccess);

        AgentSnapshot snapshot = agent.CreateSnapshot(1);
        Assert.Null(snapshot.ActiveAction);
        Assert.Equal("combat_preempted", agent.LastActionBlockReason);
        Assert.Equal(before.Nutrition.Points, snapshot.Needs.Nutrition.Points);
        Assert.Equal(before.Alertness.Points, snapshot.Needs.Alertness.Points);
        Assert.Equal(before.Mood.Points, snapshot.Needs.Mood.Points);
        Assert.Equal(before.Health.Points, snapshot.Needs.Health.Points);
        Assert.Contains(
            agent.DequeueUncommittedEvents(),
            domainEvent => domainEvent is AgentActionBlocked blocked
                && blocked.Reason == "combat_preempted");
    }

    [Fact]
    public void Closed_action_gate_skips_food_and_schedule_action_execution()
    {
        AgentState agent = AgentTestFactory.CreateAgent(
            nutrition: 2_000,
            alertness: 2_000,
            mood: 2_000);
        InMemoryAgentRepository repository = new InMemoryAgentRepository();
        Assert.True(repository.Add(agent).IsSuccess);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        ClosedGate execution = new ClosedGate();
        AgentAutonomySystem system = new AgentAutonomySystem(
            repository,
            new InMemoryAgentDecisionContextProvider(
                AgentDecisionContext.AllAvailable()),
            journal,
            new AgentDecisionSystem(),
            AgentBehaviorPolicy.CreateDefault(),
            executionOverride: execution);
        SimulationState simulation = SimulationState.Create(
            worldSeed: 19,
            tickDuration: TimeSpan.FromSeconds(2));

        system.Execute(new SimulationContext(1, simulation));

        Assert.True(execution.GateChecked);
        Assert.False(execution.Executed);
        Assert.Empty(system.LastReport!.Decisions);
        Assert.Null(agent.CreateSnapshot(1).ActiveAction);
    }

    private sealed class ClosedGate :
        IAgentIntentExecutionOverride,
        IAgentActionExecutionGate
    {
        public bool GateChecked { get; private set; }
        public bool Executed { get; private set; }

        public bool CanExecuteActions(AgentState agent, long tick)
        {
            GateChecked = true;
            return false;
        }

        public bool TryExecute(
            AgentState agent,
            AgentDecision decision,
            AgentBehaviorPolicy policy,
            long tick)
        {
            Executed = true;
            return true;
        }
    }
}

}