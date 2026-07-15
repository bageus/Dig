using System;
using Dig.Domain.Core;

namespace Dig.Domain.Combat
{

public sealed class CombatAttackResolved : IDomainEvent
{
    public CombatAttackResolved(long tick, CombatAttackResolution resolution)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        Tick = tick;
        Resolution = resolution ?? throw new ArgumentNullException(nameof(resolution));
        EventId = $"combat-attack:{resolution.ActionId}";
    }

    public string EventId { get; }
    public long Tick { get; }
    public CombatAttackResolution Resolution { get; }
}

public sealed class CombatStatusTicked : IDomainEvent
{
    public CombatStatusTicked(long tick, CombatStatusDamage damage)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        Tick = tick;
        Damage = damage;
    }

    public long Tick { get; }
    public CombatStatusDamage Damage { get; }
}
}
