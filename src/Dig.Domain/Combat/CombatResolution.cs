using System;
using Dig.Domain.Core;

namespace Dig.Domain.Combat
{

public sealed class CombatAttackRequest
{
    public CombatAttackRequest(
        CombatActionId actionId,
        EntityId attackerId,
        EntityId targetId,
        WeaponProfileId weaponProfileId,
        ulong worldSeed,
        long tick)
    {
        if (actionId.IsEmpty)
        {
            throw new ArgumentException("Combat action id cannot be empty.", nameof(actionId));
        }

        if (attackerId.IsEmpty || targetId.IsEmpty || attackerId == targetId)
        {
            throw new ArgumentException("Attack requires two distinct combatants.");
        }

        if (weaponProfileId.IsEmpty)
        {
            throw new ArgumentException("Weapon profile id cannot be empty.", nameof(weaponProfileId));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        ActionId = actionId;
        AttackerId = attackerId;
        TargetId = targetId;
        WeaponProfileId = weaponProfileId;
        WorldSeed = worldSeed;
        Tick = tick;
    }

    public CombatActionId ActionId { get; }
    public EntityId AttackerId { get; }
    public EntityId TargetId { get; }
    public WeaponProfileId WeaponProfileId { get; }
    public ulong WorldSeed { get; }
    public long Tick { get; }
}

public sealed class CombatAttackResolution
{
    public CombatAttackResolution(
        CombatActionId actionId,
        EntityId attackerId,
        EntityId targetId,
        WeaponProfileId weaponProfileId,
        CombatAttackOutcome outcome,
        int distance,
        int hitChance,
        int hitRoll,
        int blockRoll,
        int damage,
        CombatStatusId? appliedStatusId,
        bool wasAlreadyProcessed)
    {
        if (distance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distance));
        }

        ValidateChance(hitChance, nameof(hitChance));
        ValidateChance(hitRoll, nameof(hitRoll));
        ValidateChance(blockRoll, nameof(blockRoll));
        if (damage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damage));
        }

        ActionId = actionId;
        AttackerId = attackerId;
        TargetId = targetId;
        WeaponProfileId = weaponProfileId;
        Outcome = outcome;
        Distance = distance;
        HitChance = hitChance;
        HitRoll = hitRoll;
        BlockRoll = blockRoll;
        Damage = damage;
        AppliedStatusId = appliedStatusId;
        WasAlreadyProcessed = wasAlreadyProcessed;
    }

    public CombatActionId ActionId { get; }
    public EntityId AttackerId { get; }
    public EntityId TargetId { get; }
    public WeaponProfileId WeaponProfileId { get; }
    public CombatAttackOutcome Outcome { get; }
    public int Distance { get; }
    public int HitChance { get; }
    public int HitRoll { get; }
    public int BlockRoll { get; }
    public int Damage { get; }
    public CombatStatusId? AppliedStatusId { get; }
    public bool WasAlreadyProcessed { get; }

    public CombatAttackResolution AsReplay()
    {
        return new CombatAttackResolution(
            ActionId,
            AttackerId,
            TargetId,
            WeaponProfileId,
            Outcome,
            Distance,
            HitChance,
            HitRoll,
            BlockRoll,
            Damage,
            AppliedStatusId,
            wasAlreadyProcessed: true);
    }

    private static void ValidateChance(int value, string parameterName)
    {
        if (value < 0 || value > 9_999)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

public readonly struct CombatStatusDamage
{
    public CombatStatusDamage(
        EntityId targetId,
        CombatStatusId statusId,
        int ticksApplied,
        int damage,
        bool expired)
    {
        if (targetId.IsEmpty)
        {
            throw new ArgumentException("Target id cannot be empty.", nameof(targetId));
        }

        if (statusId.IsEmpty)
        {
            throw new ArgumentException("Status id cannot be empty.", nameof(statusId));
        }

        if (ticksApplied <= 0 || damage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ticksApplied));
        }

        TargetId = targetId;
        StatusId = statusId;
        TicksApplied = ticksApplied;
        Damage = damage;
        Expired = expired;
    }

    public EntityId TargetId { get; }
    public CombatStatusId StatusId { get; }
    public int TicksApplied { get; }
    public int Damage { get; }
    public bool Expired { get; }
}

public readonly struct CombatStatusSnapshot
{
    public CombatStatusSnapshot(
        EntityId targetId,
        CombatStatusId statusId,
        CombatActionId sourceActionId,
        long nextTick,
        int remainingTicks,
        int damagePerTick)
    {
        TargetId = targetId;
        StatusId = statusId;
        SourceActionId = sourceActionId;
        NextTick = nextTick;
        RemainingTicks = remainingTicks;
        DamagePerTick = damagePerTick;
    }

    public EntityId TargetId { get; }
    public CombatStatusId StatusId { get; }
    public CombatActionId SourceActionId { get; }
    public long NextTick { get; }
    public int RemainingTicks { get; }
    public int DamagePerTick { get; }
}

internal sealed class CombatStatusState
{
    public CombatStatusState(
        EntityId targetId,
        CombatStatusDefinition definition,
        CombatActionId sourceActionId,
        long appliedTick)
    {
        TargetId = targetId;
        Definition = definition;
        SourceActionId = sourceActionId;
        NextTick = checked(appliedTick + 1);
        RemainingTicks = definition.DurationTicks;
    }

    public EntityId TargetId { get; }
    public CombatStatusDefinition Definition { get; private set; }
    public CombatActionId SourceActionId { get; private set; }
    public long NextTick { get; private set; }
    public int RemainingTicks { get; private set; }

    public void Refresh(
        CombatStatusDefinition definition,
        CombatActionId sourceActionId,
        long appliedTick)
    {
        Definition = definition;
        SourceActionId = sourceActionId;
        NextTick = checked(appliedTick + 1);
        RemainingTicks = definition.DurationTicks;
    }

    public CombatStatusDamage? Advance(long tick)
    {
        if (tick < NextTick || RemainingTicks <= 0)
        {
            return null;
        }

        long elapsed = checked(tick - NextTick + 1);
        int applied = (int)Math.Min(elapsed, RemainingTicks);
        RemainingTicks -= applied;
        NextTick = checked(NextTick + applied);
        return new CombatStatusDamage(
            TargetId,
            Definition.Id,
            applied,
            checked(applied * Definition.DamagePerTick),
            RemainingTicks == 0);
    }

    public CombatStatusSnapshot CreateSnapshot()
    {
        return new CombatStatusSnapshot(
            TargetId,
            Definition.Id,
            SourceActionId,
            NextTick,
            RemainingTicks,
            Definition.DamagePerTick);
    }
}
}
