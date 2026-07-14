using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Xunit;

namespace Dig.Tests;

public sealed class ResidentSettlementTests
{
    private static readonly EntityId FirstAgent =
        EntityId.Parse("91000000000000000000000000000001");
    private static readonly EntityId SecondAgent =
        EntityId.Parse("91000000000000000000000000000002");

    [Fact]
    public void One_food_portion_is_consumed_by_only_one_critical_agent()
    {
        ResidentSettlementHarness harness = new ResidentSettlementHarness();
        harness.AddAgent(FirstAgent.ToString(), nutrition: 500, alertness: 8_000, mood: 8_000);
        harness.AddAgent(SecondAgent.ToString(), nutrition: 500, alertness: 8_000, mood: 8_000);
        harness.AddFood("92000000000000000000000000000001", quantity: 1);

        harness.Execute(tick: 0);

        Assert.Equal(1, harness.Inventory.GetTotal(ResidentSettlementHarness.Meal));
        Assert.Single(harness.Inventory.CreateSnapshot().Stacks
            .SelectMany(value => value.Reservations));
        Assert.Equal(AgentIntentKind.Eat, harness.Snapshot(FirstAgent, 0).ActiveAction!.Value.IntentKind);
        Assert.Equal("food_unavailable", harness.Agents.Get(SecondAgent)!.LastActionBlockReason);

        harness.Execute(tick: 1);

        Assert.Equal(0, harness.Inventory.GetTotal(ResidentSettlementHarness.Meal));
        Assert.True(harness.Snapshot(FirstAgent, 1).Needs.Nutrition.Points >= 3_000);
        Assert.True(harness.Snapshot(SecondAgent, 1).Needs.Nutrition.Points < 1_000);
        Assert.Single(harness.Journal.Events.OfType<ReservedItemConsumed>());
    }

    [Fact]
    public void One_bed_is_used_sequentially_without_double_reservation()
    {
        ResidentSettlementHarness harness = new ResidentSettlementHarness();
        harness.AddAgent(FirstAgent.ToString(), nutrition: 9_000, alertness: 500, mood: 8_000);
        harness.AddAgent(SecondAgent.ToString(), nutrition: 9_000, alertness: 500, mood: 8_000);
        harness.AddFacility(
            "94000000000000000000000000000001",
            BuildingFacilityKind.Bed,
            x: 4);

        harness.Execute(tick: 0);

        BuildingFacilityReservation firstReservation = Assert.Single(
            harness.Facilities.GetReservations());
        Assert.Equal(FirstAgent, firstReservation.AgentId);
        Assert.Equal("bed_unavailable", harness.Agents.Get(SecondAgent)!.LastActionBlockReason);

        harness.Execute(tick: 1);
        harness.Execute(tick: 2);

        BuildingFacilityReservation secondReservation = Assert.Single(
            harness.Facilities.GetReservations());
        Assert.Equal(SecondAgent, secondReservation.AgentId);
        Assert.True(harness.Snapshot(FirstAgent, 2).Needs.Alertness.Points >= 2_000);

        harness.Execute(tick: 3);
        harness.Execute(tick: 4);

        Assert.Empty(harness.Facilities.GetReservations());
        Assert.True(harness.Snapshot(SecondAgent, 4).Needs.Alertness.Points >= 2_000);
    }

    [Fact]
    public void Leisure_effect_is_applied_only_after_reserved_action_completes()
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
        Assert.Equal(900, inProgress.Needs.Mood.Points);
        Assert.False(harness.System.LastReport!.Agents[0].ActionCompleted);

        harness.Execute(tick: 1);

        AgentSnapshot completed = harness.Snapshot(FirstAgent, 1);
        Assert.Null(completed.ActiveAction);
        Assert.True(completed.Needs.Mood.Points >= 2_500);
        Assert.True(harness.System.LastReport!.Agents[0].ActionCompleted);
        Assert.Empty(harness.Facilities.GetReservations());
    }

    [Fact]
    public void Missing_reserved_food_blocks_action_without_applying_need_effect()
    {
        ResidentSettlementHarness harness = new ResidentSettlementHarness();
        harness.AddAgent(FirstAgent.ToString(), nutrition: 500, alertness: 9_000, mood: 9_000);
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
        Assert.Equal("food_unavailable", harness.Agents.Get(FirstAgent)!.LastActionBlockReason);
        Assert.True(snapshot.Needs.Nutrition.Points < 1_000);
        Assert.Contains(
            harness.Journal.Events,
            value => value is AgentActionBlocked blocked
                && blocked.AgentId == FirstAgent);
    }
}
