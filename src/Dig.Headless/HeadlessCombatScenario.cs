using System;
using Dig.Application.Agents;
using Dig.Application.Combat;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.Strategy;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Headless
{

internal static class HeadlessCombatScenario
{
    public static int Run(InMemoryExecutionJournal journal, long tick)
    {
        if (journal is null)
        {
            throw new ArgumentNullException(nameof(journal));
        }

        EntityId attackerId = EntityId.Parse("c1000000000000000000000000000001");
        EntityId targetId = EntityId.Parse("c2000000000000000000000000000002");
        FactionId defenders = new FactionId("faction.headless.defenders");
        FactionId raiders = new FactionId("faction.headless.raiders");
        WeaponProfileId weaponId = new WeaponProfileId("weapon.headless.spear");
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        Require(agents.Add(CreateAgent(
            attackerId,
            health: 10_000,
            new CellId(0, 0, 0))));
        Require(agents.Add(CreateAgent(
            targetId,
            health: 5_000,
            new CellId(1, 0, 0))));

        FactionState factions = new FactionState(
            new FactionCatalog(new[]
            {
                new FactionDefinition(defenders, "Defenders", -10_000),
                new FactionDefinition(raiders, "Raiders", -10_000),
            }),
            new FactionDiplomacyPolicy(-5_000, 3_000, 8_000, 1_000));
        Require(factions.AssignMember(attackerId, defenders));
        Require(factions.AssignMember(targetId, raiders));
        CombatState combat = new CombatState(new WeaponCatalog(new[]
        {
            new WeaponProfile(
                weaponId,
                minimumRange: 1,
                maximumRange: 1,
                accuracy: 10_000,
                baseDamage: 1_000,
                armorPenetration: 0,
                cooldownTicks: 1),
        }));
        ResolveCombatAttackHandler handler = new ResolveCombatAttackHandler(
            agents,
            new InMemoryCombatRepository(combat),
            new InMemoryFactionRepository(factions),
            journal,
            new AgentSkillGrantService(agents, journal));
        CombatantModifiers none = new CombatantModifiers(0, 0, 0, 0, 0);
        ResolveCombatAttackCommand command = new ResolveCombatAttackCommand(
            new CombatActionId("headless-attack-1"),
            attackerId,
            targetId,
            weaponId,
            worldSeed: 42UL,
            tick,
            none,
            none);

        CombatAttackResolution first = Require(handler.Handle(command));
        int healthAfterFirst = agents.Get(targetId)!
            .CreateSnapshot(tick)
            .Needs.Health.Points;
        CombatAttackResolution replay = Require(handler.Handle(command));
        int healthAfterReplay = agents.Get(targetId)!
            .CreateSnapshot(tick)
            .Needs.Health.Points;
        if (first.Damage != 1_000
            || healthAfterFirst != 4_000
            || healthAfterReplay != healthAfterFirst
            || !replay.WasAlreadyProcessed)
        {
            throw new InvalidOperationException(
                "Headless combat did not preserve deterministic action idempotency.");
        }

        StrategicAiState strategy = new StrategicAiState(new StrategicAiPolicy(
            planningIntervalTicks: 10,
            minimumResourceReserve: 20,
            minimumFreeHousing: 2,
            attackAdvantageRatio: 1_500,
            retreatThreatRatio: 1_500));
        StrategicDecisionReport decision = strategy.Evaluate(new StrategicAiContext(
            tick,
            defenders,
            resourceReserve: 100,
            freeHousing: 5,
            ownStrength: 4_000,
            detectedThreatStrength: 9_000,
            hostileTargetStrength: 0,
            hostileTargetFactionId: null,
            canExpandTerritory: false));
        if (decision.CurrentGoal != StrategicGoalKind.Retreat)
        {
            throw new InvalidOperationException(
                "Headless strategic AI did not retreat from an overwhelming threat.");
        }

        return first.Damage;
    }

    private static AgentState CreateAgent(EntityId id, int health, CellId position)
    {
        return new AgentState(
            id,
            "Headless Combatant",
            new AgentNeedsSnapshot(
                new NeedValue(8_000),
                new NeedValue(8_000),
                new NeedValue(8_000),
                new NeedValue(health)),
            DailySchedule.CreateBalanced(ticksPerDay: 12),
            skills: null,
            traits: null,
            position);
    }

    private static T Require<T>(Result<T> result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.ToString());
        }

        return result.Value;
    }

    private static void Require(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.ToString());
        }
    }
}
}
