using System;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class CombatDomainTests
{
    private static readonly EntityId AttackerId = Id("91000000000000000000000000000001");
    private static readonly EntityId TargetId = Id("92000000000000000000000000000002");
    private static readonly FactionId PlayerFaction = new FactionId("faction.player");
    private static readonly FactionId RaiderFaction = new FactionId("faction.raiders");
    private static readonly WeaponProfileId SwordId = new WeaponProfileId("weapon.sword");

    [Fact]
    public void Same_action_is_resolved_once_and_replay_returns_cached_result()
    {
        CombatState combat = CreateCombat();
        FactionState factions = CreateHostileFactions();
        CombatAttackRequest request = Request("attack-1", tick: 10);

        Result<CombatAttackResolution> first = combat.ResolveAttack(
            request,
            Combatant(AttackerId, PlayerFaction, new CellId(0, 0)),
            Combatant(TargetId, RaiderFaction, new CellId(1, 0), armor: 100),
            factions);
        Result<CombatAttackResolution> replay = combat.ResolveAttack(
            request,
            Combatant(AttackerId, PlayerFaction, new CellId(0, 0)),
            Combatant(TargetId, RaiderFaction, new CellId(1, 0), armor: 100),
            factions);

        Assert.True(first.IsSuccess);
        Assert.True(replay.IsSuccess);
        Assert.False(first.Value.WasAlreadyProcessed);
        Assert.True(replay.Value.WasAlreadyProcessed);
        Assert.Equal(first.Value.Outcome, replay.Value.Outcome);
        Assert.Equal(first.Value.Damage, replay.Value.Damage);
        Assert.Equal(first.Value.HitRoll, replay.Value.HitRoll);
        Assert.Single(combat.PeekUncommittedEvents().OfType<CombatAttackResolved>());
        Assert.Equal(1, combat.CreateSnapshot().ProcessedActionCount);
    }

    [Fact]
    public void Same_seed_and_action_produce_same_resolution_in_fresh_states()
    {
        FactionState factions = CreateHostileFactions();
        CombatAttackRequest request = Request("attack-stable", tick: 10);
        CombatState firstCombat = CreateCombat();
        CombatState secondCombat = CreateCombat();

        CombatAttackResolution first = firstCombat.ResolveAttack(
            request,
            Combatant(AttackerId, PlayerFaction, new CellId(0, 0)),
            Combatant(TargetId, RaiderFaction, new CellId(1, 0)),
            factions).Value;
        CombatAttackResolution second = secondCombat.ResolveAttack(
            request,
            Combatant(AttackerId, PlayerFaction, new CellId(0, 0)),
            Combatant(TargetId, RaiderFaction, new CellId(1, 0)),
            factions).Value;

        Assert.Equal(first.HitRoll, second.HitRoll);
        Assert.Equal(first.BlockRoll, second.BlockRoll);
        Assert.Equal(first.Outcome, second.Outcome);
        Assert.Equal(first.Damage, second.Damage);
    }

    [Fact]
    public void Attack_requires_hostility_range_and_completed_cooldown()
    {
        CombatState combat = CreateCombat();
        FactionState hostile = CreateHostileFactions();
        CombatantSnapshot attacker = Combatant(
            AttackerId,
            PlayerFaction,
            new CellId(0, 0));
        CombatantSnapshot closeTarget = Combatant(
            TargetId,
            RaiderFaction,
            new CellId(1, 0));

        Assert.True(combat.ResolveAttack(
            Request("attack-first", tick: 10), attacker, closeTarget, hostile).IsSuccess);
        Assert.Equal(CombatErrors.AttackOnCooldown, combat.ResolveAttack(
            Request("attack-cooldown", tick: 11), attacker, closeTarget, hostile).Error);
        Assert.True(combat.ResolveAttack(
            Request("attack-ready", tick: 12), attacker, closeTarget, hostile).IsSuccess);

        CombatState ranged = CreateCombat();
        CombatantSnapshot farTarget = Combatant(
            TargetId,
            RaiderFaction,
            new CellId(9, 0));
        Assert.Equal(CombatErrors.TargetOutOfRange, ranged.ResolveAttack(
            Request("attack-far", tick: 10), attacker, farTarget, hostile).Error);

        FactionState neutral = CreateNeutralFactions();
        Assert.Equal(CombatErrors.TargetNotHostile, ranged.ResolveAttack(
            Request("attack-neutral", tick: 10), attacker, closeTarget, neutral).Error);
    }

    [Fact]
    public void Guaranteed_status_ticks_deterministically_and_expires()
    {
        CombatStatusDefinition bleeding = new CombatStatusDefinition(
            new CombatStatusId("status.bleeding"),
            applicationChance: 10_000,
            durationTicks: 2,
            damagePerTick: 50);
        CombatState combat = CreateCombat(bleeding);
        CombatAttackResolution attack = combat.ResolveAttack(
            Request("attack-status", tick: 10),
            Combatant(AttackerId, PlayerFaction, new CellId(0, 0)),
            Combatant(TargetId, RaiderFaction, new CellId(1, 0)),
            CreateHostileFactions()).Value;

        var firstTick = combat.AdvanceStatuses(11);
        var finalTick = combat.AdvanceStatuses(12);
        var replayTick = combat.AdvanceStatuses(12);

        Assert.Equal(new CombatStatusId("status.bleeding"), attack.AppliedStatusId);
        Assert.Single(firstTick);
        Assert.Equal(50, firstTick[0].Damage);
        Assert.False(firstTick[0].Expired);
        Assert.Single(finalTick);
        Assert.True(finalTick[0].Expired);
        Assert.Empty(replayTick);
        Assert.Empty(combat.CreateSnapshot().Statuses);
    }

    [Fact]
    public void Threat_detection_and_tactics_explain_retreat_and_attack()
    {
        FactionState factions = CreateHostileFactions();
        var threats = CombatThreatDetector.FindHostileThreats(
            PlayerFaction,
            new CellId(0, 0),
            sightRange: 5,
            new[]
            {
                new ThreatCandidate(TargetId, RaiderFaction, new CellId(2, 0), 9_000, true),
                new ThreatCandidate(Id("93000000000000000000000000000003"), RaiderFaction, new CellId(7, 0), 20_000, true),
            },
            factions);
        CombatTacticalPolicy policy = new CombatTacticalPolicy(
            retreatHealthThreshold: 2_000,
            retreatThreatRatio: 1_500,
            defendDistance: 0);

        CombatTacticalDecision retreat = CombatTacticalEvaluator.Evaluate(
            policy,
            health: 8_000,
            ownStrength: 4_000,
            threatStrength: threats[0].CombatStrength,
            distance: threats[0].Distance,
            weaponMaximumRange: 1);
        CombatTacticalDecision attack = CombatTacticalEvaluator.Evaluate(
            policy,
            health: 8_000,
            ownStrength: 12_000,
            threatStrength: 4_000,
            distance: 1,
            weaponMaximumRange: 1);

        Assert.Single(threats);
        Assert.Equal(TargetId, threats[0].EntityId);
        Assert.Equal(CombatIntentKind.Retreat, retreat.Intent);
        Assert.Equal("threat_overwhelming", retreat.ReasonCode);
        Assert.Equal(CombatIntentKind.Attack, attack.Intent);
        Assert.Equal("hostile_target_in_range", attack.ReasonCode);
    }

    private static CombatState CreateCombat(CombatStatusDefinition? status = null)
    {
        return new CombatState(new WeaponCatalog(new[]
        {
            new WeaponProfile(
                SwordId,
                minimumRange: 1,
                maximumRange: 1,
                accuracy: 10_000,
                baseDamage: 1_000,
                armorPenetration: 0,
                cooldownTicks: 2,
                status),
        }));
    }

    private static CombatAttackRequest Request(string actionId, long tick)
    {
        return new CombatAttackRequest(
            new CombatActionId(actionId),
            AttackerId,
            TargetId,
            SwordId,
            worldSeed: 42UL,
            tick);
    }

    private static CombatantSnapshot Combatant(
        EntityId id,
        FactionId faction,
        CellId position,
        int armor = 0)
    {
        return new CombatantSnapshot(
            id,
            faction,
            position,
            isAlive: true,
            new NeedValue(10_000),
            accuracyModifier: 0,
            evasion: 0,
            armor,
            blockChance: 0,
            blockValue: 0);
    }

    private static FactionState CreateHostileFactions()
    {
        return CreateFactions(initialScore: -10_000);
    }

    private static FactionState CreateNeutralFactions()
    {
        return CreateFactions(initialScore: 0);
    }

    private static FactionState CreateFactions(int initialScore)
    {
        return new FactionState(
            new FactionCatalog(new[]
            {
                new FactionDefinition(PlayerFaction, "Player", initialScore),
                new FactionDefinition(RaiderFaction, "Raiders", initialScore),
            }),
            new FactionDiplomacyPolicy(
                hostileThreshold: -5_000,
                friendlyThreshold: 3_000,
                alliedThreshold: 8_000,
                territoryViolationPenalty: 1_000));
    }

    private static EntityId Id(string value)
    {
        return EntityId.Parse(value);
    }
}
}
