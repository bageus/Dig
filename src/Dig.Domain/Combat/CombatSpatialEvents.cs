using System;
using Dig.Domain.Core;

namespace Dig.Domain.Combat
{

public sealed class CombatExecutionChanged : IDomainEvent
{
    public CombatExecutionChanged(
        long tick,
        CombatExecutionSnapshot? previous,
        CombatExecutionSnapshot current)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        Tick = tick;
        Previous = previous;
        Current = current ?? throw new ArgumentNullException(nameof(current));
    }

    public long Tick { get; }
    public CombatExecutionSnapshot? Previous { get; }
    public CombatExecutionSnapshot Current { get; }
}

public sealed class CombatAlarmPublished : IDomainEvent
{
    public CombatAlarmPublished(CombatAlarmStimulus stimulus)
    {
        Stimulus = stimulus ?? throw new ArgumentNullException(nameof(stimulus));
    }

    public long Tick => Stimulus.Tick;
    public CombatAlarmStimulus Stimulus { get; }
}
}
