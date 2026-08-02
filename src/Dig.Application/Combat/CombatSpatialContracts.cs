using System;
using Dig.Application.Messaging;
using Dig.Domain.Combat;
using Dig.Domain.Core;

namespace Dig.Application.Combat
{

public static class CombatSpatialApplicationErrors
{
    public static readonly DomainError IntentMissing = new DomainError(
        "combat.spatial.intent_missing",
        "The actor has no active combat intent to execute.");
    public static readonly DomainError ActorUnavailable = new DomainError(
        "combat.spatial.actor_unavailable",
        "The combat actor is missing or dead.");
    public static readonly DomainError TargetUnavailable = new DomainError(
        "combat.spatial.target_unavailable",
        "The combat target is missing, dead or no longer hostile.");
    public static readonly DomainError EquipmentUnavailable = new DomainError(
        "combat.spatial.equipment_unavailable",
        "No valid combat equipment profile is available for the actor.");
    public static readonly DomainError EngagementUnavailable = new DomainError(
        "combat.spatial.engagement_unavailable",
        "No reachable combat engagement cell satisfies the weapon profile.");
    public static readonly DomainError RetreatUnavailable = new DomainError(
        "combat.spatial.retreat_unavailable",
        "No reachable supported retreat cell increases distance from threats.");
}

public sealed class CombatSpatialPolicy
{
    public CombatSpatialPolicy(
        int sightRange,
        int alarmRadius,
        long windUpTicks,
        long recoveryTicks,
        long retryDelayTicks,
        int maximumRetries,
        CombatTacticalPolicy tacticalPolicy,
        Func<EntityId, bool>? retainsAggro = null)
    {
        if (sightRange < 0 || alarmRadius < 0 || windUpTicks < 0
            || recoveryTicks < 0 || retryDelayTicks <= 0 || maximumRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sightRange));
        }

        SightRange = sightRange;
        AlarmRadius = alarmRadius;
        WindUpTicks = windUpTicks;
        RecoveryTicks = recoveryTicks;
        RetryDelayTicks = retryDelayTicks;
        MaximumRetries = maximumRetries;
        TacticalPolicy = tacticalPolicy ?? throw new ArgumentNullException(nameof(tacticalPolicy));
        RetainsAggro = retainsAggro ?? (_ => false);
    }

    public int SightRange { get; }
    public int AlarmRadius { get; }
    public long WindUpTicks { get; }
    public long RecoveryTicks { get; }
    public long RetryDelayTicks { get; }
    public int MaximumRetries { get; }
    public CombatTacticalPolicy TacticalPolicy { get; }
    public Func<EntityId, bool> RetainsAggro { get; }
}

public readonly struct CombatEquipmentSelection
{
    public CombatEquipmentSelection(
        WeaponProfileId weaponProfileId,
        CombatantModifiers attackerModifiers,
        CombatantModifiers targetModifiers)
    {
        if (weaponProfileId.IsEmpty)
        {
            throw new ArgumentException("Weapon profile id is required.", nameof(weaponProfileId));
        }

        WeaponProfileId = weaponProfileId;
        AttackerModifiers = attackerModifiers;
        TargetModifiers = targetModifiers;
    }

    public WeaponProfileId WeaponProfileId { get; }
    public CombatantModifiers AttackerModifiers { get; }
    public CombatantModifiers TargetModifiers { get; }
}

public interface ICombatEquipmentProvider
{
    Result<CombatEquipmentSelection> Select(EntityId actorId, EntityId targetId);
}

public sealed class AdvanceCombatSpatialExecutionCommand
    : ICommand<Result<CombatSpatialExecutionReport>>
{
    public AdvanceCombatSpatialExecutionCommand(EntityId actorId, ulong worldSeed, long tick)
    {
        if (actorId.IsEmpty) throw new ArgumentException("Actor id is required.", nameof(actorId));
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        ActorId = actorId;
        WorldSeed = worldSeed;
        Tick = tick;
    }

    public EntityId ActorId { get; }
    public ulong WorldSeed { get; }
    public long Tick { get; }
}

public sealed class CombatSpatialExecutionReport
{
    public CombatSpatialExecutionReport(
        CombatExecutionSnapshot execution,
        bool moved,
        CombatAttackResolution? attack,
        string reasonCode)
    {
        Execution = execution ?? throw new ArgumentNullException(nameof(execution));
        Moved = moved;
        Attack = attack;
        ReasonCode = string.IsNullOrWhiteSpace(reasonCode) ? "unspecified" : reasonCode.Trim();
    }

    public CombatExecutionSnapshot Execution { get; }
    public bool Moved { get; }
    public CombatAttackResolution? Attack { get; }
    public string ReasonCode { get; }
}
}
