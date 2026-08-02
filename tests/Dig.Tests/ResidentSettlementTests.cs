using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentSettlementTests
{
    private static readonly EntityId FirstAgent =
        EntityId.Parse("91000000000000000000000000000001");
    private static readonly EntityId SecondAgent =
        EntityId.Parse("91000000000000000000000000000002");

    [Fact]
    public void One_food_portion_is_consumed_by_only_one_free_time_critical_agent()
    {
        ResidentSettlementHarness harness = new ResidentSettlementHarness();
        harness.AddAgent(
            FirstAgent.ToString(),
            nutrition: 500,
            alertness: 8_000,
            mood: 8_000,
            scheduleActivity: ScheduleActivity.Rest);
        harness.AddAgent(
            SecondAgent.ToString(),
            nutrition: 500,
            alertness: 8_000,
            mood: 8_000,
            scheduleActivity: ScheduleActivity.Rest);
        harness.AddFood("92000000000000000000000000000001", quantity: 1);

        harness.Execute(tick: 0);

        Assert.Equal(1, harness.Inventory.GetTotal(ResidentSettlementHarness.Meal));
        Assert.Single(harness.Inventory.CreateSnapshot().Stacks
            .SelectMany(value => value.Reservations));
        Assert.Equal(
            AgentIntentKind.Eat,
            harness.Snapshot(FirstAgent, 0).ActiveAction!.Value.IntentKind);
        Assert.Equal(
            "food_unavailable",
            harness.Agents.Get(SecondAgent)!.LastActionBlockReason);

        harness.Execute(tick: 1);

        Assert.Equal(0, harness.Inventory.GetTotal(ResidentSettlementHarness.Meal));
        Assert.True(harness.Snapshot(FirstAgent, 1).Needs.Nutrition.Points >= 3_000);
        Assert.True(harness.Snapshot(SecondAgent, 1).Needs.Nutrition.Points < 1_000);
        Assert.Single(harness.Journal.Events.OfType<ReservedItemConsumed>());
    }

    [Fact]
    public void Available_bed_is_preferred_and_second_tired_resident_sleeps_on_floor()
    {
        ResidentSettlementHarness harness = new ResidentSettlementHarness();
        harness.AddAgent(
            FirstAgent.ToString(),
            nutrition: 9_000,
            alertness: 500,
            mood: 8_000);
        harness.AddAgent(
            SecondAgent.ToString(),
            nutrition: 9_000,
            alertness: 500,
            mood: 8_000);
        harness.AddFacility(
            "94000000000000000000000000000001",
            BuildingFacilityKind.Bed,
            x: 4);

        harness.Execute(tick: 0);

        BuildingFacilityReservation reservation = Assert.Single(
            harness.Facilities.GetReservations());
        Assert.Equal(FirstAgent, reservation.AgentId);
        AgentActivityTarget firstTarget = harness.Snapshot(FirstAgent, 0)
            .ActiveAction!.Value.Target!.Value;
        AgentActivityTarget secondTarget = harness.Snapshot(SecondAgent, 0)
            .ActiveAction!.Value.Target!.Value;
        Assert.Equal(AgentActivityTargetKind.Bed, firstTarget.Kind);
        Assert.Equal(AgentActivityTargetKind.FloorSleep, secondTarget.Kind);
        Assert.Null(harness.Agents.Get(SecondAgent)!.LastActionBlockReason);

        harness.Execute(tick: 1);
        harness.Execute(tick: 2);

        Assert.Empty(harness.Facilities.GetReservations());
        Assert.True(harness.Snapshot(FirstAgent, 2).Needs.Alertness.Points > 500);
        Assert.True(harness.Snapshot(SecondAgent, 2).Needs.Alertness.Points > 500);
    }

    [Fact]
    public void Leisure_effect_is_applied_progressively_while_reserved_action_runs()
    {
        ResidentSettlementHarness harness = new ResidentSettlementHarness();
        harness.AddAgent(
            FirstAgent.ToString(),
            nutrition: 9_000,
            alertness: 9_000,
            mood: 1_000,
            scheduleActivity: ScheduleActivity.Rest);
        harness.AddFacility(
            "94000000000000000000000000000002",
            BuildingFacilityKind.Leisure,
            x: 5);

        harness.Execute(tick: 0);

        AgentSnapshot inProgress = harness.Snapshot(FirstAgent, 0);
        Assert.Equal(AgentIntentKind.Rest, inProgress.ActiveAction!.Value.IntentKind);
        Assert.True(inProgress.Needs.Mood.Points > 900);
        Assert.False(harness.System.LastReport!.Agents[0].ActionCompleted);

        harness.Execute(tick: 1);

        AgentSnapshot completed = harness.Snapshot(FirstAgent, 1);
        Assert.Null(completed.ActiveAction);
        Assert.True(completed.Needs.Mood.Points >= 2_500);
        Assert.True(harness.System.LastReport!.Agents[0].ActionCompleted);
        Assert.Empty(harness.Facilities.GetReservations());
    }

    [Fact]
    public void Missing_reserved_food_blocks_action_and_keeps_applied_interval()
    {
        ResidentSettlementHarness harness = new ResidentSettlementHarness();
        harness.AddAgent(
            FirstAgent.ToString(),
            nutrition: 500,
            alertness: 9_000,
            mood: 9_000,
            scheduleActivity: ScheduleActivity.Rest);
        EntityId stackId = harness.AddFood(
            "92000000000000000000000000000003",
            quantity: 1);
        harness.Execute(tick: 0);
        Assert.True(harness.Inventory.ConsumeReserved(
            FirstAgent,
            stackId,
            quantity: 1,
            tick: 0).IsSuccess);

        harness.Execute(tick: 1);

        AgentSnapshot snapshot = harness.Snapshot(FirstAgent, 1);
        Assert.Null(snapshot.ActiveAction);
        Assert.Equal(
            "food_unavailable",
            harness.Agents.Get(FirstAgent)!.LastActionBlockReason);
        Assert.True(snapshot.Needs.Nutrition.Points > 1_000);
        Assert.Contains(
            harness.Journal.Events,
            value => value is AgentActionBlocked blocked
                && blocked.AgentId == FirstAgent);
    }

    [Fact]
    public void Work_time_hunger_notifies_without_automatic_food_reservation()
    {
        ResidentSettlementHarness harness = new ResidentSettlementHarness();
        harness.AddAgent(
            FirstAgent.ToString(),
            nutrition: 1_600,
            alertness: 9_000,
            mood: 9_000,
            scheduleActivity: ScheduleActivity.Work);
        harness.AddFood("92000000000000000000000000000004", quantity: 1);

        harness.Execute(tick: 0);

        AgentSnapshot snapshot = harness.Snapshot(FirstAgent, 0);
        Assert.Equal(AgentIntentKind.Work, snapshot.ActiveAction!.Value.IntentKind);
        Assert.Equal(1, harness.Inventory.GetTotal(ResidentSettlementHarness.Meal));
        Assert.Empty(harness.Inventory.CreateSnapshot().Stacks
            .SelectMany(value => value.Reservations));
        AgentNeedThresholdCrossed hunger = Assert.Single(
            harness.Journal.Events.OfType<AgentNeedThresholdCrossed>(),
            value => value.Kind == AgentNeedThresholdKind.Hunger);
        Assert.Equal(FirstAgent, hunger.AgentId);
    }

    [Fact]
    public void Floor_sleep_has_no_positive_mood_and_caps_alertness_at_seventy_five_percent()
    {
        ResidentSettlementHarness harness = new ResidentSettlementHarness();
        harness.AddAgent(
            FirstAgent.ToString(),
            nutrition: 9_000,
            alertness: 7_400,
            mood: 4_000,
            scheduleActivity: ScheduleActivity.Sleep);

        harness.Execute(tick: 0);
        harness.Execute(tick: 1);
        harness.Execute(tick: 2);

        AgentSnapshot snapshot = harness.Snapshot(FirstAgent, 2);
        Assert.True(snapshot.Needs.Alertness.Points <= 7_500);
        Assert.True(snapshot.Needs.Mood.Points < 4_000);
        Assert.Empty(harness.Facilities.GetReservations());
    }
}
}
