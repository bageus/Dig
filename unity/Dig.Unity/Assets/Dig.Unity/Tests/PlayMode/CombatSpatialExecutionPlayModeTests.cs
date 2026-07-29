using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Combat;
using Dig.Application.Messaging;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class CombatSpatialExecutionPlayModeTests
{
    [Test]
    public void Direct_order_approaches_winds_up_and_commits_one_damage_result()
    {
        EntityId attackerId = EntityId.Parse("e1000000000000000000000000000001");
        EntityId targetId = EntityId.Parse("e2000000000000000000000000000002");
        FactionId residents = new FactionId("faction.playmode.residents");
        FactionId hostiles = new FactionId("faction.playmode.hostiles");
        WeaponProfileId weaponId = new WeaponProfileId("weapon.playmode.melee");
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        Assert.That(agents.Add(Agent(attackerId, new CellId(0, 0, 0))).IsSuccess, Is.True);
        Assert.That(agents.Add(Agent(targetId, new CellId(2, 0, 0))).IsSuccess, Is.True);
        FactionState factions = new FactionState(
            new FactionCatalog(new[]
            {
                new FactionDefinition(residents, "Residents", -10_000),
                new FactionDefinition(hostiles, "Hostiles", -10_000),
            }),
            new FactionDiplomacyPolicy(-5_000, 3_000, 8_000, 1_000));
        factions.AssignMember(attackerId, residents);
        factions.AssignMember(targetId, hostiles);
        CombatState combat = new CombatState(new WeaponCatalog(new[]
        {
            new WeaponProfile(
                weaponId,
                1,
                1,
                10_000,
                1_000,
                0,
                1,
                spatialMode: CombatAttackSpatialMode.Melee),
        }));
        CombatIntentSnapshot intent = combat.IssueIntent(new CombatIntentRequest(
            new CombatIntentId("intent.playmode.attack"),
            attackerId,
            CombatIntentKind.Attack,
            CombatIntentSource.PlayerOrder,
            0,
            100,
            targetId,
            new CellId(2, 0, 0)));
        Assert.That(intent.IsActive, Is.True);
        InMemoryCombatRepository combatRepository = new InMemoryCombatRepository(combat);
        RecordingSink events = new RecordingSink();
        CellId[] cells =
        {
            new CellId(0, 0, 0),
            new CellId(1, 0, 0),
            new CellId(2, 0, 0),
        };
        CombatSpatialExecutionHandler handler = new CombatSpatialExecutionHandler(
            agents,
            combatRepository,
            new InMemoryFactionRepository(factions),
            new TunnelNavigationVolume(3, 1, 1, cells, Array.Empty<CellId>(), cells),
            new FixedEquipment(weaponId),
            events,
            new AgentSkillGrantService(agents, events),
            new CombatSpatialPolicy(
                8,
                3,
                1,
                1,
                1,
                2,
                new CombatTacticalPolicy(0, 10_000, 0)));

        CombatAttackResolution? attack = null;
        for (long tick = 1; tick <= 20 && attack == null; tick++)
        {
            Result<CombatSpatialExecutionReport> advanced = handler.Handle(
                new AdvanceCombatSpatialExecutionCommand(attackerId, 321UL, tick));
            Assert.That(advanced.IsSuccess, Is.True, advanced.Error?.ToString());
            attack = advanced.Value.Attack;
        }

        Assert.That(attack, Is.Not.Null);
        Assert.That(agents.Get(attackerId)!.Position, Is.EqualTo(new CellId(1, 0, 0)));
        Assert.That(agents.Get(targetId)!.CreateSnapshot(20).Needs.Health.Points,
            Is.EqualTo(9_000));
        Assert.That(events.Events.OfType<CombatAttackResolved>().Count(), Is.EqualTo(1));
        Assert.That(combatRepository.Get().GetActiveIntent(attackerId), Is.Not.Null);
        Assert.That(combatRepository.Get().GetActiveExecution(attackerId)!.Stage,
            Is.EqualTo(CombatExecutionStage.Recover));
    }

    private static AgentState Agent(EntityId id, CellId cell) => new AgentState(
        id,
        "PlayMode combatant",
        new AgentNeedsSnapshot(
            new NeedValue(8_000),
            new NeedValue(8_000),
            new NeedValue(8_000),
            new NeedValue(10_000)),
        DailySchedule.CreateBalanced(24),
        skills: null,
        traits: null,
        initialPosition: cell);

    private sealed class FixedEquipment : ICombatEquipmentProvider
    {
        private readonly WeaponProfileId _weapon;
        public FixedEquipment(WeaponProfileId weapon) { _weapon = weapon; }
        public Result<CombatEquipmentSelection> Select(EntityId actorId, EntityId targetId) =>
            Result<CombatEquipmentSelection>.Success(new CombatEquipmentSelection(
                _weapon,
                new CombatantModifiers(0, 0, 0, 0, 0),
                new CombatantModifiers(0, 0, 0, 0, 0)));
    }

    private sealed class RecordingSink : IEventSink
    {
        private readonly List<IDomainEvent> _events = new List<IDomainEvent>();
        public IReadOnlyList<IDomainEvent> Events => _events;
        public void Append(IReadOnlyCollection<IDomainEvent> events) => _events.AddRange(events);
    }
}
}
