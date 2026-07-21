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

    public static readonly DomainError CombatIntentInactive = new DomainError(
        "combat.application.intent_inactive",
        "The attack intent was cancelled, expired, replaced or does not match the attack.");

    public static readonly DomainError HealingJobInvalid = new DomainError(
        "combat.application.healing_job_invalid",
        "The job is not an active healing job for the requested patient.");
}

public readonly struct CombatantModifiers
{
    public CombatantModifiers(
        int accuracyModifier,
        int evasion,
        int armor,
        int blockChance,
        int blockValue,
        ShieldSkillProfile? shieldSkillProfile = null)
    {
        AccuracyModifier = accuracyModifier;
        Evasion = evasion;
        Armor = armor;
        BlockChance = blockChance;
        BlockValue = blockValue;
        ShieldSkillProfile = shieldSkillProfile;
    }

    public int AccuracyModifier { get; }
    public int Evasion { get; }
    public int Armor { get; }
    public int BlockChance { get; }
    public int BlockValue { get; }
    public ShieldSkillProfile? ShieldSkillProfile { get; }
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
        CombatantModifiers targetModifiers,
        CombatIntentId? sourceIntentId = null)
    {
        ActionId = actionId;
        AttackerId = attackerId;
        TargetId = targetId;
        WeaponProfileId = weaponProfileId;
        WorldSeed = worldSeed;
        Tick = tick;
        AttackerModifiers = attackerModifiers;
        TargetModifiers = targetModifiers;
        SourceIntentId = sourceIntentId;
    }

    public CombatActionId ActionId { get; }
    public EntityId AttackerId { get; }
    public EntityId TargetId { get; }
    public WeaponProfileId WeaponProfileId { get; }
    public ulong WorldSeed { get; }
    public long Tick { get; }
    public CombatantModifiers AttackerModifiers { get; }
    public CombatantModifiers TargetModifiers { get; }
    public CombatIntentId? SourceIntentId { get; }
}

public sealed class AdvanceCombatStatusesCommand
    : ICommand<Result<CombatStatusAdvanceReport>>
{
    public AdvanceCombatStatusesCommand(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        Tick = tick;
    }

    public long Tick { get; }
}

public sealed class CombatStatusAdvanceReport
{
    public CombatStatusAdvanceReport(
        long tick,
        int statusCount,
        int targetCount,
        int totalDamage)
    {
        Tick = tick;
        StatusCount = statusCount;
        TargetCount = targetCount;
        TotalDamage = totalDamage;
    }

    public long Tick { get; }
    public int StatusCount { get; }
    public int TargetCount { get; }
    public int TotalDamage { get; }
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
