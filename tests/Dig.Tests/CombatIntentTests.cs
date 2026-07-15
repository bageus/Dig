using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Combat;
using Dig.Application.Messaging;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class CombatIntentTests
{
    private static readonly EntityId ActorId = EntityId.Parse(
        "d1000000000000000000000000000001");
    private static readonly EntityId TargetId = EntityId.Parse(
        "d2000000000000000000000000000002");

    [Fact]
    public void Same_intent_id_is_idempotent_and_new_intent_replaces_active_one()
    {
        CombatState combat = CreateCombat();
        CombatIntentRequest attack = new CombatIntentRequest(
            new CombatIntentId("intent.attack.1"),
            ActorId,
            CombatIntentKind.Attack,
            CombatIntentSource.PlayerOrder,
            createdTick: 1,
            expiresTick: 10,
            targetEntityId: TargetId);

        CombatIntentSnapshot first = combat.IssueIntent(attack);
        int eventCount = combat.PeekUncommittedEvents().Count;
        CombatIntentSnapshot replay = combat.IssueIntent(attack);
        CombatIntentSnapshot retreat = combat.IssueIntent(new CombatIntentRequest(
            new CombatIntentId("intent.retreat.1"),
            ActorId,
            CombatIntentKind.Retreat,
            CombatIntentSource.Autonomous,
            createdTick: 2,
            expiresTick: 5,
            targetCell: new CellId(0, 0)));

        Assert.Equal(first.IntentId, replay.IntentId);
        Assert.Equal(eventCount, 1);
        Assert.Equal(CombatIntentKind.Retreat, retreat.Kind);
        Assert.Equal(retreat.IntentId, combat.GetActiveIntent(ActorId)!.IntentId);
        CombatIntentSnapshot previous = combat.CreateIntentSnapshot()
            .Single(item => item.IntentId == first.IntentId);
        Assert.Equal(CombatIntentStatus.Cancelled, previous.Status);
        Assert.Equal("replaced_by_new_intent", previous.FinishReason);
        Assert.Single(combat.PeekUncommittedEvents().OfType<CombatIntentFinished>());
        Assert.Equal(2, combat.PeekUncommittedEvents().OfType<CombatIntentChanged>().Count());
    }

    [Fact]
    public void Intent_completion_and_expiry_are_applied_once()
    {
        CombatState combat = CreateCombat();
        CombatIntentSnapshot defend = combat.IssueIntent(new CombatIntentRequest(
            new CombatIntentId("intent.defend.1"),
            ActorId,
            CombatIntentKind.Defend,
            CombatIntentSource.Alarm,
            createdTick: 1,
            expiresTick: 3,
            targetCell: new CellId(4, 4)));

        Assert.Empty(combat.ExpireIntents(2));
        Assert.Single(combat.ExpireIntents(3));
        Assert.Empty(combat.ExpireIntents(3));
        Assert.Null(combat.GetActiveIntent(ActorId));
        Assert.True(combat.CompleteIntent(defend.IntentId, tick: 4).IsSuccess);
        Assert.Equal(
            CombatIntentStatus.Expired,
            combat.CreateIntentSnapshot().Single().Status);
    }

    [Fact]
    public void Application_handler_publishes_intent_facts_without_world_mutation()
    {
        CombatState combat = CreateCombat();
        RecordingEventSink events = new RecordingEventSink();
        IssueCombatIntentHandler handler = new IssueCombatIntentHandler(
            new InMemoryCombatRepository(combat),
            events);
        CombatIntentRequest request = new CombatIntentRequest(
            new CombatIntentId("intent.application.1"),
            ActorId,
            CombatIntentKind.Approach,
            CombatIntentSource.StrategicAi,
            createdTick: 10,
            expiresTick: 20,
            targetEntityId: TargetId);

        CombatIntentSnapshot result = handler.Handle(
            new IssueCombatIntentCommand(request));

        Assert.Equal(CombatIntentStatus.Active, result.Status);
        Assert.Single(events.Events.OfType<CombatIntentChanged>());
        Assert.Equal(result.IntentId, combat.GetActiveIntent(ActorId)!.IntentId);
    }

    private static CombatState CreateCombat()
    {
        return new CombatState(new WeaponCatalog(new[]
        {
            new WeaponProfile(
                new WeaponProfileId("weapon.intent.test"),
                minimumRange: 1,
                maximumRange: 1,
                accuracy: 10_000,
                baseDamage: 1,
                armorPenetration: 0,
                cooldownTicks: 0),
        }));
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
