using System.Diagnostics;
using Dig.Application.Runtime;

namespace Dig.Infrastructure.InMemory;

public sealed class InMemorySimulationPerformance : ISimulationPerformanceSink
{
    private readonly object _gate = new object();
    private readonly Dictionary<string, MutableSystemPerformance> _systems =
        new Dictionary<string, MutableSystemPerformance>(StringComparer.Ordinal);

    public void Record(SystemPerformanceSample sample)
    {
        lock (_gate)
        {
            if (!_systems.TryGetValue(sample.SystemName, out MutableSystemPerformance? value))
            {
                value = new MutableSystemPerformance(sample.SystemName);
                _systems.Add(sample.SystemName, value);
            }

            value.Add(sample);
        }
    }

    public SimulationPerformanceReport CreateReport(SimulationPerformanceBudget budget)
    {
        if (budget is null)
        {
            throw new ArgumentNullException(nameof(budget));
        }

        lock (_gate)
        {
            List<SystemPerformanceSummary> summaries = _systems.Values
                .Select(value => value.CreateSummary())
                .ToList();
            List<PerformanceBudgetViolation> violations =
                new List<PerformanceBudgetViolation>();
            foreach (SystemPerformanceSummary summary in summaries)
            {
                SystemPerformanceBudgetLimit limit = budget.GetLimit(summary.SystemName);
                if (summary.AverageMicroseconds > limit.MaximumAverageMicroseconds)
                {
                    violations.Add(new PerformanceBudgetViolation(
                        summary.SystemName,
                        "average_time",
                        $"{summary.AverageMicroseconds:F2} us exceeds "
                        + $"{limit.MaximumAverageMicroseconds:F2} us."));
                }

                if (summary.AverageAllocatedBytes > limit.MaximumAverageAllocatedBytes)
                {
                    violations.Add(new PerformanceBudgetViolation(
                        summary.SystemName,
                        "average_allocations",
                        $"{summary.AverageAllocatedBytes} bytes exceeds "
                        + $"{limit.MaximumAverageAllocatedBytes} bytes."));
                }

                if (summary.MaximumMilliseconds
                    > limit.MaximumSingleExecutionMilliseconds)
                {
                    violations.Add(new PerformanceBudgetViolation(
                        summary.SystemName,
                        "maximum_time",
                        $"{summary.MaximumMilliseconds:F3} ms exceeds "
                        + $"{limit.MaximumSingleExecutionMilliseconds:F3} ms."));
                }
            }

            return new SimulationPerformanceReport(summaries, violations);
        }
    }

    private sealed class MutableSystemPerformance
    {
        private long _totalTimestampTicks;
        private long _maximumTimestampTicks;
        private long _totalAllocatedBytes;

        public MutableSystemPerformance(string systemName)
        {
            SystemName = systemName;
        }

        public string SystemName { get; }

        public int ExecutionCount { get; private set; }

        public void Add(SystemPerformanceSample sample)
        {
            ExecutionCount = checked(ExecutionCount + 1);
            _totalTimestampTicks = checked(
                _totalTimestampTicks + sample.ElapsedTimestampTicks);
            _maximumTimestampTicks = Math.Max(
                _maximumTimestampTicks,
                sample.ElapsedTimestampTicks);
            _totalAllocatedBytes = checked(
                _totalAllocatedBytes + sample.AllocatedBytes);
        }

        public SystemPerformanceSummary CreateSummary()
        {
            double totalMilliseconds = TimestampTicksToMilliseconds(_totalTimestampTicks);
            double averageMicroseconds = ExecutionCount == 0
                ? 0
                : totalMilliseconds * 1000 / ExecutionCount;
            long averageAllocated = ExecutionCount == 0
                ? 0
                : _totalAllocatedBytes / ExecutionCount;
            return new SystemPerformanceSummary(
                SystemName,
                ExecutionCount,
                totalMilliseconds,
                averageMicroseconds,
                TimestampTicksToMilliseconds(_maximumTimestampTicks),
                _totalAllocatedBytes,
                averageAllocated);
        }

        private static double TimestampTicksToMilliseconds(long ticks)
        {
            return ticks * 1000d / Stopwatch.Frequency;
        }
    }
}
