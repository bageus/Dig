using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Runtime;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Runtime;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class AgentAutonomySystemTests
{
    [Fact]
    public void Resident_eats_sleeps_and_works_without_direct_orders()
    {
        DailySchedule schedule = new DailySchedule(
            ticksPerDay: 6,
            new[]
            {
                new ScheduleSegment(0, 2, ScheduleActivity.Work),
                new ScheduleSegment(2, 4, ScheduleActivity.Rest),
                new ScheduleSegment(4, 6, ScheduleActivity.Sleep),
            });
        AgentState agent = AgentTestFactory.CreateAgent(
            nutrition: 3_000,
            alertness: 8_000,
            mood: 8_000,
            schedule: schedule);
        RuntimeHarness harness = CreateHarness(agent);
        List<AgentIntentKind> sequence = new List<AgentIntentKind>();

        for (long tick = 1; tick <= 6; tick++)
        {
            harness.System.Execute(new SimulationContext(tick, harness.Simulation));
            sequence.Add(Assert.Single(harness.System.LastReport!.Decisions).Decision.SelectedIntent);
        }

        Assert.Contains(AgentIntentKind.Eat, sequence);
        Assert.Contains(AgentIntentKind.Sleep, sequence);
        Assert.Contains(AgentIntentKind.Work, sequence);
        Assert.True(agent.IsAlive);
    }

    [Fact]
    public void Critical_hunger_does_not_interrupt_active_work_during_work_schedule()
    {
        AgentState agent = AgentTestFactory.CreateAgent(
            nutrition: 1_000,
            alertness: 8_000,
            mood: 8_000);
        AgentBehaviorPolicy policy = AgentBehaviorPolicy.CreateDefault();
        Assert.True(agent.ApplyDecision(
            AgentTestFactory.CreateForcedDecision(AgentIntentKind.Work, tick: 0),
            policy,
            tick: 0).IsSuccess);
        RuntimeHarness harness = CreateHarness(agent, policy);

        harness.System.Execute(new SimulationContext(1, harness.Simulation));

        AgentDecision decision = Assert.Single(
            harness.System.LastReport!.Decisions).Decision;
        Assert.Equal(AgentIntentKind.Work, decision.SelectedIntent);
        Assert.Equal(
            AgentIntentKind.Work,
            agent.CreateSnapshot(1).ActiveAction!.Value.IntentKind);
        UtilityOptionDiagnostic eat = Assert.Single(
            decision.Options,
            option => option.IntentKind == AgentIntentKind.Eat);
        Assert.False(eat.Available);
        Assert.DoesNotContain(
            harness.Journal.Events,
            domainEvent => domainEvent is AgentActionInterrupted interrupted
                && interrupted.NextIntent == AgentIntentKind.Eat);
    }

    [Fact]
    public void Same_inputs_produce_same_intent_sequence()
    {
        AgentState first = AgentTestFactory.CreateAgent(
            nutrition: 1_000,
            alertness: 2_500,
            mood: 6_000,
            id: EntityId.Parse("22222222222222222222222222222222"));
        AgentState second = AgentTestFactory.CreateAgent(
            nutrition: 1_000,
            alertness: 2_500,
            mood: 6_000,
            id: EntityId.Parse("33333333333333333333333333333333"));
        RuntimeHarness firstHarness = CreateHarness(first);
        RuntimeHarness secondHarness = CreateHarness(second);
        List<AgentIntentKind> firstSequence = new List<AgentIntentKind>();
        List<AgentIntentKind> secondSequence = new List<AgentIntentKind>();

        for (long tick = 1; tick <= 8; tick++)
        {
            firstHarness.System.Execute(new SimulationContext(tick, firstHarness.Simulation));
            secondHarness.System.Execute(new SimulationContext(tick, secondHarness.Simulation));
            firstSequence.Add(
                Assert.Single(firstHarness.System.LastReport!.Decisions).Decision.SelectedIntent);
            secondSequence.Add(
                Assert.Single(secondHarness.System.LastReport!.Decisions).Decision.SelectedIntent);
        }

        Assert.Equal(firstSequence, secondSequence);
    }

    [Fact]
    public void Player_order_command_flows_through_application_and_query()
    {
        AgentState agent = AgentTestFactory.CreateAgent(
            nutrition: 9_000,
            alertness: 9_000,
            mood: 9_000);
        RuntimeHarness harness = CreateHarness(agent);
        PlayerOrder order = new PlayerOrder(
            "order-inspect",
            "Inspect selected tunnel",
            priority: 8_000,
            issuedTick: 0,
            expiresTick: 10);
        SetAgentPlayerOrderCommandHandler setOrder = new SetAgentPlayerOrderCommandHandler(
            harness.Repository,
            harness.Journal);

        Assert.True(setOrder.Handle(
            new SetAgentPlayerOrderCommand(agent.Id, order, tick: 0)).IsSuccess);
        harness.System.Execute(new SimulationContext(1, harness.Simulation));
        Result<AgentSnapshot> snapshotResult = new GetAgentSnapshotQueryHandler(
            harness.Repository).Handle(new GetAgentSnapshotQuery(agent.Id, tick: 1));

        Assert.True(snapshotResult.IsSuccess);
        Assert.Equal(
            AgentIntentKind.PlayerOrder,
            snapshotResult.Value.ActiveAction!.Value.IntentKind);
        Assert.Equal(order.Id, snapshotResult.Value.ActiveAction.Value.PlayerOrderId);
        Assert.Contains(
            harness.Journal.Events,
            domainEvent => domainEvent is AgentPlayerOrderChanged changed
                && changed.PlayerOrderId == order.Id);
    }


    [Fact]
    public void Execution_override_owns_real_action_progress_without_generic_delta()
    {
        AgentState agent = AgentTestFactory.CreateAgent(
            nutrition: 8_000,
            alertness: 8_000,
            mood: 8_000);
        InMemoryAgentRepository repository = new InMemoryAgentRepository();
        Assert.True(repository.Add(agent).IsSuccess);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        HoldingExecutionOverride execution = new HoldingExecutionOverride();
        AgentAutonomySystem system = new AgentAutonomySystem(
            repository,
            new InMemoryAgentDecisionContextProvider(
                AgentDecisionContext.AllAvailable()),
            journal,
            new AgentDecisionSystem(),
            AgentBehaviorPolicy.CreateDefault(),
            executionOverride: execution);
        SimulationState simulation = SimulationState.Create(
            worldSeed: 7,
            tickDuration: TimeSpan.FromSeconds(2));

        system.Execute(new SimulationContext(1, simulation));

        Assert.True(execution.Executed);
        AgentActionSnapshot action = agent.CreateSnapshot(1).ActiveAction!.Value;
        Assert.Equal(0, action.ElapsedTicks);
    }

    [Fact]
    public void Missing_agent_order_returns_stable_error()
    {
        InMemoryAgentRepository repository = new InMemoryAgentRepository();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        SetAgentPlayerOrderCommandHandler handler = new SetAgentPlayerOrderCommandHandler(
            repository,
            journal);
        PlayerOrder order = new PlayerOrder(
            "missing",
            "Missing target",
            priority: 1,
            issuedTick: 0,
            expiresTick: 1);

        Result result = handler.Handle(new SetAgentPlayerOrderCommand(
            AgentTestFactory.DefaultAgentId,
            order,
            tick: 0));

        Assert.True(result.IsFailure);
        Assert.Equal(AgentApplicationErrors.NotFound, result.Error);
    }

    private static RuntimeHarness CreateHarness(
        AgentState agent,
        AgentBehaviorPolicy? policy = null)
    {
        InMemoryAgentRepository repository = new InMemoryAgentRepository();
        Assert.True(repository.Add(agent).IsSuccess);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        InMemoryAgentDecisionContextProvider contexts =
            new InMemoryAgentDecisionContextProvider(
                AgentDecisionContext.AllAvailable());
        AgentAutonomySystem system = new AgentAutonomySystem(
            repository,
            contexts,
            journal,
            new AgentDecisionSystem(),
            policy ?? AgentBehaviorPolicy.CreateDefault());
        SimulationState simulation = SimulationState.Create(
            worldSeed: 42,
            tickDuration: TimeSpan.FromMilliseconds(100));
        return new RuntimeHarness(repository, journal, system, simulation);
    }


    private sealed class HoldingExecutionOverride : IAgentIntentExecutionOverride
    {
        public bool Executed { get; private set; }

        public bool TryExecute(
            AgentState agent,
            AgentDecision decision,
            AgentBehaviorPolicy policy,
            long tick)
        {
            Executed = true;
            Assert.True(agent.ApplyDecision(decision, policy, tick).IsSuccess);
            return true;
        }
    }

    private sealed class RuntimeHarness
    {
        public RuntimeHarness(
            InMemoryAgentRepository repository,
            InMemoryExecutionJournal journal,
            AgentAutonomySystem system,
            SimulationState simulation)
        {
            Repository = repository;
            Journal = journal;
            System = system;
            Simulation = simulation;
        }

        public InMemoryAgentRepository Repository { get; }

        public InMemoryExecutionJournal Journal { get; }

        public AgentAutonomySystem System { get; }

        public SimulationState Simulation { get; }
    }
}
}
