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
    private static readonly EntityId Attacker = EntityId.Parse("c1000000000000000000000000000001");
    private static readonly EntityId Target = EntityId.Parse("c2000000000000000000000000000002");
    private static readonly EntityId Ally = EntityId.Parse("c3000000000000000000000000000003");
    private static readonly FactionId Player = new FactionId("faction.player");
    private static readonly FactionId Enemy = new FactionId("faction.enemy");
    private static readonly WeaponProfileId WeaponId = new WeaponProfileId("weapon.spatial.melee");

    [Fact]
    public void Direct_intent_approaches_and_resolves_exactly_one_attack()
    {
        Fixture fixture = CreateFixture();
        CombatIntentId intentId = new CombatIntentId("intent.player.attack");
        fixture.Combat.IssueIntent(new CombatIntentRequest(
            intentId, Attacker, CombatIntentKind.Attack, CombatIntentSource.PlayerOrder,
            createdTick: 0, expiresTick: 100, targetEntityId: Target));
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
        Assert.Equal(9_000, fixture.Agents.Get(Target)!
            .CreateSnapshot(20).Needs.Health.Points);
        Assert.NotNull(fixture.Combat.GetActiveIntent(Attacker));
        Assert.Equal(CombatExecutionStage.Recover,
            fixture.Combat.GetActiveExecution(Attacker)!.Stage);
        Assert.Single(fixture.Events.Events.OfType<CombatAttackResolved>());
        Assert.Single(fixture.Events.Events.OfType<CombatAlarmPublished>());
        Assert.Equal(CombatIntentSource.Alarm,
            fixture.Combat.GetActiveIntent(Ally)!.Source);
    }

    [Fact]
    public void Player_target_death_completes_intent_without_retarget()
    {
        Fixture fixture = CreateFixture(targetAlive: false);
        fixture.Combat.IssueIntent(new CombatIntentRequest(
            new CombatIntentId("intent.dead.target"), Attacker,
            CombatIntentKind.Attack, CombatIntentSource.PlayerOrder,
            0, 100, Target));
        fixture.CombatRepository.Save(fixture.Combat);

        fixture.Handler.Handle(new AdvanceCombatSpatialExecutionCommand(Attacker, 1UL, 1));
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
            openCells: new[] { new CellId(0, 0, 0), new CellId(2, 0, 0) });
        fixture.Combat.IssueIntent(new CombatIntentRequest(
            new CombatIntentId("intent.ranged.blocked"), Attacker,
            CombatIntentKind.Attack, CombatIntentSource.PlayerOrder,
            0, 100, Target));
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

    [Fact]
    public void Intent_expiry_cancels_active_execution_without_attack()
    {
        Fixture fixture = CreateFixture();
        fixture.Combat.IssueIntent(new CombatIntentRequest(
            new CombatIntentId("intent.expiring"), Attacker,
            CombatIntentKind.Attack, CombatIntentSource.PlayerOrder,
            0, 2, Target));
        fixture.CombatRepository.Save(fixture.Combat);

        Assert.True(fixture.Handler.Handle(
            new AdvanceCombatSpatialExecutionCommand(Attacker, 1UL, 1)).IsSuccess);
        Result<CombatSpatialExecutionReport> expired = fixture.Handler.Handle(
            new AdvanceCombatSpatialExecutionCommand(Attacker, 1UL, 2));

        Assert.True(expired.IsSuccess);
        Assert.Equal("intent_expired", expired.Value.ReasonCode);
        Assert.Equal(CombatExecutionStage.Cancelled, expired.Value.Execution.Stage);
        Assert.Null(fixture.Combat.GetActiveIntent(Attacker));
        Assert.Empty(fixture.Events.Events.OfType<CombatAttackResolved>());
    }

    [Fact]
    public void Sight_loss_pursues_last_known_cell_then_completes_player_intent()
    {
        CellId[] cells = Enumerable.Range(0, 6)
            .Select(x => new CellId(x, 0, 0)).ToArray();
        Fixture fixture = CreateFixture(openCells: cells, width: 6, height: 1, sightRange: 2);
        CombatIntentId intentId = new CombatIntentId("intent.last.known");
        fixture.Combat.IssueIntent(new CombatIntentRequest(
            intentId, Attacker, CombatIntentKind.Attack,
            CombatIntentSource.PlayerOrder, 0, 100, Target,
            new CellId(2, 0, 0)));
        Result<CombatExecutionSnapshot> started = fixture.Combat.StartExecution(
            new CombatExecutionRequest(
                new CombatExecutionId("execution.last.known"), intentId, Attacker,
                CombatIntentSource.PlayerOrder, CombatExecutionStage.Reevaluate, 1));
        Assert.True(started.IsSuccess);
        Assert.True(fixture.Combat.SetExecutionTarget(
            started.Value.ExecutionId, Target, new CellId(2, 0, 0), 1,
            "target_previously_visible").IsSuccess);
        fixture.CombatRepository.Save(fixture.Combat);
        AgentState target = fixture.Agents.Get(Target)!;
        Assert.True(target.MoveTo(new CellId(5, 0, 0), 1).IsSuccess);
        fixture.Agents.Save(target);

        CombatSpatialExecutionReport? report = null;
        for (long tick = 2; tick <= 8 && fixture.Combat.GetActiveIntent(Attacker) != null; tick++)
        {
            Result<CombatSpatialExecutionReport> advanced = fixture.Handler.Handle(
                new AdvanceCombatSpatialExecutionCommand(Attacker, 1UL, tick));
            Assert.True(advanced.IsSuccess, advanced.Error?.ToString());
            report = advanced.Value;
        }

        Assert.NotNull(report);
        Assert.Equal(new CellId(2, 0, 0), fixture.Agents.Get(Attacker)!.Position);
        Assert.Equal(CombatExecutionStage.Completed, report!.Execution.Stage);
        Assert.Equal("target_lost", report.ReasonCode);
        Assert.Null(fixture.Combat.GetActiveIntent(Attacker));
    }

    [Fact]
    public void Persistent_enemy_aggro_tracks_living_target_out_of_sight_without_retreat_or_expiry()
    {
        CellId[] cells = Enumerable.Range(0, 6)
            .Select(x => new CellId(x, 0, 0)).ToArray();
        Fixture fixture = CreateFixture(
            openCells: cells,
            width: 6,
            height: 1,
            sightRange: 2);
        CombatIntentId intentId = new CombatIntentId("intent.persistent.enemy");
        fixture.Combat.IssueIntent(new CombatIntentRequest(
            intentId,
            Attacker,
            CombatIntentKind.Attack,
            CombatIntentSource.Autonomous,
            createdTick: 0,
            expiresTick: long.MaxValue,
            targetEntityId: Target,
            targetCell: new CellId(2, 0, 0)));
        Result<CombatExecutionSnapshot> started = fixture.Combat.StartExecution(
            new CombatExecutionRequest(
                new CombatExecutionId("execution.persistent.enemy"),
                intentId,
                Attacker,
                CombatIntentSource.Autonomous,
                CombatExecutionStage.Reevaluate,
                tick: 1));
        Assert.True(started.IsSuccess);
        Assert.True(fixture.Combat.SetExecutionTarget(
            started.Value.ExecutionId,
            Target,
            new CellId(2, 0, 0),
            tick: 1,
            reason: "target_previously_visible").IsSuccess);
        fixture.CombatRepository.Save(fixture.Combat);
        AgentState target = fixture.Agents.Get(Target)!;
        Assert.True(target.MoveTo(new CellId(5, 0, 0), tick: 1).IsSuccess);
        fixture.Agents.Save(target);

        Result<CombatSpatialExecutionReport> advanced = fixture.Handler.Handle(
            new AdvanceCombatSpatialExecutionCommand(Attacker, 1UL, tick: 2));

        Assert.True(advanced.IsSuccess, advanced.Error?.ToString());
        Assert.Equal("persistent_aggro_target_tracked", advanced.Value.ReasonCode);
        Assert.Equal(
            CombatExecutionStage.SelectEngagementCell,
            advanced.Value.Execution.Stage);
        CombatIntentSnapshot active = Assert.IsType<CombatIntentSnapshot>(
            fixture.Combat.GetActiveIntent(Attacker));
        Assert.True(active.IsPersistent);
        Assert.Equal(Target, active.TargetEntityId);
    }

    private static Fixture CreateFixture(
        bool targetAlive = true,
        CombatAttackSpatialMode weaponMode = CombatAttackSpatialMode.Melee,
        int minimumRange = 1,
        int maximumRange = 1,
        CellId[]? openCells = null,
        int width = 3,
        int height = 2,
        int sightRange = 8)
    {
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        AgentState attacker = CreateAgent(Attacker, new CellId(0, 0, 0), 10_000);
        AgentState target = CreateAgent(Target, new CellId(2, 0, 0), 10_000);
        if (!targetAlive)
            Assert.True(target.ApplyExternalNeedDelta(
                new NeedDelta(0, 0, 0, -10_000), "test-death", 0).IsSuccess);
        AgentState ally = CreateAgent(Ally, new CellId(0, Math.Min(1, height - 1), 0), 10_000);
        Assert.True(agents.Add(attacker).IsSuccess);
        Assert.True(agents.Add(target).IsSuccess);
        Assert.True(agents.Add(ally).IsSuccess);

        FactionState factions = new FactionState(
            new FactionCatalog(new[]
            {
                new FactionDefinition(Player, "Player", -10_000),
                new FactionDefinition(Enemy, "Enemy", -10_000),
            }), new FactionDiplomacyPolicy(-5_000, 3_000, 8_000, 1_000));
        factions.AssignMember(Attacker, Player);
        factions.AssignMember(Ally, Enemy);
        factions.AssignMember(Target, Enemy);

        WeaponProfile weapon = new WeaponProfile(
            WeaponId, minimumRange, maximumRange, 10_000, 1_000, 0, 1,
            skillProfile: new CombatSkillProfile(AgentSkillCatalog.OneHandedCombat, 1),
            spatialMode: weaponMode);
        CombatState combat = new CombatState(new WeaponCatalog(new[] { weapon }));
        InMemoryCombatRepository combatRepository = new InMemoryCombatRepository(combat);
        RecordingEvents events = new RecordingEvents();
        CellId[] cells = openCells ?? new[]
        {
            new CellId(0, 0, 0), new CellId(1, 0, 0), new CellId(2, 0, 0),
            new CellId(0, 1, 0), new CellId(1, 1, 0), new CellId(2, 1, 0),
        };
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width, height, 1, cells, Array.Empty<CellId>(), cells);
        CombatSpatialExecutionHandler handler = new CombatSpatialExecutionHandler(
            agents, combatRepository, new InMemoryFactionRepository(factions), volume,
            new FixedEquipmentProvider(WeaponId), events,
            new AgentSkillGrantService(agents, events),
            new CombatSpatialPolicy(sightRange, 4, 0, 1, 1, 2,
                new CombatTacticalPolicy(0, 10_000, 0)));
        return new Fixture(agents, combat, combatRepository, events, handler);
    }

    private static AgentState CreateAgent(EntityId id, CellId position, int health) =>
        new AgentState(id, "Spatial combatant",
            AgentTestFactory.CreateNeeds(8_000, 8_000, 8_000, health),
            AgentTestFactory.CreateWorkSchedule(), null, null, position);

    private sealed class FixedEquipmentProvider : ICombatEquipmentProvider
    {
        private readonly WeaponProfileId _weapon;
        public FixedEquipmentProvider(WeaponProfileId weapon) { _weapon = weapon; }
        public Result<CombatEquipmentSelection> Select(EntityId actorId, EntityId targetId) =>
            Result<CombatEquipmentSelection>.Success(new CombatEquipmentSelection(
                _weapon, new CombatantModifiers(0, 0, 0, 0, 0),
                new CombatantModifiers(0, 0, 0, 0, 0)));
    }

    private sealed class RecordingEvents : IEventSink
    {
        private readonly List<IDomainEvent> _events = new List<IDomainEvent>();
        public IReadOnlyList<IDomainEvent> Events => _events;
        public void Append(IReadOnlyCollection<IDomainEvent> events) => _events.AddRange(events);
    }

    private sealed class Fixture
    {
        public Fixture(InMemoryAgentRepository agents, CombatState combat,
            InMemoryCombatRepository combatRepository, RecordingEvents events,
            CombatSpatialExecutionHandler handler)
        {
            Agents = agents; Combat = combat; CombatRepository = combatRepository;
            Events = events; Handler = handler;
        }
        public InMemoryAgentRepository Agents { get; }
        public CombatState Combat { get; }
        public InMemoryCombatRepository CombatRepository { get; }
        public RecordingEvents Events { get; }
        public CombatSpatialExecutionHandler Handler { get; }
    }
}
}
