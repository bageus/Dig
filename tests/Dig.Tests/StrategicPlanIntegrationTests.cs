using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Messaging;
using Dig.Application.Strategy;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.Jobs;
using Dig.Domain.Strategy;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class StrategicPlanIntegrationTests
{
    private static readonly FactionId Player = new FactionId("faction.player");
    private static readonly FactionId Raiders = new FactionId("faction.raiders");
    private static readonly EntityId WorkerId = EntityId.Parse(
        "e1000000000000000000000000000001");
    private static readonly EntityId JobId = EntityId.Parse(
        "e2000000000000000000000000000002");

    [Fact]
    public void Resource_development_plan_materializes_claims_and_completes_job()
    {
        StrategicAiState strategy = CreateStrategy();
        StrategicDecisionReport decision = strategy.Evaluate(Context(
            tick: 0,
            resources: 0,
            housing: 5));
        Assert.Equal(StrategicGoalKind.DevelopResources, decision.CurrentGoal);
        StrategicExecutionPlanId planId = new StrategicExecutionPlanId("plan.resources.1");
        CellId target = new CellId(6, 4);
        Assert.True(strategy.CreateExecutionPlan(new StrategicExecutionPlanRequest(
            planId,
            Player,
            StrategicGoalKind.DevelopResources,
            "resource_reserve_below_target",
            createdTick: 0,
            targetCell: target)).IsSuccess);
        JobSystem jobs = new JobSystem();
        RecordingEventSink events = new RecordingEventSink();
        InMemoryStrategicAiRepository strategyRepository =
            new InMemoryStrategicAiRepository(strategy);
        InMemoryJobRepository jobsRepository = new InMemoryJobRepository(jobs);
        MaterializeStrategicPlanHandler materialize = new MaterializeStrategicPlanHandler(
            strategyRepository,
            jobsRepository,
            events);

        Assert.True(materialize.Handle(new MaterializeStrategicPlanCommand(
            planId,
            JobId,
            priority: 20,
            tick: 1)).IsSuccess);
        JobSnapshot job = jobs.Get(JobId)!;
        Assert.Equal(JobStatus.Available, job.Status);
        StrategicExecutionJobDefinition definition =
            Assert.IsType<StrategicExecutionJobDefinition>(job.Definition);
        Assert.Equal(planId, definition.PlanId);
        Assert.Equal(target, definition.TargetCell!.Value);
        Assert.True(jobs.Claim(JobId, WorkerId, tick: 2).IsSuccess);
        Assert.Contains(
            jobs.GetReservations(),
            item => item.Key == ReservationKey.ForPosition(target));
        Assert.True(jobs.Complete(JobId, tick: 3).IsSuccess);
        SynchronizeStrategicPlanJobHandler synchronize =
            new SynchronizeStrategicPlanJobHandler(
                strategyRepository,
                jobsRepository,
                events);
        Assert.True(synchronize.Handle(new SynchronizeStrategicPlanJobCommand(
            planId,
            JobId,
            tick: 3)).IsSuccess);

        StrategicExecutionPlanSnapshot completed = strategy.GetExecutionPlan(planId)!;
        Assert.Equal(StrategicExecutionPlanStatus.Completed, completed.Status);
        Assert.Equal(JobId, completed.JobId!.Value);
        Assert.Null(strategy.GetActiveExecutionPlan(Player));
        Assert.Empty(jobs.GetReservations());
        Assert.Contains(
            events.Events.OfType<JobStatusChanged>(),
            item => item.JobId == JobId && item.CurrentStatus == JobStatus.Available);
        Assert.Contains(events.Events, item => item is StrategicExecutionPlanChanged);
    }

    [Fact]
    public void Attack_plan_materializes_with_target_faction()
    {
        StrategicAiState strategy = CreateStrategy();
        StrategicDecisionReport decision = strategy.Evaluate(Context(
            tick: 0,
            resources: 100,
            housing: 5,
            ownStrength: 20_000,
            hostileStrength: 5_000,
            hostileTarget: Raiders));
        Assert.Equal(StrategicGoalKind.Attack, decision.CurrentGoal);
        StrategicExecutionPlanId planId = new StrategicExecutionPlanId("plan.attack.1");
        Assert.True(strategy.CreateExecutionPlan(new StrategicExecutionPlanRequest(
            planId,
            Player,
            StrategicGoalKind.Attack,
            "hostile_target_weaker",
            createdTick: 0,
            targetFactionId: Raiders)).IsSuccess);
        JobSystem jobs = new JobSystem();
        RecordingEventSink events = new RecordingEventSink();
        Assert.True(new MaterializeStrategicPlanHandler(
            new InMemoryStrategicAiRepository(strategy),
            new InMemoryJobRepository(jobs),
            events).Handle(new MaterializeStrategicPlanCommand(
                planId,
                JobId,
                priority: 100,
                tick: 1)).IsSuccess);

        StrategicExecutionJobDefinition job =
            Assert.IsType<StrategicExecutionJobDefinition>(jobs.Get(JobId)!.Definition);
        Assert.Equal(StrategicGoalKind.Attack, job.Goal);
        Assert.Equal(Raiders, job.TargetFactionId!.Value);
        Assert.Null(job.TargetCell);
    }

    [Fact]
    public void Cancelled_job_cancels_materialized_plan_idempotently()
    {
        StrategicAiState strategy = CreateStrategy();
        strategy.Evaluate(Context(tick: 0, resources: 0, housing: 5));
        StrategicExecutionPlanId planId = new StrategicExecutionPlanId("plan.cancel.1");
        Assert.True(strategy.CreateExecutionPlan(new StrategicExecutionPlanRequest(
            planId,
            Player,
            StrategicGoalKind.DevelopResources,
            "resource_reserve_below_target",
            createdTick: 0,
            targetCell: new CellId(3, 3))).IsSuccess);
        JobSystem jobs = new JobSystem();
        RecordingEventSink events = new RecordingEventSink();
        InMemoryStrategicAiRepository strategyRepository =
            new InMemoryStrategicAiRepository(strategy);
        InMemoryJobRepository jobsRepository = new InMemoryJobRepository(jobs);
        Assert.True(new MaterializeStrategicPlanHandler(
            strategyRepository,
            jobsRepository,
            events).Handle(new MaterializeStrategicPlanCommand(
                planId,
                JobId,
                priority: 10,
                tick: 1)).IsSuccess);
        Assert.True(jobs.Cancel(
            JobId,
            new JobBlockReason("strategy_cancelled", "Cancelled for test."),
            tick: 2).IsSuccess);
        SynchronizeStrategicPlanJobHandler handler =
            new SynchronizeStrategicPlanJobHandler(
                strategyRepository,
                jobsRepository,
                events);

        Assert.True(handler.Handle(new SynchronizeStrategicPlanJobCommand(
            planId,
            JobId,
            tick: 2)).IsSuccess);
        Assert.True(handler.Handle(new SynchronizeStrategicPlanJobCommand(
            planId,
            JobId,
            tick: 3)).IsSuccess);

        StrategicExecutionPlanSnapshot cancelled = strategy.GetExecutionPlan(planId)!;
        Assert.Equal(StrategicExecutionPlanStatus.Cancelled, cancelled.Status);
        Assert.Equal("job_cancelled", cancelled.FinishReason);
    }

    [Fact]
    public void New_execution_plan_replaces_previous_active_plan()
    {
        StrategicAiState strategy = CreateStrategy();
        strategy.Evaluate(Context(tick: 0, resources: 0, housing: 5));
        StrategicExecutionPlanId firstId = new StrategicExecutionPlanId("plan.replace.1");
        StrategicExecutionPlanId secondId = new StrategicExecutionPlanId("plan.replace.2");
        Assert.True(strategy.CreateExecutionPlan(new StrategicExecutionPlanRequest(
            firstId,
            Player,
            StrategicGoalKind.DevelopResources,
            "first",
            createdTick: 0,
            targetCell: new CellId(1, 1))).IsSuccess);

        Assert.True(strategy.CreateExecutionPlan(new StrategicExecutionPlanRequest(
            secondId,
            Player,
            StrategicGoalKind.DevelopResources,
            "second",
            createdTick: 1,
            targetCell: new CellId(2, 1))).IsSuccess);

        Assert.Equal(
            StrategicExecutionPlanStatus.Cancelled,
            strategy.GetExecutionPlan(firstId)!.Status);
        Assert.Equal(
            secondId,
            strategy.GetActiveExecutionPlan(Player)!.PlanId);
    }

    private static StrategicAiState CreateStrategy()
    {
        return new StrategicAiState(new StrategicAiPolicy(
            planningIntervalTicks: 10,
            minimumResourceReserve: 20,
            minimumFreeHousing: 2,
            attackAdvantageRatio: 1_500,
            retreatThreatRatio: 1_500));
    }

    private static StrategicAiContext Context(
        long tick,
        int resources,
        int housing,
        int ownStrength = 10_000,
        int threat = 0,
        int hostileStrength = 0,
        FactionId? hostileTarget = null)
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
            canExpandTerritory: false);
    }

    private sealed class RecordingEventSink : IEventSink
    {
        private readonly List<IDomainEvent> _events = new List<IDomainEvent>();

        public IReadOnlyList<IDomainEvent> Events => _events;

        public void Append(IReadOnlyCollection<IDomainEvent> events)
        {
            if (events is null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            _events.AddRange(events);
        }
    }
}
}
