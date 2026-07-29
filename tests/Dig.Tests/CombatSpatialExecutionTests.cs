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
using Xunit;

namespace Dig.Tests
{

public sealed class CombatSpatialExecutionTests
{
    private static readonly EntityId Attacker =
        EntityId.Parse("c1000000000000000000000000000001");
    private static readonly EntityId Target =
        EntityId.Parse("c2000000000000000000000000000002");
    private static readonly EntityId Ally =
        EntityId.Parse("c3000000000000000000000000000003");
    private static readonly FactionId Player = new FactionId("faction.player");
    private static readonly FactionId Enemy = new FactionId("faction.enemy");
    private static readonly WeaponProfileId WeaponId =
        new WeaponProfileId("weapon.spatial.melee");

    [Fact]
    public void Direct_intent_approaches_and_resolves_exactly_one_attack()
    {
        Fixture fixture = CreateFixture();
        CombatIntentId intentId = new CombatIntentId("intent.player.attack");
        fixture.Combat.IssueIntent(new CombatIntentRequest(
            intentId,
            Attacker,
            CombatIntentKind.Attack,
            CombatIntentSource.PlayerOrder,
            createdTick: 0,
            expiresTick: 100,
            targetEntityId: Target));
        fixture.CombatRepository.Save(fixture.Combat);

        CombatAttackResolution? attack = null;
        for (long tick = 1; tick <= 20 && attack is null; tick++)
        {
            Result<CombatSpatialExecutionReport> result = fixture.Handler.Handle(
                new AdvanceCombatSpatialExecutionCommand(Attacker, 77UL, tick));
            Assert.True(result.IsSuccess, result.Error?.ToString());
            attack = result.Value.Attack;
        }

        Assert.NotNull(attack);
        Assert.Equal(new CellId(1, 0, 0), fixture.Agents.Get(Attacker)!.Position);
        Assert.Equal(
            9_000,
            fixture.Agents.Get(Target)!.CreateSnapshot(20).Needs.Health.Points);
        Assert.NotNull(fixture.Combat.GetActiveIntent(Attacker));
        Assert.Equal(
            CombatExecutionStage.Recover,
            fixture.Combat.GetActiveExecution(Attacker)!.Stage);
        Assert.Single(fixture.Events.Events.OfType<CombatAttackResolved>());
        Assert.Single(fixture.Events.Events.OfType<CombatAlarmPublished>());
        Assert.Equal(
            CombatIntentSource.Alarm,
            fixture.Combat.GetActiveIntent(Ally)!.Source);
    }

    [Fact]
    public void Player_target_death_completes_intent_without_retarget()
    {
        Fixture fixture = CreateFixture(targetAlive: false);
        CombatIntentId intentId = new CombatIntentId("intent.dead.target");
        fixture.Combat.IssueIntent(new CombatIntentRequest(
            intentId,
            Attacker,
            CombatIntentKind.Attack,
            CombatIntentSource.PlayerOrder,
            0,
            100,
            Target));
        fixture.CombatRepository.Save(fixture.Combat);

        fixture.Handler.Handle(
            new AdvanceCombatSpatialExecutionCommand(Attacker, 1UL, 1));
        Result<CombatSpatialExecutionReport> result = fixture.Handler.Handle(
            new AdvanceCombatSpatialExecutionCommand(Attacker, 1UL, 2));

        Assert.True(result.IsSuccess);
        Assert.Null(fixture.Combat.GetActiveIntent(Attacker));
        Assert.Equal(CombatExecutionStage.Completed, result.Value.Execution.Stage);
    }

    [Fact]
    public void Ranged_world_terrain_blocks_attack_engagement()
    {
        Fixture fixture = CreateFixture(
            weaponMode: CombatAttackSpatialMode.Ranged,
            minimumRange: 2,
            maximumRange: 4,
            openCells: new[]
            {
                new CellId(0, 0, 0),
                new CellId(2, 0, 0),
            });
        CombatIntentId intentId = new CombatIntentId("intent.ranged.blocked");
        fixture.Combat.IssueIntent(new CombatIntentRequest(
            intentId,
            Attacker,
            CombatIntentKind.Attack,
            CombatIntentSource.PlayerOrder,
            0,
            100,
            Target));
        fixture.CombatRepository.Save(fixture.Combat);

        CombatSpatialExecutionReport? report = null;
        for (long tick = 1; tick <= 8; tick++)
        {
            Result<CombatSpatialExecutionReport> result = fixture.Handler.Handle(
                new AdvanceCombatSpatialExecutionCommand(Attacker, 7UL, tick));
            Assert.True(result.IsSuccess);
            report = result.Value;
        }

        Assert.NotNull(report);
        Assert.Null(report!.Attack);
        Assert.Empty(fixture.Events.Events.OfType<CombatAttackResolved>());
    }

