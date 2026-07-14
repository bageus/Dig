using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Runtime;

namespace Dig.Headless.Soak
{

internal sealed class HeadlessSoakConfiguration
{
    public HeadlessSoakConfiguration(
        HeadlessSoakProfile profile,
        int seed,
        int tickCount,
        int residentCount,
        string reportPath,
        double maximumElapsedSeconds)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        if (tickCount < 100)
        {
            throw new ArgumentOutOfRangeException(nameof(tickCount));
        }

        if (residentCount < 2 || residentCount > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(residentCount));
        }

        if (string.IsNullOrWhiteSpace(reportPath))
        {
            throw new ArgumentException("Report path is required.", nameof(reportPath));
        }

        if (maximumElapsedSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumElapsedSeconds));
        }

        Seed = seed;
        TickCount = tickCount;
        ResidentCount = residentCount;
        ReportPath = reportPath;
        MaximumElapsedSeconds = maximumElapsedSeconds;
    }

    public HeadlessSoakProfile Profile { get; }

    public int Seed { get; }

    public int TickCount { get; }

    public int ResidentCount { get; }

    public string ReportPath { get; }

    public double MaximumElapsedSeconds { get; }

    public int InitialFoodQuantity => Profile.InitialFoodQuantity;

    public int HaulingWorkerCount => Math.Min(
        Profile.MaximumHaulingWorkers,
        ResidentCount);

    public SimulationPerformanceBudget PerformanceBudget => Profile.PerformanceBudget;

    public static HeadlessSoakConfiguration Parse(string[] args)
    {
        HeadlessSoakProfile profile = HeadlessSoakProfile.Parse(
            FindValue(args, "--profile"));
        return new HeadlessSoakConfiguration(
            profile,
            seed: ReadInt(args, "--seed", 4242),
            tickCount: ReadInt(args, "--ticks", profile.DefaultTickCount),
            residentCount: ReadInt(args, "--residents", profile.DefaultResidentCount),
            reportPath: ReadString(
                args,
                "--report",
                $"soak-report-{profile.Name}.json"),
            maximumElapsedSeconds: ReadDouble(
                args,
                "--max-seconds",
                profile.DefaultMaximumElapsedSeconds));
    }

    private static int ReadInt(string[] args, string name, int defaultValue)
    {
        string? raw = FindValue(args, name);
        return raw is null ? defaultValue : int.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static double ReadDouble(string[] args, string name, double defaultValue)
    {
        string? raw = FindValue(args, name);
        return raw is null
            ? defaultValue
            : double.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string ReadString(string[] args, string name, string defaultValue)
    {
        return FindValue(args, name) ?? defaultValue;
    }

    private static string? FindValue(string[] args, string name)
    {
        int index = Array.FindIndex(
            args,
            value => string.Equals(value, name, StringComparison.Ordinal));
        if (index < 0)
        {
            return null;
        }

        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value after '{name}'.", nameof(args));
        }

        return args[index + 1];
    }
}

internal sealed class HeadlessSoakSystemReport
{
    public HeadlessSoakSystemReport(SystemPerformanceSummary summary)
    {
        Name = summary.SystemName;
        Executions = summary.ExecutionCount;
        TotalMilliseconds = summary.TotalMilliseconds;
        AverageMicroseconds = summary.AverageMicroseconds;
        MaximumMilliseconds = summary.MaximumMilliseconds;
        TotalAllocatedBytes = summary.TotalAllocatedBytes;
        AverageAllocatedBytes = summary.AverageAllocatedBytes;
    }

    public string Name { get; }

    public int Executions { get; }

    public double TotalMilliseconds { get; }

    public double AverageMicroseconds { get; }

    public double MaximumMilliseconds { get; }

    public long TotalAllocatedBytes { get; }

    public long AverageAllocatedBytes { get; }
}

internal sealed class HeadlessSoakReport
{
    public string Profile { get; init; } = string.Empty;

    public int Seed { get; init; }

    public int RequestedTicks { get; init; }

    public long FinalTick { get; init; }

    public int ResidentCount { get; init; }

    public int HaulingWorkerCount { get; init; }

    public int InitialFoodQuantity { get; init; }

    public int EntityCount { get; init; }

    public int SpawnedOre { get; init; }

    public int TotalOre { get; init; }

    public int StoredOre { get; init; }

    public int RemainingFood { get; init; }

    public int CompletedHaulingJobs { get; init; }

    public int ActiveHaulingJobs { get; init; }

    public int JobReservationCount { get; init; }

    public int StorageReservationCount { get; init; }

    public int FacilityReservationCount { get; init; }

    public int RetainedEventCount { get; init; }

    public long DroppedEventCount { get; init; }

    public double ElapsedMilliseconds { get; init; }

    public double MaximumElapsedMilliseconds { get; init; }

    public string StateHash { get; init; } = string.Empty;

    public IReadOnlyList<HeadlessSoakSystemReport> Systems { get; init; } =
        Array.Empty<HeadlessSoakSystemReport>();

    public IReadOnlyList<string> BudgetViolations { get; init; } =
        Array.Empty<string>();

    public IReadOnlyList<string> InvariantViolations { get; init; } =
        Array.Empty<string>();

    public bool DeterministicReplayMatched { get; set; }

    public bool Succeeded { get; set; }
}
}
