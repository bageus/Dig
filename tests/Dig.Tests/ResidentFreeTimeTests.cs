using System;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Application.Agents;
using Dig.Domain.World;
using Dig.Domain.Core;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentFreeTimeTests
{
    [Fact]
    public void Direct_order_during_free_time_reduces_mood_once()
    {
        AgentState resident = CreateResident(ScheduleActivity.Free);

        Result applied = resident.ApplyFreeTimeDirectOrderPenalty(tick: 1);

        Assert.True(applied.IsSuccess);
        Assert.Equal(
            8_000 - AgentState.FreeTimeDirectOrderMoodPenalty,
            resident.CreateSnapshot(1).Needs.Mood.Points);
    }

    [Fact]
    public void Direct_order_during_work_does_not_reduce_mood()
    {
        AgentState resident = CreateResident(ScheduleActivity.Work);

        Result applied = resident.ApplyFreeTimeDirectOrderPenalty(tick: 1);

        Assert.True(applied.IsSuccess);
        Assert.Equal(8_000, resident.CreateSnapshot(1).Needs.Mood.Points);
    }

    [Fact]
    public void Leisure_history_is_committed_after_first_effect_and_penalizes_repetition()
    {
        AgentState resident = CreateResident(ScheduleActivity.Free);
        LeisureActivityDefinition play = new LeisureActivityDefinition(
            new LeisureVarietyId("solo_play"), 100, 2, false);
        long tick = 0;
        for (int action = 0; action < 6; action++)
        {
            int historyCount = resident.CreateLeisureRuntimeSnapshot().History.Count;
            Assert.True(resident.BeginLeisure(play, null, tick).IsSuccess);
            Assert.Equal(historyCount, resident.CreateLeisureRuntimeSnapshot().History.Count);
            tick += 2;
            Assert.True(resident.AdvanceLeisure(play, tick).IsSuccess);
            resident.CancelLeisure();
        }

        LeisureRuntimeSnapshot snapshot = resident.CreateLeisureRuntimeSnapshot();
        Assert.Equal(6, snapshot.History.Count);
        Assert.Equal(8_000 + 550, resident.CreateSnapshot(tick).Needs.Mood.Points);
    }

    [Fact]
    public void Leisure_runtime_round_trip_keeps_choice_partner_and_history()
    {
        AgentState resident = CreateResident(ScheduleActivity.Free);
        EntityId partner = EntityId.Parse("00000000-0000-0000-0000-000000000002");
        LeisureActivityDefinition social = new LeisureActivityDefinition(
            new LeisureVarietyId("social"), 60, 5, true);
        Assert.True(resident.BeginLeisure(social, partner, 10).IsSuccess);
        Assert.True(resident.AdvanceLeisure(social, 15).IsSuccess);

        AgentRuntimeSnapshot saved = resident.CreateRuntimeSnapshot();
        AgentState restored = CreateResident(ScheduleActivity.Free);
        Assert.True(restored.RestoreRuntime(saved).IsSuccess);

        LeisureRuntimeSnapshot actual = restored.CreateLeisureRuntimeSnapshot();
        Assert.Equal("social", actual.ActiveVariety!.Value.ToString());
        Assert.Equal(partner, actual.PartnerId);
        Assert.True(actual.HistoryCommitted);
        Assert.Single(actual.History);
    }

    [Fact]
    public void Selector_is_deterministic_and_uses_previous_history_weights()
    {
        LeisureActivityDefinition repeated = new LeisureActivityDefinition(
            new LeisureVarietyId("repeated"), 10, 1, false);
        LeisureActivityDefinition rare = new LeisureActivityDefinition(
            new LeisureVarietyId("rare"), 10, 1, false);
        LeisureVarietyId[] history = Enumerable.Repeat(repeated.Id, 10).ToArray();
        LeisureActivitySelector selector = new LeisureActivitySelector();
        int rareSelections = Enumerable.Range(0, 1_000).Count(index =>
            selector.Select(new[] { repeated, rare }, history, 42, index).Id.Equals(rare.Id));

        Assert.True(rareSelections > 800);
        Assert.Equal(
            selector.Select(new[] { repeated, rare }, history, 42, 17).Id,
            selector.Select(new[] { rare, repeated }, history, 42, 17).Id);
    }

    [Fact]
    public void Selector_keeps_active_social_choice_for_the_same_partner()
    {
        EntityId partner = EntityId.Parse("00000000-0000-0000-0000-000000000002");
        LeisureActivityDefinition group = new LeisureActivityDefinition(
            new LeisureVarietyId("group"), 50, 25, true);
        LeisureActivityDefinition social = new LeisureActivityDefinition(
            new LeisureVarietyId("social"), 60, 25, true);
        AgentState resident = CreateResident(ScheduleActivity.Free);
        Assert.True(resident.BeginLeisure(group, partner, 10).IsSuccess);

        LeisureActivityDefinition selected = new LeisureActivitySelector().SelectOrContinue(
            new[] { group, social },
            resident.CreateLeisureRuntimeSnapshot(),
            partner,
            worldSeed: 42,
            decisionId: 999);

        Assert.Equal(group.Id, selected.Id);
        Assert.Equal(35, resident.CreateLeisureRuntimeSnapshot().NextEffectTick);
    }

    [Fact]
    public void Social_reservation_claims_both_residents_and_meeting_cell_atomically()
    {
        EntityId first = EntityId.Parse("00000000-0000-0000-0000-000000000001");
        EntityId second = EntityId.Parse("00000000-0000-0000-0000-000000000002");
        EntityId third = EntityId.Parse("00000000-0000-0000-0000-000000000003");
        CellId meeting = new CellId(2, 0, 3);
        LeisureReservationLedger ledger = new LeisureReservationLedger();

        Assert.True(ledger.TryReservePair(first, second, meeting));
        Assert.Equal(second, ledger.GetPartner(first));
        Assert.False(ledger.TryReservePair(first, third, new CellId(3, 0, 3)));
        Assert.False(ledger.TryReservePair(third, EntityId.Parse(
            "00000000-0000-0000-0000-000000000004"), meeting));
        ledger.Release(first);
        Assert.True(ledger.TryReservePair(first, third, meeting));
    }

    private static AgentState CreateResident(ScheduleActivity activity)
    {
        return new AgentState(
            AgentTestFactory.DefaultAgentId,
            "Free Time Test",
            AgentTestFactory.CreateNeeds(8_000, 8_000, 8_000, 10_000),
            new DailySchedule(
                ticksPerDay: 4,
                new[] { new ScheduleSegment(0, 4, activity) }));
    }

}

}