    private static Fixture CreateFixture(
        bool targetAlive = true,
        CombatAttackSpatialMode weaponMode = CombatAttackSpatialMode.Melee,
        int minimumRange = 1,
        int maximumRange = 1,
        CellId[]? openCells = null)
    {
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        AgentState attacker = CreateAgent(Attacker, new CellId(0, 0, 0));
        AgentState target = CreateAgent(Target, new CellId(2, 0, 0));
        if (!targetAlive)
        {
            Assert.True(target.ApplyExternalNeedDelta(
                new NeedDelta(0, 0, 0, -10_000),
                "test-death",
                0).IsSuccess);
        }

        AgentState ally = CreateAgent(Ally, new CellId(0, 1, 0));
        Assert.True(agents.Add(attacker).IsSuccess);
        Assert.True(agents.Add(target).IsSuccess);
        Assert.True(agents.Add(ally).IsSuccess);

        FactionState factions = new FactionState(
            new FactionCatalog(new[]
            {
                new FactionDefinition(Player, "Player", -10_000),
                new FactionDefinition(Enemy, "Enemy", -10_000),
            }),
            new FactionDiplomacyPolicy(-5_000, 3_000, 8_000, 1_000));
        factions.AssignMember(Attacker, Player);
        factions.AssignMember(Ally, Player);
        factions.AssignMember(Target, Enemy);

        WeaponProfile weapon = new WeaponProfile(
            WeaponId,
            minimumRange,
            maximumRange,
            10_000,
            1_000,
            0,
            1,
            skillProfile: new CombatSkillProfile(
                AgentSkillCatalog.OneHandedCombat,
                1),
            spatialMode: weaponMode);
        CombatState combat = new CombatState(
            new WeaponCatalog(new[] { weapon }));
        InMemoryCombatRepository combatRepository =
            new InMemoryCombatRepository(combat);
        RecordingEvents events = new RecordingEvents();
        CellId[] cells = openCells ?? new[]
        {
            new CellId(0, 0, 0),
            new CellId(1, 0, 0),
            new CellId(2, 0, 0),
            new CellId(0, 1, 0),
            new CellId(1, 1, 0),
            new CellId(2, 1, 0),
        };
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            3,
            2,
            1,
            cells,
            Array.Empty<CellId>(),
            cells);
        CombatSpatialExecutionHandler handler = new CombatSpatialExecutionHandler(
            agents,
            combatRepository,
            new InMemoryFactionRepository(factions),
            volume,
            new FixedEquipmentProvider(WeaponId),
            events,
            new AgentSkillGrantService(agents, events),
            new CombatSpatialPolicy(
                8,
                4,
                0,
                1,
                1,
                2,
                new CombatTacticalPolicy(0, 10_000, 0)));
        return new Fixture(agents, combat, combatRepository, events, handler);
    }

    private static AgentState CreateAgent(EntityId id, CellId position) =>
        new AgentState(
            id,
            "Spatial combatant",
            AgentTestFactory.CreateNeeds(8_000, 8_000, 8_000, 10_000),
            AgentTestFactory.CreateWorkSchedule(),
            null,
            null,
            position);

    private sealed class FixedEquipmentProvider : ICombatEquipmentProvider
    {
        private readonly WeaponProfileId _weapon;

        public FixedEquipmentProvider(WeaponProfileId weapon)
        {
            _weapon = weapon;
        }

        public Result<CombatEquipmentSelection> Select(
            EntityId actorId,
            EntityId targetId) =>
            Result<CombatEquipmentSelection>.Success(
                new CombatEquipmentSelection(
                    _weapon,
                    new CombatantModifiers(0, 0, 0, 0, 0),
                    new CombatantModifiers(0, 0, 0, 0, 0)));
    }

    private sealed class RecordingEvents : IEventSink
    {
        private readonly List<IDomainEvent> _events = new List<IDomainEvent>();

        public IReadOnlyList<IDomainEvent> Events => _events;

        public void Append(IReadOnlyCollection<IDomainEvent> events)
        {
            _events.AddRange(events);
        }
    }

    private sealed class Fixture
    {
        public Fixture(
            InMemoryAgentRepository agents,
            CombatState combat,
            InMemoryCombatRepository combatRepository,
            RecordingEvents events,
            CombatSpatialExecutionHandler handler)
        {
            Agents = agents;
            Combat = combat;
            CombatRepository = combatRepository;
            Events = events;
            Handler = handler;
        }

        public InMemoryAgentRepository Agents { get; }
        public CombatState Combat { get; }
        public InMemoryCombatRepository CombatRepository { get; }
        public RecordingEvents Events { get; }
        public CombatSpatialExecutionHandler Handler { get; }
    }
}
}
