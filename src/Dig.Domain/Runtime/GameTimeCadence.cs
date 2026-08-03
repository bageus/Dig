using System;

namespace Dig.Domain.Runtime
{

public readonly struct GameTimeSnapshot
{
    public GameTimeSnapshot(
        long totalGameSeconds,
        long dayIndex,
        int hour,
        int minute,
        int second,
        int tickOfDay)
    {
        TotalGameSeconds = totalGameSeconds;
        DayIndex = dayIndex;
        Hour = hour;
        Minute = minute;
        Second = second;
        TickOfDay = tickOfDay;
    }

    public long TotalGameSeconds { get; }

    public long DayIndex { get; }

    public int Hour { get; }

    public int Minute { get; }

    public int Second { get; }

    public int TickOfDay { get; }
}

public static class GameTimeCadence
{
    public const int GameSecondsPerMinute = 60;
    public const int GameMinutesPerHour = 60;
    public const int HoursPerDay = 24;
    public const int GameSecondsPerHour = GameSecondsPerMinute * GameMinutesPerHour;
    public const int GameSecondsPerDay = GameSecondsPerHour * HoursPerDay;

    public const int RealMillisecondsPerNormalTick = 1_000;
    public const int GameSecondsPerRealSecond = 24;
    public const int GameSecondsPerTick = GameSecondsPerRealSecond;
    public const int TicksPerHour = GameSecondsPerHour / GameSecondsPerTick;
    public const int TicksPerDay = GameSecondsPerDay / GameSecondsPerTick;

    public static readonly TimeSpan NormalTickDuration =
        TimeSpan.FromMilliseconds(RealMillisecondsPerNormalTick);

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

    public static long TotalGameSeconds(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        return checked(tick * GameSecondsPerTick);
    }

    public static int TickOfDay(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        return (int)(tick % TicksPerDay);
    }

    public static int HourOfDay(long tick)
    {
        return Project(tick).Hour;
    }

    public static GameTimeSnapshot Project(long tick)
    {
        long totalGameSeconds = TotalGameSeconds(tick);
        long dayIndex = totalGameSeconds / GameSecondsPerDay;
        int secondOfDay = (int)(totalGameSeconds % GameSecondsPerDay);
        int hour = secondOfDay / GameSecondsPerHour;
        int minute = (secondOfDay % GameSecondsPerHour) / GameSecondsPerMinute;
        int second = secondOfDay % GameSecondsPerMinute;
        return new GameTimeSnapshot(
            totalGameSeconds,
            dayIndex,
            hour,
            minute,
            second,
            TickOfDay(tick));
    }

    public static int EffectiveGameSecondsPerRealSecond(int playbackMultiplier)
    {
        if (playbackMultiplier < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(playbackMultiplier));
        }

        return checked(GameSecondsPerRealSecond * playbackMultiplier);
    }
}

}
