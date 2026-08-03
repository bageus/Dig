using System;
using Dig.Domain.Runtime;
using Xunit;

namespace Dig.Tests
{

public sealed class GameTimeCadenceTests
{
    [Fact]
    public void One_day_is_derived_from_the_single_real_to_game_coefficient()
    {
        Assert.Equal(TimeSpan.FromSeconds(1), GameTimeCadence.NormalTickDuration);
        Assert.Equal(1_000, GameTimeCadence.RealMillisecondsPerNormalTick);
        Assert.Equal(24, GameTimeCadence.GameSecondsPerRealSecond);
        Assert.Equal(24, GameTimeCadence.GameSecondsPerTick);
        Assert.Equal(150, GameTimeCadence.TicksPerHour);
        Assert.Equal(3_600, GameTimeCadence.TicksPerDay);
        Assert.Equal(GameTimeCadence.TicksPerDay, GameTimeCadence.TicksFromDays(1));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(149, 0)]
    [InlineData(150, 1)]
    [InlineData(3_599, 23)]
    [InlineData(3_600, 0)]
    public void Hour_of_day_wraps_on_authoritative_calendar(long tick, int expected)
    {
        Assert.Equal(expected, GameTimeCadence.HourOfDay(tick));
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(1, 0, 0, 0, 24)]
    [InlineData(149, 0, 0, 59, 36)]
    [InlineData(150, 0, 1, 0, 0)]
    [InlineData(3_599, 0, 23, 59, 36)]
    [InlineData(3_600, 1, 0, 0, 0)]
    public void Projection_uses_the_same_coefficient_for_day_hour_minute_and_second(
        long tick,
        long expectedDay,
        int expectedHour,
        int expectedMinute,
        int expectedSecond)
    {
        GameTimeSnapshot value = GameTimeCadence.Project(tick);

        Assert.Equal(expectedDay, value.DayIndex);
        Assert.Equal(expectedHour, value.Hour);
        Assert.Equal(expectedMinute, value.Minute);
        Assert.Equal(expectedSecond, value.Second);
        Assert.Equal(tick * GameTimeCadence.GameSecondsPerTick, value.TotalGameSeconds);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 24)]
    [InlineData(2, 48)]
    [InlineData(4, 96)]
    public void Playback_multiplier_scales_the_same_real_to_game_coefficient(
        int multiplier,
        int expectedGameSecondsPerRealSecond)
    {
        Assert.Equal(
            expectedGameSecondsPerRealSecond,
            GameTimeCadence.EffectiveGameSecondsPerRealSecond(multiplier));
    }
}

}
