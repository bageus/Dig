using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace Dig.Application.Runtime
{

public readonly struct SystemPerformanceSample
{
    public SystemPerformanceSample(
        long tick,
        string systemName,
        long elapsedTimestampTicks,
        long allocatedBytes)
    {
        if (tick <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (string.IsNullOrWhiteSpace(systemName))
        {
            throw new ArgumentException("System name is required.", nameof(systemName));
        }

        if (elapsedTimestampTicks < 0 || allocatedBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedTimestampTicks));
        }

        Tick = tick;
        SystemName = systemName.Trim();
        ElapsedTimestampTicks = elapsedTimestampTicks;
        AllocatedBytes = allocatedBytes;
    }

    public long Tick { get; }

    public string SystemName { get; }

    public long ElapsedTimestampTicks { get; }

    public long AllocatedBytes { get; }
}

public interface ISimulationPerformanceSink
{
    void Record(SystemPerformanceSample sample);
}

public sealed class NullSimulationPerformanceSink : ISimulationPerformanceSink
{
    public static readonly NullSimulationPerformanceSink Instance =
        new NullSimulationPerformanceSink();

    private NullSimulationPerformanceSink()
    {
    }

    public void Record(SystemPerformanceSample sample)
    {
    }
}

public sealed class SystemPerformanceBudgetLimit
{
    public SystemPerformanceBudgetLimit(
        string systemName,
        double maximumAverageMicroseconds,
        long maximumAverageAllocatedBytes,
        double maximumSingleExecutionMilliseconds)
    {
        if (string.IsNullOrWhiteSpace(systemName))
        {
            throw new ArgumentException("System name is required.", nameof(systemName));
        }

        ValidateLimits(
            maximumAverageMicroseconds,
            maximumAverageAllocatedBytes,
            maximumSingleExecutionMilliseconds);
        SystemName = systemName.Trim();
        MaximumAverageMicroseconds = maximumAverageMicroseconds;
        MaximumAverageAllocatedBytes = maximumAverageAllocatedBytes;
        MaximumSingleExecutionMilliseconds = maximumSingleExecutionMilliseconds;
    }

    public string SystemName { get; }

    public double MaximumAverageMicroseconds { get; }

    public long MaximumAverageAllocatedBytes { get; }

    public double MaximumSingleExecutionMilliseconds { get; }

    internal static void ValidateLimits(
        double maximumAverageMicroseconds,
        long maximumAverageAllocatedBytes,
        double maximumSingleExecutionMilliseconds)
    {
        if (maximumAverageMicroseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAverageMicroseconds));
        }

        if (maximumAverageAllocatedBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAverageAllocatedBytes));
        }

        if (maximumSingleExecutionMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumSingleExecutionMilliseconds));
        }
    }
}

public sealed class SimulationPerformanceBudget
{
    private readonly Dictionary<string, SystemPerformanceBudgetLimit> _overrides;

    public SimulationPerformanceBudget(
        double maximumAverageMicroseconds,
        long maximumAverageAllocatedBytes,
        double maximumSingleExecutionMilliseconds,
        IEnumerable<SystemPerformanceBudgetLimit>? overrides = null)
    {
        SystemPerformanceBudgetLimit.ValidateLimits(
            maximumAverageMicroseconds,
            maximumAverageAllocatedBytes,
            maximumSingleExecutionMilliseconds);
        MaximumAverageMicroseconds = maximumAverageMicroseconds;
        MaximumAverageAllocatedBytes = maximumAverageAllocatedBytes;
        MaximumSingleExecutionMilliseconds = maximumSingleExecutionMilliseconds;
        SystemPerformanceBudgetLimit[] values = (overrides
                ?? Array.Empty<SystemPerformanceBudgetLimit>())
            .ToArray();
        if (values.Any(value => value is null)
            || values.Select(value => value.SystemName).Distinct(StringComparer.Ordinal).Count()
                != values.Length)
        {
            throw new ArgumentException(
                "Performance budget overrides must be non-null and unique by system name.",
                nameof(overrides));
        }

        _overrides = values.ToDictionary(
            value => value.SystemName,
            StringComparer.Ordinal);
    }

    public double MaximumAverageMicroseconds { get; }

    public long MaximumAverageAllocatedBytes { get; }

    public double MaximumSingleExecutionMilliseconds { get; }

    public SystemPerformanceBudgetLimit GetLimit(string systemName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
        {
            throw new ArgumentException("System name is required.", nameof(systemName));
        }

        return _overrides.TryGetValue(systemName, out SystemPerformanceBudgetLimit? value)
            ? value
            : new SystemPerformanceBudgetLimit(
                systemName,
                MaximumAverageMicroseconds,
                MaximumAverageAllocatedBytes,
                MaximumSingleExecutionMilliseconds);
    }
}

public sealed class SystemPerformanceSummary
{
    public SystemPerformanceSummary(
        string systemName,
        int executionCount,
        double totalMilliseconds,
        double averageMicroseconds,
        double maximumMilliseconds,
        long totalAllocatedBytes,
        long averageAllocatedBytes)
    {
        SystemName = systemName;
        ExecutionCount = executionCount;
        TotalMilliseconds = totalMilliseconds;
        AverageMicroseconds = averageMicroseconds;
        MaximumMilliseconds = maximumMilliseconds;
        TotalAllocatedBytes = totalAllocatedBytes;
        AverageAllocatedBytes = averageAllocatedBytes;
    }

    public string SystemName { get; }

    public int ExecutionCount { get; }

    public double TotalMilliseconds { get; }

    public double AverageMicroseconds { get; }

    public double MaximumMilliseconds { get; }

    public long TotalAllocatedBytes { get; }

    public long AverageAllocatedBytes { get; }
}

public sealed class PerformanceBudgetViolation
{
    public PerformanceBudgetViolation(string systemName, string metric, string detail)
    {
        SystemName = systemName;
        Metric = metric;
        Detail = detail;
    }

    public string SystemName { get; }

    public string Metric { get; }

    public string Detail { get; }
}

public sealed class SimulationPerformanceReport
{
    public SimulationPerformanceReport(
        IReadOnlyCollection<SystemPerformanceSummary> systems,
        IReadOnlyCollection<PerformanceBudgetViolation> violations)
    {
        Systems = new ReadOnlyCollection<SystemPerformanceSummary>(systems
            .OrderByDescending(value => value.TotalMilliseconds)
            .ThenBy(value => value.SystemName, StringComparer.Ordinal)
            .ToArray());
        Violations = new ReadOnlyCollection<PerformanceBudgetViolation>(violations
            .OrderBy(value => value.SystemName, StringComparer.Ordinal)
            .ThenBy(value => value.Metric, StringComparer.Ordinal)
            .ToArray());
    }

    public IReadOnlyList<SystemPerformanceSummary> Systems { get; }

    public IReadOnlyList<PerformanceBudgetViolation> Violations { get; }

    public bool IsWithinBudget => Violations.Count == 0;
}
}
