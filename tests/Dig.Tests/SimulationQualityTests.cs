using Dig.Application.Runtime;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Runtime;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests;

public sealed class SimulationQualityTests
{
    [Fact]
    public void Scheduler_records_system_time_and_allocations()
    {
        InMemorySimulationPerformance performance = new InMemorySimulationPerformance();
        SimulationScheduler scheduler = new SimulationScheduler(performance: performance);
        scheduler.Register(new CountingSystem());
        SimulationState state = SimulationState.Create(
            worldSeed: 5,
            tickDuration: TimeSpan.FromMilliseconds(100));
        SimulationRunner runner = new SimulationRunner(state, scheduler);

        runner.Step(3);

        SimulationPerformanceReport report = performance.CreateReport(
            new SimulationPerformanceBudget(
                maximumAverageMicroseconds: 1_000_000,
                maximumAverageAllocatedBytes: 10_000_000,
                maximumSingleExecutionMilliseconds: 10_000));
        SystemPerformanceSummary summary = Assert.Single(report.Systems);
        Assert.Equal("quality.counting", summary.SystemName);
        Assert.Equal(3, summary.ExecutionCount);
        Assert.True(summary.TotalMilliseconds >= 0);
        Assert.True(summary.TotalAllocatedBytes >= 0);
        Assert.True(report.IsWithinBudget);
    }

    [Fact]
    public void Performance_report_exposes_budget_violations()
    {
        InMemorySimulationPerformance performance = new InMemorySimulationPerformance();
        performance.Record(new SystemPerformanceSample(
            tick: 1,
            systemName: "slow.system",
            elapsedTimestampTicks: long.MaxValue / 4,
            allocatedBytes: 1_000));

        SimulationPerformanceReport report = performance.CreateReport(
            new SimulationPerformanceBudget(
                maximumAverageMicroseconds: 1,
                maximumAverageAllocatedBytes: 1,
                maximumSingleExecutionMilliseconds: 1));

        Assert.False(report.IsWithinBudget);
        Assert.Contains(report.Violations, value => value.Metric == "average_time");
        Assert.Contains(report.Violations, value => value.Metric == "average_allocations");
        Assert.Contains(report.Violations, value => value.Metric == "maximum_time");
    }

    [Fact]
    public void Bounded_journal_retains_latest_events_and_counts_dropped_entries()
    {
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal(
            maximumCommands: 2,
            maximumEvents: 2);
        EntityId first = EntityId.Parse("a1000000000000000000000000000001");
        EntityId second = EntityId.Parse("a1000000000000000000000000000002");
        EntityId third = EntityId.Parse("a1000000000000000000000000000003");

        journal.Append(new IDomainEvent[]
        {
            new AgentDied(1, first),
            new AgentDied(2, second),
            new AgentDied(3, third),
        });

        Assert.Equal(1, journal.DroppedEventCount);
        Assert.Collection(
            journal.Events,
            value => Assert.Equal(second, Assert.IsType<AgentDied>(value).AgentId),
            value => Assert.Equal(third, Assert.IsType<AgentDied>(value).AgentId));
    }

    private sealed class CountingSystem : ISimulationSystem
    {
        public string Name => "quality.counting";

        public int Order => 100;

        public int IntervalTicks => 1;

        public void Execute(SimulationContext context)
        {
            _ = new byte[32];
        }
    }
}
