using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace Dig.Domain.Agents
{

public enum ScheduleActivity
{
    Work = 0,
    Rest = 1,
    Sleep = 2,
    Free = 3,
}

public readonly struct ScheduleSegment
{
    public ScheduleSegment(
        int startTickInclusive,
        int endTickExclusive,
        ScheduleActivity activity)
    {
        if (startTickInclusive < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startTickInclusive));
        }

        if (endTickExclusive <= startTickInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(endTickExclusive));
        }

        if (!Enum.IsDefined(typeof(ScheduleActivity), activity))
        {
            throw new ArgumentOutOfRangeException(nameof(activity));
        }

        StartTickInclusive = startTickInclusive;
        EndTickExclusive = endTickExclusive;
        Activity = activity;
    }

    public int StartTickInclusive { get; }

    public int EndTickExclusive { get; }

    public ScheduleActivity Activity { get; }

    public bool Contains(int tickOfDay)
    {
        return tickOfDay >= StartTickInclusive && tickOfDay < EndTickExclusive;
    }
}

public sealed class DailySchedule
{
    public DailySchedule(int ticksPerDay, IEnumerable<ScheduleSegment> segments)
    {
        if (ticksPerDay <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ticksPerDay));
        }

        if (segments is null)
        {
            throw new ArgumentNullException(nameof(segments));
        }

        ScheduleSegment[] ordered = segments
            .OrderBy(segment => segment.StartTickInclusive)
            .ToArray();
        ValidateCoverage(ticksPerDay, ordered);

        TicksPerDay = ticksPerDay;
        Segments = new ReadOnlyCollection<ScheduleSegment>(ordered);
    }

    public int TicksPerDay { get; }

    public IReadOnlyList<ScheduleSegment> Segments { get; }

    public ScheduleActivity GetActivity(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        int tickOfDay = (int)(tick % TicksPerDay);
        foreach (ScheduleSegment segment in Segments)
        {
            if (segment.Contains(tickOfDay))
            {
                return segment.Activity;
            }
        }

        throw new InvalidOperationException("The schedule does not cover the full day.");
    }

    public static DailySchedule CreateBalanced(int ticksPerDay)
    {
        if (ticksPerDay < 4)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ticksPerDay),
                "A balanced schedule requires at least four ticks per day.");
        }

        int workEnd = Math.Max(1, ticksPerDay / 2);
        int restEnd = Math.Max(workEnd + 1, (ticksPerDay * 3) / 4);
        restEnd = Math.Min(restEnd, ticksPerDay - 1);

        return new DailySchedule(
            ticksPerDay,
            new[]
            {
                new ScheduleSegment(0, workEnd, ScheduleActivity.Work),
                new ScheduleSegment(workEnd, restEnd, ScheduleActivity.Rest),
                new ScheduleSegment(restEnd, ticksPerDay, ScheduleActivity.Sleep),
            });
    }

    private static void ValidateCoverage(
        int ticksPerDay,
        IReadOnlyList<ScheduleSegment> segments)
    {
        if (segments.Count == 0)
        {
            throw new ArgumentException(
                "A schedule requires at least one segment.",
                nameof(segments));
        }

        int expectedStart = 0;
        foreach (ScheduleSegment segment in segments)
        {
            if (segment.StartTickInclusive != expectedStart)
            {
                throw new ArgumentException(
                    "Schedule segments must be contiguous and non-overlapping.",
                    nameof(segments));
            }

            if (segment.EndTickExclusive > ticksPerDay)
            {
                throw new ArgumentException(
                    "Schedule segments cannot exceed the day length.",
                    nameof(segments));
            }

            expectedStart = segment.EndTickExclusive;
        }

        if (expectedStart != ticksPerDay)
        {
            throw new ArgumentException(
                "Schedule segments must cover the full day.",
                nameof(segments));
        }
    }
}
}
