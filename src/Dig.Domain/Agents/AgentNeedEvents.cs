using System;
using Dig.Domain.Core;

namespace Dig.Domain.Agents
{

public enum AgentNeedThresholdKind
{
    Hunger = 0,
    CriticalMood = 1,
}

public sealed class AgentNeedThresholdCrossed : IDomainEvent
{
    public AgentNeedThresholdCrossed(
        long tick,
        EntityId agentId,
        AgentNeedThresholdKind kind,
        int threshold,
        int previousValue,
        int currentValue)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (agentId.IsEmpty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(agentId));
        }

        if (!Enum.IsDefined(typeof(AgentNeedThresholdKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (threshold < NeedValue.Minimum || threshold > NeedValue.Maximum
            || previousValue < threshold
            || previousValue > NeedValue.Maximum
            || currentValue >= threshold
            || currentValue < NeedValue.Minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(currentValue));
        }

        Tick = tick;
        AgentId = agentId;
        Kind = kind;
        Threshold = threshold;
        PreviousValue = previousValue;
        CurrentValue = currentValue;
        EventId = $"agent-need:{agentId}:{kind}:{tick}";
    }

    public string EventId { get; }
    public long Tick { get; }
    public EntityId AgentId { get; }
    public AgentNeedThresholdKind Kind { get; }
    public int Threshold { get; }
    public int PreviousValue { get; }
    public int CurrentValue { get; }
}

}
