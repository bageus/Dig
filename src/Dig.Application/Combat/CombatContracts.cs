using System;
using Dig.Application.Messaging;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.Factions;

namespace Dig.Application.Combat
{

public interface ICombatRepository
{
    CombatState Get();

    void Save(CombatState combat);
}

public interface IFactionRepository
{
    FactionState Get();

    void Save(FactionState factions);
}

public static class CombatApplicationErrors
{
    public static readonly DomainError AgentNotFound = new DomainError(
        "combat.application.agent_not_found",
        "The attacker or target agent is not registered.");

    public static readonly DomainError FactionMembershipMissing = new DomainError(
        "combat.application.faction_missing",
        "The attacker or target has no faction membership.");

    public static readonly DomainError DamageRejected = new DomainError(
        "combat.application.damage_rejected",
        "The resolved combat damage could not be applied to the target agent.");

    public static readonly DomainError HealingJobInvalid = new DomainError(
        "combat.application.healing_job_invalid",
        "The job is not a completed healing job for the requested patient.");
}

public readonly struct CombatantModifiers
{
    public CombatantModifiers(
        int accuracyModifier,
        int evasion,
        int armor,
        int blockChance,
        int blockValue)
    {
        AccuracyModifier = accuracyModifier;
        Evasion = evasion;
        Armor = armor;
        BlockChance = blockChance;
        BlockValue = blockValue;
    }

    public int AccuracyModifier { get; }
    public int Evasion { get; }
    public int Armor { get; }
    public int BlockChance { get; }
    public int BlockValue { get; }
}

public sealed class ResolveCombatAttackCommand
    : ICommand<Result<CombatAttackResolution>>
{
    public ResolveCombatAttackCommand(
        CombatActionId actionId,
        EntityId attackerId,
        EntityId targetId,
        WeaponProfileId weaponProfileId,
        ulong worldSeed,
        long tick,
        CombatantModifiers attackerModifiers,
        CombatantModifiers targetModifiers)
    {
        ActionId = actionId;
        AttackerId = attackerId;
        TargetId = targetId;
        WeaponProfileId = weaponProfileId;
        WorldSeed = worldSeed;
        Tick = tick;
        AttackerModifiers = attackerModifiers;
        TargetModifiers = targetModifiers;
    }

    public CombatActionId ActionId { get; }
    public EntityId AttackerId { get; }
    public EntityId TargetId { get; }
    public WeaponProfileId WeaponProfileId { get; }
    public ulong WorldSeed { get; }
    public long Tick { get; }
    public CombatantModifiers AttackerModifiers { get; }
    public CombatantModifiers TargetModifiers { get; }
}

public sealed class CompleteHealingJobCommand : ICommand<Result>
{
    public CompleteHealingJobCommand(EntityId jobId, EntityId patientId, long tick)
    {
        JobId = jobId;
        PatientId = patientId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId PatientId { get; }
    public long Tick { get; }
}
}
