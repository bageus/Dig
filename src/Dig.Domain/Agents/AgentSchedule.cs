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

        TicksPerDay = ticksPerDay;
        Segments = NormalizeSegments(ticksPerDay, segments);
    }

    public int TicksPerDay { get; }

    public IReadOnlyList<ScheduleSegment> Segments { get; private set; }

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

    public bool TryGetWorkWindow(out int startTickInclusive, out int endTickExclusive)
    {
        ScheduleSegment[] work = Segments
            .Where(segment => segment.Activity == ScheduleActivity.Work)
            .ToArray();
        if (work.Length == 1)
        {
            startTickInclusive = work[0].StartTickInclusive;
            endTickExclusive = work[0].EndTickExclusive % TicksPerDay;
            return true;
        }

        if (work.Length == 2
            && work[0].StartTickInclusive == 0
            && work[1].EndTickExclusive == TicksPerDay)
        {
            startTickInclusive = work[1].StartTickInclusive;
            endTickExclusive = work[0].EndTickExclusive;
            return true;
        }

        startTickInclusive = 0;
        endTickExclusive = 0;
        return false;
    }

    internal void SetWorkRestWindow(
        int workStartTickInclusive,
        int workEndTickExclusive)
    {
        DailySchedule replacement = CreateWorkRest(
            TicksPerDay,
            workStartTickInclusive,
            workEndTickExclusive);
        Segments = replacement.Segments;
    }

    public static DailySchedule CreateWorkRest(
        int ticksPerDay,
        int workStartTickInclusive,
        int workEndTickExclusive)
    {
        ValidateWorkWindow(
            ticksPerDay,
            workStartTickInclusive,
            workEndTickExclusive);
        List<ScheduleSegment> segments = new List<ScheduleSegment>(3);
        if (workStartTickInclusive < workEndTickExclusive)
        {
            AddSegment(
                segments,
                0,
                workStartTickInclusive,
                ScheduleActivity.Rest);
            AddSegment(
                segments,
                workStartTickInclusive,
                workEndTickExclusive,
                ScheduleActivity.Work);
            AddSegment(
                segments,
                workEndTickExclusive,
                ticksPerDay,
                ScheduleActivity.Rest);
        }
        else
        {
            AddSegment(
                segments,
                0,
                workEndTickExclusive,
                ScheduleActivity.Work);
            AddSegment(
                segments,
                workEndTickExclusive,
                workStartTickInclusive,
                ScheduleActivity.Rest);
            AddSegment(
                segments,
                workStartTickInclusive,
                ticksPerDay,
                ScheduleActivity.Work);
        }

        return new DailySchedule(ticksPerDay, segments);
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

    private static IReadOnlyList<ScheduleSegment> NormalizeSegments(
        int ticksPerDay,
        IEnumerable<ScheduleSegment> segments)
    {
        if (segments is null)
        {
            throw new ArgumentNullException(nameof(segments));
        }

        ScheduleSegment[] ordered = segments
            .OrderBy(segment => segment.StartTickInclusive)
            .ToArray();
        ValidateCoverage(ticksPerDay, ordered);
        return new ReadOnlyCollection<ScheduleSegment>(ordered);
    }

    private static void AddSegment(
        ICollection<ScheduleSegment> segments,
        int start,
        int end,
        ScheduleActivity activity)
    {
        if (end > start)
        {
            segments.Add(new ScheduleSegment(start, end, activity));
        }
    }

    private static void ValidateWorkWindow(int ticksPerDay, int start, int end)
    {
        if (ticksPerDay < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(ticksPerDay));
        }

        if (start < 0 || start >= ticksPerDay)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        if (end < 0 || end >= ticksPerDay)
        {
            throw new ArgumentOutOfRangeException(nameof(end));
        }

        if (start == end)
        {
            throw new ArgumentException("The work interval must leave time for rest.");
        }
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