using System;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Agents
{

public sealed class AgentActionStarted : IDomainEvent
{
    public AgentActionStarted(
        long tick,
        EntityId agentId,
        AgentIntentKind intentKind,
        string? playerOrderId)
    {
        Tick = tick;
        AgentId = agentId;
        IntentKind = intentKind;
        PlayerOrderId = playerOrderId;
    }

    public long Tick { get; }

    public EntityId AgentId { get; }

    public AgentIntentKind IntentKind { get; }

    public string? PlayerOrderId { get; }
}

public sealed class AgentActionInterrupted : IDomainEvent
{
    public AgentActionInterrupted(
        long tick,
        EntityId agentId,
        AgentIntentKind previousIntent,
        AgentIntentKind nextIntent)
    {
        Tick = tick;
        AgentId = agentId;
        PreviousIntent = previousIntent;
        NextIntent = nextIntent;
    }

    public long Tick { get; }

    public EntityId AgentId { get; }

    public AgentIntentKind PreviousIntent { get; }

    public AgentIntentKind NextIntent { get; }
}

public sealed class AgentActionCompleted : IDomainEvent
{
    public AgentActionCompleted(long tick, EntityId agentId, AgentIntentKind intentKind)
    {
        Tick = tick;
        AgentId = agentId;
        IntentKind = intentKind;
    }

    public long Tick { get; }

    public EntityId AgentId { get; }

    public AgentIntentKind IntentKind { get; }
}

public sealed class AgentActionBlocked : IDomainEvent
{
    public AgentActionBlocked(
        long tick,
        EntityId agentId,
        AgentIntentKind intentKind,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Blocked action reason is required.", nameof(reason));
        }

        Tick = tick;
        AgentId = agentId;
        IntentKind = intentKind;
        Reason = reason.Trim();
    }

    public long Tick { get; }

    public EntityId AgentId { get; }

    public AgentIntentKind IntentKind { get; }

    public string Reason { get; }
}

public sealed class AgentPlayerOrderChanged : IDomainEvent
{
    public AgentPlayerOrderChanged(long tick, EntityId agentId, string? playerOrderId)
    {
        Tick = tick;
        AgentId = agentId;
        PlayerOrderId = playerOrderId;
    }

    public long Tick { get; }

    public EntityId AgentId { get; }

    public string? PlayerOrderId { get; }
}

public sealed class AgentScheduleChanged : IDomainEvent
{
    public AgentScheduleChanged(
        long tick,
        EntityId agentId,
        int workStartTickInclusive,
        int workEndTickExclusive)
    {
        Tick = tick;
        AgentId = agentId;
        WorkStartTickInclusive = workStartTickInclusive;
        WorkEndTickExclusive = workEndTickExclusive;
    }

    public long Tick { get; }

    public EntityId AgentId { get; }

    public int WorkStartTickInclusive { get; }

    public int WorkEndTickExclusive { get; }
}

public sealed class AgentAutomaticPlanningChanged : IDomainEvent
{
    public AgentAutomaticPlanningChanged(
        long tick,
        EntityId agentId,
        bool enabled)
    {
        Tick = tick;
        AgentId = agentId;
        Enabled = enabled;
    }

    public long Tick { get; }

    public EntityId AgentId { get; }

    public bool Enabled { get; }
}

public sealed class AgentDied : IDomainEvent
{
    public AgentDied(long tick, EntityId agentId, CellId? lastKnownPosition = null)
    {
        Tick = tick;
        AgentId = agentId;
        LastKnownPosition = lastKnownPosition;
    }

    public long Tick { get; }

    public EntityId AgentId { get; }

    public CellId? LastKnownPosition { get; }
}
}
