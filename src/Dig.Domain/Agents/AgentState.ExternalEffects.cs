using System;
using Dig.Domain.Core;

namespace Dig.Domain.Agents
{

public sealed class AgentExternalEffectApplied : IDomainEvent
{
    public AgentExternalEffectApplied(
        long tick,
        EntityId agentId,
        string sourceId,
        NeedDelta delta,
        int previousHealth,
        int currentHealth)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (agentId.IsEmpty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(agentId));
        }

        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("External effect source is required.", nameof(sourceId));
        }

        Tick = tick;
        AgentId = agentId;
        SourceId = sourceId.Trim();
        Delta = delta;
        PreviousHealth = previousHealth;
        CurrentHealth = currentHealth;
    }

    public long Tick { get; }
    public EntityId AgentId { get; }
    public string SourceId { get; }
    public NeedDelta Delta { get; }
    public int PreviousHealth { get; }
    public int CurrentHealth { get; }
}

public sealed partial class AgentState
{
    public Result ApplyExternalNeedDelta(
        NeedDelta delta,
        string sourceId,
        long tick)
    {
        ValidateTick(tick);
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("External effect source is required.", nameof(sourceId));
        }

        if (!IsAlive)
        {
            return Result.Failure(AgentErrors.AgentDead);
        }

        int previousHealth = _needs.Health.Points;
        ApplyNeedDelta(delta, tick);
        int currentHealth = _needs.Health.Points;
        Version = checked(Version + 1);
        Raise(new AgentExternalEffectApplied(
            tick,
            Id,
            sourceId,
            delta,
            previousHealth,
            currentHealth));
        HandleDeath(tick);
        return Result.Success();
    }
}
}
