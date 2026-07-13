namespace Dig.Domain.Runtime;

public enum SimulationRate
{
    Paused = 0,
    Normal = 1,
    Fast = 2,
    VeryFast = 4,
}

public readonly record struct SimulationClockSnapshot(
    long TickIndex,
    long TickDurationTicks,
    SimulationRate Rate);

public sealed class SimulationClock
{
    public SimulationClock(
        TimeSpan tickDuration,
        long initialTick = 0,
        SimulationRate initialRate = SimulationRate.Normal)
    {
        if (tickDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(tickDuration));
        }

        if (initialTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialTick));
        }

        ValidateRate(initialRate);

        TickDuration = tickDuration;
        TickIndex = initialTick;
        Rate = initialRate;
    }

    public long TickIndex { get; private set; }

    public TimeSpan TickDuration { get; }

    public SimulationRate Rate { get; private set; }

    public bool IsPaused => Rate == SimulationRate.Paused;

    public int RateMultiplier => (int)Rate;

    public void SetRate(SimulationRate rate)
    {
        ValidateRate(rate);
        Rate = rate;
    }

    public long AdvanceOneTick()
    {
        TickIndex = checked(TickIndex + 1);
        return TickIndex;
    }

    public SimulationClockSnapshot CaptureSnapshot()
    {
        return new SimulationClockSnapshot(
            TickIndex,
            TickDuration.Ticks,
            Rate);
    }

    public static SimulationClock Restore(SimulationClockSnapshot snapshot)
    {
        return new SimulationClock(
            TimeSpan.FromTicks(snapshot.TickDurationTicks),
            snapshot.TickIndex,
            snapshot.Rate);
    }

    private static void ValidateRate(SimulationRate rate)
    {
        if (rate is not SimulationRate.Paused
            and not SimulationRate.Normal
            and not SimulationRate.Fast
            and not SimulationRate.VeryFast)
        {
            throw new ArgumentOutOfRangeException(nameof(rate));
        }
    }
}
