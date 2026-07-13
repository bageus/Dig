using Dig.Domain.Core;

namespace Dig.Domain.Agents;

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

public sealed class AgentDied : IDomainEvent
{
    public AgentDied(long tick, EntityId agentId)
    {
        Tick = tick;
        AgentId = agentId;
    }

    public long Tick { get; }

    public EntityId AgentId { get; }
}
