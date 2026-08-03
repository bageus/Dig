using System;

namespace Dig.Domain.Runtime
{

public static class GameTimeCadence
{
    public const int HoursPerDay = 24;
    public const int TicksPerHour = 150;
    public const int TicksPerDay = HoursPerDay * TicksPerHour;
    public const int GameSecondsPerTick = 24;

    public static readonly TimeSpan NormalTickDuration = TimeSpan.FromSeconds(1);

    public static long TicksFromHours(int hours)
    {
        if (hours < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hours));
        }

        return checked((long)hours * TicksPerHour);
    }

    public static long TicksFromDays(int days)
    {
        if (days < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days));
        }

        return checked((long)days * TicksPerDay);
    }

    public static int HourOfDay(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        return (int)((tick % TicksPerDay) / TicksPerHour);
    }
}

}
