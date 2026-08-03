using System;
using System.Collections.Generic;
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

public sealed class CombatMovementCadenceTests
{
    private static readonly EntityId Actor = EntityId.Parse(
        "cd000000000000000000000000000001");
    private static readonly EntityId Target = EntityId.Parse(
        "cd000000000000000000000000000002");
    private static readonly FactionId Residents = new FactionId("faction.residents");
    private static readonly FactionId Hostiles = new FactionId("faction.hostiles");
    private static readonly WeaponProfileId Weapon = new WeaponProfileId(
        "combat.test.cadence");

    [Fact]
    public void Straight_combat_approach_commits_five_validated_cells_in_four_ticks()
    {
        CellId[] cells = new CellId[7];
        for (int x = 0; x < cells.Length; x++)
        {
            cells[x] = new CellId(x, 0, 0);
        }

        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        AgentState actor = CreateAgent(Actor, cells[0]);
        AgentState target = CreateAgent(Target, cells[6]);
        Assert.True(agents.Add(actor).IsSuccess);
        Assert.True(agents.Add(target).IsSuccess);

        FactionState factions = new FactionState(
            new FactionCatalog(new[]
            {
                new FactionDefinition(Residents, "Residents", -10_000),
                new FactionDefinition(Hostiles, "Hostiles", -10_000),
            }),
            new FactionDiplomacyPolicy(-5_000, 3_000, 8_000, 1_000));
        factions.AssignMember(Actor, Residents);
        factions.AssignMember(Target, Hostiles);

        CombatState combat = new CombatState(new WeaponCatalog(new[]
        {
            new WeaponProfile(
                Weapon,
                minimumRange: 1,
                maximumRange: 1,
                accuracy: 10_000,
                baseDamage: 1_000,
                armorPenetration: 0,
                cooldownTicks: 4),
        }));
        CombatIntentId intentId = new CombatIntentId("intent.cadence");
        combat.IssueIntent(new CombatIntentRequest(
            intentId,
            Actor,
            CombatIntentKind.Attack,
            CombatIntentSource.PlayerOrder,
            createdTick: 0,
            expiresTick: 100,
            targetEntityId: Target,
            targetCell: cells[6]));
        Result<CombatExecutionSnapshot> started = combat.StartExecution(
            new CombatExecutionRequest(
                new CombatExecutionId("execution.cadence"),
                intentId,
                Actor,
                CombatIntentSource.PlayerOrder,
                CombatExecutionStage.Approach,
                tick: 0));
        Assert.True(started.IsSuccess);
        Assert.True(combat.SetExecutionTarget(
            started.Value.ExecutionId,
            Target,
            cells[6],
            tick: 0,
            reasonCode: "target_set").IsSuccess);
        Assert.True(combat.SetExecutionEquipment(
            started.Value.ExecutionId,
            Weapon,
            tick: 0,
            reasonCode: "equipment_set").IsSuccess);
        Assert.True(combat.SetExecutionEngagement(
            started.Value.ExecutionId,
            cells[5],
            tick: 0,
            reasonCode: "engagement_set").IsSuccess);

        InMemoryCombatRepository combatRepository =
            new InMemoryCombatRepository(combat);
        CombatSpatialExecutionHandler handler = new CombatSpatialExecutionHandler(
            agents,
            combatRepository,
            new InMemoryFactionRepository(factions),
            new TunnelNavigationVolume(
                width: 7,
                height: 1,
                depth: 1,
                openCells: cells,
                verticalTunnelCells: Array.Empty<CellId>(),
                supportedCells: cells),
            new FixedEquipmentProvider(),
            new RecordingEvents(),
            new AgentSkillGrantService(agents, new RecordingEvents()),
            new CombatSpatialPolicy(
                sightRange: 8,
                alarmRadius: 4,
                windUpTicks: 1,
                recoveryTicks: 3,
                retryDelayTicks: 1,
                maximumRetries: 2,
                new CombatTacticalPolicy(0, 10_000, 0)));

        for (long tick = 1; tick <= 4; tick++)
        {
            Result<CombatSpatialExecutionReport> advanced = handler.Handle(
                new AdvanceCombatSpatialExecutionCommand(Actor, 1UL, tick));
            Assert.True(advanced.IsSuccess, advanced.Error?.ToString());
        }

        Assert.Equal(cells[5], agents.Get(Actor)!.Position);
        Assert.Equal(
            CombatExecutionStage.FaceTarget,
            combatRepository.Get().GetActiveExecution(Actor)!.Stage);
    }

    private static AgentState CreateAgent(EntityId id, CellId position)
    {
        return new AgentState(
            id,
            "Combat cadence",
            AgentTestFactory.CreateNeeds(10_000, 10_000, 10_000, 10_000),
            AgentTestFactory.CreateWorkSchedule(),
            initialPosition: position);
    }

    private sealed class FixedEquipmentProvider : ICombatEquipmentProvider
    {
        public Result<CombatEquipmentSelection> Select(EntityId actorId, EntityId targetId)
        {
            return Result<CombatEquipmentSelection>.Success(
                new CombatEquipmentSelection(
                    Weapon,
                    new CombatantModifiers(0, 0, 0, 0, 0),
                    new CombatantModifiers(0, 0, 0, 0, 0)));
        }
    }

    private sealed class RecordingEvents : IEventSink
    {
        public void Append(IReadOnlyCollection<IDomainEvent> events)
        {
        }
    }
}

}
