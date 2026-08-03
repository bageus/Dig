using System;
using Dig.Domain.Runtime;
using Xunit;

namespace Dig.Tests
{

public sealed class GameTimeCadenceTests
{
    [Fact]
    public void One_day_is_twenty_four_hours_and_three_thousand_six_hundred_ticks()
    {
        Assert.Equal(TimeSpan.FromSeconds(1), GameTimeCadence.NormalTickDuration);
        Assert.Equal(150, GameTimeCadence.TicksPerHour);
        Assert.Equal(3_600, GameTimeCadence.TicksPerDay);
        Assert.Equal(24, GameTimeCadence.GameSecondsPerTick);
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
}

}
