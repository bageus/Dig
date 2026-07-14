using System;
using Dig.Domain.Core;

namespace Dig.Domain.Agents
{

public enum AgentIntentKind
{
    Flee = 0,
    Eat = 1,
    Sleep = 2,
    PlayerOrder = 3,
    Work = 4,
    Rest = 5,
    Idle = 6,
}

public enum AgentActivityTargetKind
{
    Food = 0,
    Bed = 1,
    Leisure = 2,
}

public readonly struct AgentActivityTarget : IEquatable<AgentActivityTarget>
{
    public AgentActivityTarget(AgentActivityTargetKind kind, EntityId entityId)
    {
        if (!Enum.IsDefined(typeof(AgentActivityTargetKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (entityId.IsEmpty)
        {
            throw new ArgumentException("Activity target id cannot be empty.", nameof(entityId));
        }

        Kind = kind;
        EntityId = entityId;
    }

    public AgentActivityTargetKind Kind { get; }

    public EntityId EntityId { get; }

    public bool Equals(AgentActivityTarget other)
    {
        return Kind == other.Kind && EntityId == other.EntityId;
    }

    public override bool Equals(object? obj)
    {
        return obj is AgentActivityTarget other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Kind, EntityId);
    }

    public override string ToString()
    {
        return $"{Kind}:{EntityId}";
    }

    public static bool operator ==(AgentActivityTarget left, AgentActivityTarget right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AgentActivityTarget left, AgentActivityTarget right)
    {
        return !left.Equals(right);
    }
}

public sealed class PlayerOrder
{
    public PlayerOrder(
        string id,
        string label,
        int priority,
        long issuedTick,
        long expiresTick)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Player order id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Player order label is required.", nameof(label));
        }

        if (priority < 0 || priority > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        if (issuedTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(issuedTick));
        }

        if (expiresTick < issuedTick)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresTick));
        }

        Id = id.Trim();
        Label = label.Trim();
        Priority = priority;
        IssuedTick = issuedTick;
        ExpiresTick = expiresTick;
    }

    public string Id { get; }

    public string Label { get; }

    public int Priority { get; }

    public long IssuedTick { get; }

    public long ExpiresTick { get; }

    public bool IsActiveAt(long tick)
    {
        return tick >= IssuedTick && tick <= ExpiresTick;
    }
}

public readonly struct AgentActionSnapshot
{
    public AgentActionSnapshot(
        AgentIntentKind intentKind,
        string? playerOrderId,
        long startedTick,
        int requiredTicks,
        int elapsedTicks)
        : this(
            intentKind,
            playerOrderId,
            startedTick,
            requiredTicks,
            elapsedTicks,
            target: null)
    {
    }

    public AgentActionSnapshot(
        AgentIntentKind intentKind,
        string? playerOrderId,
        long startedTick,
        int requiredTicks,
        int elapsedTicks,
        AgentActivityTarget? target)
    {
        IntentKind = intentKind;
        PlayerOrderId = playerOrderId;
        StartedTick = startedTick;
        RequiredTicks = requiredTicks;
        ElapsedTicks = elapsedTicks;
        Target = target;
    }

    public AgentIntentKind IntentKind { get; }

    public string? PlayerOrderId { get; }

    public long StartedTick { get; }

    public int RequiredTicks { get; }

    public int ElapsedTicks { get; }

    public AgentActivityTarget? Target { get; }

    public bool IsReadyToComplete => ElapsedTicks >= RequiredTicks;
}

internal sealed class ActiveAgentAction
{
    public ActiveAgentAction(
        AgentIntentKind intentKind,
        string? playerOrderId,
        long startedTick,
        int requiredTicks,
        AgentActivityTarget? target = null)
    {
        if (!Enum.IsDefined(typeof(AgentIntentKind), intentKind))
        {
            throw new ArgumentOutOfRangeException(nameof(intentKind));
        }

        if (startedTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startedTick));
        }

        if (requiredTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredTicks));
        }

        IntentKind = intentKind;
        PlayerOrderId = playerOrderId;
        StartedTick = startedTick;
        RequiredTicks = requiredTicks;
        Target = target;
    }

    public AgentIntentKind IntentKind { get; }

    public string? PlayerOrderId { get; }

    public long StartedTick { get; }

    public int RequiredTicks { get; }

    public int ElapsedTicks { get; private set; }

    public AgentActivityTarget? Target { get; }

    public bool IsReadyToComplete => ElapsedTicks >= RequiredTicks;

    public bool Advance()
    {
        if (IsReadyToComplete)
        {
            throw new InvalidOperationException("The action is already ready to complete.");
        }

        ElapsedTicks = checked(ElapsedTicks + 1);
        return IsReadyToComplete;
    }

    public AgentActionSnapshot CreateSnapshot()
    {
        return new AgentActionSnapshot(
            IntentKind,
            PlayerOrderId,
            StartedTick,
            RequiredTicks,
            ElapsedTicks,
            Target);
    }
}
}
