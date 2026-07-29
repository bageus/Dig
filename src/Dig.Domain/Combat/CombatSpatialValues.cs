using System;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Combat
{

public readonly struct CombatExecutionId : IEquatable<CombatExecutionId>, IComparable<CombatExecutionId>
{
    private readonly string? _value;

    public CombatExecutionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Combat execution id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public int CompareTo(CombatExecutionId other) =>
        string.Compare(_value, other._value, StringComparison.Ordinal);

    public bool Equals(CombatExecutionId other) =>
        string.Equals(_value, other._value, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
        obj is CombatExecutionId other && Equals(other);

    public override int GetHashCode() =>
        StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);

    public override string ToString() => _value ?? string.Empty;

    public static bool operator ==(CombatExecutionId left, CombatExecutionId right) =>
        left.Equals(right);

    public static bool operator !=(CombatExecutionId left, CombatExecutionId right) =>
        !left.Equals(right);
}

public enum CombatAttackSpatialMode
{
    Melee = 0,
    Ranged = 1,
}

public enum CombatExecutionStage
{
    AcquireTarget = 0,
    SelectEquipment = 1,
    SelectEngagementCell = 2,
    Approach = 3,
    FaceTarget = 4,
    WindUp = 5,
    ResolveAttack = 6,
    Recover = 7,
    Reevaluate = 8,
    Retreat = 9,
    Blocked = 10,
    Completed = 11,
    Cancelled = 12,
}

public sealed class CombatExecutionRequest
{
    public CombatExecutionRequest(
        CombatExecutionId executionId,
        CombatIntentId intentId,
        EntityId actorId,
        CombatIntentSource source,
        CombatExecutionStage initialStage,
        long tick)
    {
        if (executionId.IsEmpty)
        {
            throw new ArgumentException("Execution id cannot be empty.", nameof(executionId));
        }

        if (intentId.IsEmpty)
        {
            throw new ArgumentException("Intent id cannot be empty.", nameof(intentId));
        }

        if (actorId.IsEmpty)
        {
            throw new ArgumentException("Actor id cannot be empty.", nameof(actorId));
        }

        if (initialStage == CombatExecutionStage.Completed
            || initialStage == CombatExecutionStage.Cancelled
            || !Enum.IsDefined(typeof(CombatExecutionStage), initialStage))
        {
            throw new ArgumentOutOfRangeException(nameof(initialStage));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        ExecutionId = executionId;
        IntentId = intentId;
        ActorId = actorId;
        Source = source;
        InitialStage = initialStage;
        Tick = tick;
    }

    public CombatExecutionId ExecutionId { get; }
    public CombatIntentId IntentId { get; }
    public EntityId ActorId { get; }
    public CombatIntentSource Source { get; }
    public CombatExecutionStage InitialStage { get; }
    public long Tick { get; }
}

public sealed class CombatExecutionSnapshot
{
    public CombatExecutionSnapshot(
        CombatExecutionId executionId,
        CombatIntentId intentId,
        EntityId actorId,
        CombatIntentSource source,
        CombatExecutionStage stage,
        long startedTick,
        long nextStageTick,
        EntityId? targetEntityId,
        CellId? lastKnownTargetCell,
        WeaponProfileId? weaponProfileId,
        CellId? engagementCell,
        CombatActionId? lastResolvedActionId,
        int resolvedActionCount,
        int retryCount,
        string reasonCode,
        long version)
    {
        if (executionId.IsEmpty || intentId.IsEmpty || actorId.IsEmpty)
        {
            throw new ArgumentException("Execution identity is incomplete.");
        }

        if (startedTick < 0 || nextStageTick < 0 || resolvedActionCount < 0
            || retryCount < 0 || version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startedTick));
        }

        ExecutionId = executionId;
        IntentId = intentId;
        ActorId = actorId;
        Source = source;
        Stage = stage;
        StartedTick = startedTick;
        NextStageTick = nextStageTick;
        TargetEntityId = targetEntityId;
        LastKnownTargetCell = lastKnownTargetCell;
        WeaponProfileId = weaponProfileId;
        EngagementCell = engagementCell;
        LastResolvedActionId = lastResolvedActionId;
        ResolvedActionCount = resolvedActionCount;
        RetryCount = retryCount;
        ReasonCode = reasonCode ?? string.Empty;
        Version = version;
    }

    public CombatExecutionId ExecutionId { get; }
    public CombatIntentId IntentId { get; }
    public EntityId ActorId { get; }
    public CombatIntentSource Source { get; }
    public CombatExecutionStage Stage { get; }
    public long StartedTick { get; }
    public long NextStageTick { get; }
    public EntityId? TargetEntityId { get; }
    public CellId? LastKnownTargetCell { get; }
    public WeaponProfileId? WeaponProfileId { get; }
    public CellId? EngagementCell { get; }
    public CombatActionId? LastResolvedActionId { get; }
    public int ResolvedActionCount { get; }
    public int RetryCount { get; }
    public string ReasonCode { get; }
    public long Version { get; }
    public bool IsTerminal => Stage == CombatExecutionStage.Completed
        || Stage == CombatExecutionStage.Cancelled;
}

public readonly struct CombatEngagementCandidate
{
    public CombatEngagementCandidate(
        CellId cell,
        int distanceToTarget,
        int routeCost,
        int softClaimCount,
        bool reachable,
        bool supported,
        bool hasImmediateTraversalEdge,
        bool hasLineOfSight)
    {
        if (distanceToTarget < 0 || routeCost < 0 || softClaimCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distanceToTarget));
        }

        Cell = cell;
        DistanceToTarget = distanceToTarget;
        RouteCost = routeCost;
        SoftClaimCount = softClaimCount;
        Reachable = reachable;
        Supported = supported;
        HasImmediateTraversalEdge = hasImmediateTraversalEdge;
        HasLineOfSight = hasLineOfSight;
    }

    public CellId Cell { get; }
    public int DistanceToTarget { get; }
    public int RouteCost { get; }
    public int SoftClaimCount { get; }
    public bool Reachable { get; }
    public bool Supported { get; }
    public bool HasImmediateTraversalEdge { get; }
    public bool HasLineOfSight { get; }
}

public readonly struct CombatRetreatCandidate
{
    public CombatRetreatCandidate(
        CellId cell,
        int minimumThreatDistance,
        int routeCost,
        bool reachable,
        bool supported,
        bool ownTerritory)
    {
        if (minimumThreatDistance < 0 || routeCost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumThreatDistance));
        }

        Cell = cell;
        MinimumThreatDistance = minimumThreatDistance;
        RouteCost = routeCost;
        Reachable = reachable;
        Supported = supported;
        OwnTerritory = ownTerritory;
    }

    public CellId Cell { get; }
    public int MinimumThreatDistance { get; }
    public int RouteCost { get; }
    public bool Reachable { get; }
    public bool Supported { get; }
    public bool OwnTerritory { get; }
}

public sealed class CombatAlarmStimulus
{
    public CombatAlarmStimulus(
        EntityId attackerId,
        EntityId victimId,
        CellId cell,
        int radius,
        long tick)
    {
        if (attackerId.IsEmpty || victimId.IsEmpty || attackerId == victimId)
        {
            throw new ArgumentException("Alarm requires distinct combatants.");
        }

        if (radius < 0 || tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius));
        }

        AttackerId = attackerId;
        VictimId = victimId;
        Cell = cell;
        Radius = radius;
        Tick = tick;
    }

    public EntityId AttackerId { get; }
    public EntityId VictimId { get; }
    public CellId Cell { get; }
    public int Radius { get; }
    public long Tick { get; }
}
}
