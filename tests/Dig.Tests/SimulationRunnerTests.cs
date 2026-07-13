using Dig.Application.Runtime;
using Dig.Domain.Runtime;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests;

public sealed class SimulationRunnerTests
{
    [Fact]
    public void Frame_partition_does_not_change_ticks_or_random_sequence()
    {
        RuntimeFixture singleFrame = RuntimeFixture.Create(seed: 777);
        RuntimeFixture splitFrames = RuntimeFixture.Create(seed: 777);

        int singleTicks = singleFrame.Runner.Advance(TimeSpan.FromSeconds(1));
        int splitTicks = 0;
        for (int index = 0; index < 10; index++)
        {
            splitTicks += splitFrames.Runner.Advance(TimeSpan.FromMilliseconds(100));
        }

        Assert.Equal(10, singleTicks);
        Assert.Equal(10, splitTicks);
        Assert.Equal(10, singleFrame.State.Clock.TickIndex);
        Assert.Equal(singleFrame.Values, splitFrames.Values);
    }

    [Fact]
    public void Pause_discards_elapsed_time_instead_of_accumulating_catch_up()
    {
        RuntimeFixture fixture = RuntimeFixture.Create(
            seed: 10,
            rate: SimulationRate.Paused);

        int pausedTicks = fixture.Runner.Advance(TimeSpan.FromSeconds(5));
        fixture.State.Clock.SetRate(SimulationRate.Normal);
        int resumedTicks = fixture.Runner.Advance(TimeSpan.FromMilliseconds(100));

        Assert.Equal(0, pausedTicks);
        Assert.Equal(1, resumedTicks);
        Assert.Equal(1, fixture.State.Clock.TickIndex);
        Assert.Equal(TimeSpan.Zero, fixture.Runner.PendingSimulationTime);
    }

    [Fact]
    public void Faster_rate_executes_more_fixed_ticks_without_fractional_drift()
    {
        RuntimeFixture fixture = RuntimeFixture.Create(
            seed: 10,
            rate: SimulationRate.Fast);

        int executedTicks = fixture.Runner.Advance(TimeSpan.FromMilliseconds(250));

        Assert.Equal(5, executedTicks);
        Assert.Equal(5, fixture.State.Clock.TickIndex);
        Assert.Equal(TimeSpan.Zero, fixture.Runner.PendingSimulationTime);
    }

    [Fact]
    public void Scheduler_uses_stable_order_and_per_system_intervals()
    {
        SimulationState state = SimulationState.Create(
            worldSeed: 1,
            tickDuration: TimeSpan.FromMilliseconds(100));
        InMemorySimulationTrace trace = new InMemorySimulationTrace();
        SimulationScheduler scheduler = new SimulationScheduler(trace);
        scheduler.Register(new NoOpSystem("system.b", order: 20, intervalTicks: 1));
        scheduler.Register(new NoOpSystem("system.c", order: 10, intervalTicks: 2));
        scheduler.Register(new NoOpSystem("system.a", order: 10, intervalTicks: 1));
        SimulationRunner runner = new SimulationRunner(state, scheduler);

        runner.Step(2);

        string[] actual = trace.Executions
            .Select(item => $"{item.Tick}:{item.SystemName}")
            .ToArray();
        Assert.Equal(
            new[]
            {
                "1:system.a",
                "1:system.b",
                "2:system.a",
                "2:system.c",
                "2:system.b",
            },
            actual);
    }

    [Fact]
    public void Scheduler_rejects_registration_after_first_tick()
    {
        RuntimeFixture fixture = RuntimeFixture.Create(seed: 1);
        fixture.Runner.Step();

        Assert.Throws<InvalidOperationException>(() =>
            fixture.Scheduler.Register(new NoOpSystem(
                "late.system",
                order: 1,
                intervalTicks: 1)));
    }

    private sealed class RuntimeFixture
    {
        private RuntimeFixture(
            SimulationState state,
            SimulationScheduler scheduler,
            SimulationRunner runner,
            List<ulong> values)
        {
            State = state;
            Scheduler = scheduler;
            Runner = runner;
            Values = values;
        }

        public SimulationState State { get; }

        public SimulationScheduler Scheduler { get; }

        public SimulationRunner Runner { get; }

        public IReadOnlyList<ulong> Values { get; }

        public static RuntimeFixture Create(
            ulong seed,
            SimulationRate rate = SimulationRate.Normal)
        {
            SimulationState state = SimulationState.Create(
                seed,
                TimeSpan.FromMilliseconds(100),
                rate);
            List<ulong> values = new List<ulong>();
            SimulationScheduler scheduler = new SimulationScheduler();
            scheduler.Register(new RandomCaptureSystem(values));
            return new RuntimeFixture(
                state,
                scheduler,
                new SimulationRunner(state, scheduler),
                values);
        }
    }

    private sealed class RandomCaptureSystem : ISimulationSystem
    {
        private readonly List<ulong> _values;

        public RandomCaptureSystem(List<ulong> values)
        {
            _values = values;
        }

        public string Name => "test.random_capture";

        public int Order => 1;

        public int IntervalTicks => 1;

        public void Execute(SimulationContext context)
        {
            _values.Add(
                context.State.RandomStreams
                    .GetOrCreate("test.values")
                    .NextUInt64());
        }
    }

    private sealed class NoOpSystem : ISimulationSystem
    {
        public NoOpSystem(string name, int order, int intervalTicks)
        {
            Name = name;
            Order = order;
            IntervalTicks = intervalTicks;
        }

        public string Name { get; }

        public int Order { get; }

        public int IntervalTicks { get; }

        public void Execute(SimulationContext context)
        {
        }
    }
}
