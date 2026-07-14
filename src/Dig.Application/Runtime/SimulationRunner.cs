using System;
using Dig.Domain.Runtime;

namespace Dig.Application.Runtime
{

public sealed class SimulationRunner
{
    private readonly SimulationState _state;
    private readonly SimulationScheduler _scheduler;
    private long _accumulatedSimulationTicks;

    public SimulationRunner(
        SimulationState state,
        SimulationScheduler scheduler)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
    }

    public SimulationState State => _state;

    public TimeSpan PendingSimulationTime =>
        TimeSpan.FromTicks(_accumulatedSimulationTicks);

    public int Advance(TimeSpan elapsedRealTime)
    {
        if (elapsedRealTime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedRealTime));
        }

        if (elapsedRealTime == TimeSpan.Zero || _state.Clock.IsPaused)
        {
            return 0;
        }

        long scaledTicks = checked(
            elapsedRealTime.Ticks * _state.Clock.RateMultiplier);
        _accumulatedSimulationTicks = checked(
            _accumulatedSimulationTicks + scaledTicks);

        int executedTicks = 0;
        long tickDuration = _state.Clock.TickDuration.Ticks;

        while (_accumulatedSimulationTicks >= tickDuration)
        {
            _accumulatedSimulationTicks -= tickDuration;
            ExecuteOneTick();
            executedTicks = checked(executedTicks + 1);
        }

        return executedTicks;
    }

    public void Step(int tickCount = 1)
    {
        if (tickCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tickCount));
        }

        for (int index = 0; index < tickCount; index++)
        {
            ExecuteOneTick();
        }
    }

    public void ClearPendingTime()
    {
        _accumulatedSimulationTicks = 0;
    }

    private void ExecuteOneTick()
    {
        long tick = _state.Clock.AdvanceOneTick();
        _scheduler.ExecuteTick(new SimulationContext(tick, _state));
    }
}
}
