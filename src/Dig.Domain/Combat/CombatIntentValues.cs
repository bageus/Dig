using System;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Combat
{

public readonly struct CombatIntentId : IEquatable<CombatIntentId>, IComparable<CombatIntentId>
{
    private readonly string? _value;

    public CombatIntentId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Combat intent id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public int CompareTo(CombatIntentId other)
    {
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public bool Equals(CombatIntentId other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is CombatIntentId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);
    }

    public override string ToString()
    {
        return _value ?? string.Empty;
    }

    public static bool operator ==(CombatIntentId left, CombatIntentId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CombatIntentId left, CombatIntentId right)
    {
        return !left.Equals(right);
    }
}

public enum CombatIntentSource
{
    Autonomous = 0,
    StrategicAi = 1,
    PlayerOrder = 2,
    Alarm = 3,
}

public enum CombatIntentStatus
{
    Active = 0,
    Completed = 1,
    Cancelled = 2,
    Expired = 3,
}

public sealed class CombatIntentRequest
{
    public CombatIntentRequest(
        CombatIntentId intentId,
        EntityId actorId,
        CombatIntentKind kind,
        CombatIntentSource source,
        long createdTick,
        long expiresTick,
        EntityId? targetEntityId = null,
        CellId? targetCell = null)
    {
        if (intentId.IsEmpty)
        {
            throw new ArgumentException("Combat intent id cannot be empty.", nameof(intentId));
        }

        if (actorId.IsEmpty)
        {
            throw new ArgumentException("Combat actor id cannot be empty.", nameof(actorId));
        }

        if (createdTick < 0 || expiresTick <= createdTick)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresTick));
        }

        if ((kind == CombatIntentKind.Attack || kind == CombatIntentKind.Approach)
            && !targetEntityId.HasValue)
        {
            throw new ArgumentException(
                "Attack and approach intents require a target entity.",
                nameof(targetEntityId));
        }

        if (targetEntityId.HasValue && targetEntityId.Value == actorId)
        {
            throw new ArgumentException("Combat actor cannot target itself.", nameof(targetEntityId));
        }

        IntentId = intentId;
        ActorId = actorId;
        Kind = kind;
        Source = source;
        CreatedTick = createdTick;
        ExpiresTick = expiresTick;
        TargetEntityId = targetEntityId;
        TargetCell = targetCell;
    }

    public CombatIntentId IntentId { get; }
    public EntityId ActorId { get; }
    public CombatIntentKind Kind { get; }
    public CombatIntentSource Source { get; }
    public long CreatedTick { get; }
    public long ExpiresTick { get; }
    public EntityId? TargetEntityId { get; }
    public CellId? TargetCell { get; }
}

public sealed class CombatIntentSnapshot
{
    public CombatIntentSnapshot(
        CombatIntentId intentId,
        EntityId actorId,
        CombatIntentKind kind,
        CombatIntentSource source,
        CombatIntentStatus status,
        long createdTick,
        long expiresTick,
        long? finishedTick,
        EntityId? targetEntityId,
        CellId? targetCell,
        string? finishReason)
    {
        IntentId = intentId;
        ActorId = actorId;
        Kind = kind;
        Source = source;
        Status = status;
        CreatedTick = createdTick;
        ExpiresTick = expiresTick;
        FinishedTick = finishedTick;
        TargetEntityId = targetEntityId;
        TargetCell = targetCell;
        FinishReason = finishReason;
    }

    public CombatIntentId IntentId { get; }
    public EntityId ActorId { get; }
    public CombatIntentKind Kind { get; }
    public CombatIntentSource Source { get; }
    public CombatIntentStatus Status { get; }
    public long CreatedTick { get; }
    public long ExpiresTick { get; }
    public long? FinishedTick { get; }
    public EntityId? TargetEntityId { get; }
    public CellId? TargetCell { get; }
    public string? FinishReason { get; }
    public bool IsActive => Status == CombatIntentStatus.Active;
}

public sealed class CombatIntentChanged : IDomainEvent
{
    public CombatIntentChanged(
        long tick,
        EntityId actorId,
        CombatIntentSnapshot? previous,
        CombatIntentSnapshot current)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        Tick = tick;
        ActorId = actorId;
        Previous = previous;
        Current = current ?? throw new ArgumentNullException(nameof(current));
    }

    public long Tick { get; }
    public EntityId ActorId { get; }
    public CombatIntentSnapshot? Previous { get; }
    public CombatIntentSnapshot Current { get; }
}

public sealed class CombatIntentFinished : IDomainEvent
{
    public CombatIntentFinished(long tick, CombatIntentSnapshot intent)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        Tick = tick;
        Intent = intent ?? throw new ArgumentNullException(nameof(intent));
    }

    public long Tick { get; }
    public CombatIntentSnapshot Intent { get; }
}
}
