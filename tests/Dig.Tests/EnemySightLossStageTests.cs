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

public sealed class EnemySightLossStageTests
{
    private static readonly EntityId EnemyActor =
        EntityId.Parse("d1000000000000000000000000000001");
    private static readonly EntityId ResidentTarget =
        EntityId.Parse("d2000000000000000000000000000002");
    private static readonly FactionId EnemyFaction = new FactionId("faction.enemy");
    private static readonly FactionId ResidentFaction = new FactionId("faction.resident");
    private static readonly WeaponProfileId WeaponId =
        new WeaponProfileId("weapon.enemy.sight.loss");

    [Fact]
    public void Autonomous_approach_does_not_take_another_step_after_sight_loss()
    {
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        AgentState enemy = CreateAgent(EnemyActor, new CellId(0, 0, 0));
        AgentState resident = CreateAgent(ResidentTarget, new CellId(2, 0, 0));
        Assert.True(agents.Add(enemy).IsSuccess);
        Assert.True(agents.Add(resident).IsSuccess);

        FactionState factions = new FactionState(
            new FactionCatalog(new[]
            {
                new FactionDefinition(EnemyFaction, "Enemy", -10_000),
                new FactionDefinition(ResidentFaction, "Resident", -10_000),
            }),
            new FactionDiplomacyPolicy(-5_000, 3_000, 8_000, 1_000));
        factions.AssignMember(EnemyActor, EnemyFaction);
        factions.AssignMember(ResidentTarget, ResidentFaction);

        WeaponProfile weapon = new WeaponProfile(
            WeaponId,
            1,
            1,
            10_000,
            1_000,
            0,
            1,
            skillProfile: new CombatSkillProfile(
                AgentSkillCatalog.OneHandedCombat,
                1),
            spatialMode: CombatAttackSpatialMode.Melee);
        CombatState combat = new CombatState(new WeaponCatalog(new[] { weapon }));
        CombatIntentId intentId = new CombatIntentId("intent.enemy.sight.loss.stage");
        Assert.True(combat.IssueIntent(new CombatIntentRequest(
            intentId,
            EnemyActor,
            CombatIntentKind.Attack,
            CombatIntentSource.Autonomous,
            createdTick: 0,
            expiresTick: long.MaxValue,
            targetEntityId: ResidentTarget,
            targetCell: resident.Position)).IsSuccess);
        Result<CombatExecutionSnapshot> started = combat.StartExecution(
            new CombatExecutionRequest(
                new CombatExecutionId("execution.enemy.sight.loss.stage"),
                intentId,
                EnemyActor,
                CombatIntentSource.Autonomous,
                CombatExecutionStage.Approach,
                tick: 1));
        Assert.True(started.IsSuccess);
        Assert.True(combat.SetExecutionTarget(
            started.Value.ExecutionId,
            ResidentTarget,
            resident.Position,
            tick: 1,
            reason: "target_previously_visible").IsSuccess);
        Assert.True(combat.SetExecutionEngagement(
            started.Value.ExecutionId,
            new CellId(1, 0, 0),
            tick: 1,
            reason: "approach_required").IsSuccess);

        InMemoryCombatRepository combatRepository = new InMemoryCombatRepository(combat);
        RecordingEvents events = new RecordingEvents();
        CellId[] cells = Enumerable.Range(0, 6)
            .Select(x => new CellId(x, 0, 0))
            .ToArray();
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            6,
            1,
            1,
            cells,
            Array.Empty<CellId>(),
            cells);
        CombatSpatialExecutionHandler handler = new CombatSpatialExecutionHandler(
            agents,
            combatRepository,
            new InMemoryFactionRepository(factions),
            volume,
            new FixedEquipmentProvider(),
            events,
            new AgentSkillGrantService(agents, events),
            new CombatSpatialPolicy(
                2,
                4,
                0,
                1,
                1,
                2,
                new CombatTacticalPolicy(0, 10_000, 0)));

        Assert.True(resident.MoveTo(new CellId(5, 0, 0), tick: 1).IsSuccess);
        agents.Save(resident);

        Result<CombatSpatialExecutionReport> advanced = handler.Handle(
            new AdvanceCombatSpatialExecutionCommand(EnemyActor, 1UL, tick: 2));

        Assert.True(advanced.IsSuccess, advanced.Error?.ToString());
        Assert.Equal("enemy_target_out_of_sight", advanced.Value.ReasonCode);
        Assert.Equal(CombatExecutionStage.Completed, advanced.Value.Execution.Stage);
        Assert.Null(combatRepository.Get().GetActiveIntent(EnemyActor));
        Assert.Equal(new CellId(0, 0, 0), agents.Get(EnemyActor)!.Position);
    }

    private static AgentState CreateAgent(EntityId id, CellId position)
    {
        return new AgentState(
            id,
            "Sight loss combatant",
            AgentTestFactory.CreateNeeds(8_000, 8_000, 8_000, 10_000),
            AgentTestFactory.CreateWorkSchedule(),
            null,
            null,
            position);
    }

    private sealed class FixedEquipmentProvider : ICombatEquipmentProvider
    {
        public Result<CombatEquipmentSelection> Select(
            EntityId actorId,
            EntityId targetId)
        {
            return Result<CombatEquipmentSelection>.Success(
                new CombatEquipmentSelection(
                    WeaponId,
                    new CombatantModifiers(0, 0, 0, 0, 0),
                    new CombatantModifiers(0, 0, 0, 0, 0)));
        }
    }

    private sealed class RecordingEvents : IEventSink
    {
        private readonly List<IDomainEvent> _events = new List<IDomainEvent>();

        public void Append(IReadOnlyCollection<IDomainEvent> events)
        {
            _events.AddRange(events);
        }
    }
}

}