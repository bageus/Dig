using Dig.Application.Runtime;

namespace Dig.Headless.Soak;

internal sealed class HeadlessSoakProfile
{
    private HeadlessSoakProfile(
        string name,
        int defaultTickCount,
        int defaultResidentCount,
        int initialFoodQuantity,
        int maximumHaulingWorkers,
        double defaultMaximumElapsedSeconds,
        SimulationPerformanceBudget performanceBudget)
    {
        Name = name;
        DefaultTickCount = defaultTickCount;
        DefaultResidentCount = defaultResidentCount;
        InitialFoodQuantity = initialFoodQuantity;
        MaximumHaulingWorkers = maximumHaulingWorkers;
        DefaultMaximumElapsedSeconds = defaultMaximumElapsedSeconds;
        PerformanceBudget = performanceBudget;
    }

    public string Name { get; }

    public int DefaultTickCount { get; }

    public int DefaultResidentCount { get; }

    public int InitialFoodQuantity { get; }

    public int MaximumHaulingWorkers { get; }

    public double DefaultMaximumElapsedSeconds { get; }

    public SimulationPerformanceBudget PerformanceBudget { get; }

    public static HeadlessSoakProfile Parse(string? value)
    {
        string name = string.IsNullOrWhiteSpace(value)
            ? "standard"
            : value.Trim().ToLowerInvariant();
        return name switch
        {
            "standard" => CreateStandard(),
            "large" => CreateLarge(),
            _ => throw new ArgumentException(
                $"Unknown soak profile '{value}'. Expected 'standard' or 'large'."),
        };
    }

    private static HeadlessSoakProfile CreateStandard()
    {
        return new HeadlessSoakProfile(
            name: "standard",
            defaultTickCount: 2_000,
            defaultResidentCount: 8,
            initialFoodQuantity: 5_000,
            maximumHaulingWorkers: 4,
            defaultMaximumElapsedSeconds: 30,
            performanceBudget: new SimulationPerformanceBudget(
                maximumAverageMicroseconds: 10_000,
                maximumAverageAllocatedBytes: 2_000_000,
                maximumSingleExecutionMilliseconds: 500,
                overrides: new[]
                {
                    new SystemPerformanceBudgetLimit(
                        "agents.settlement",
                        maximumAverageMicroseconds: 500,
                        maximumAverageAllocatedBytes: 12_000,
                        maximumSingleExecutionMilliseconds: 100),
                    new SystemPerformanceBudgetLimit(
                        "soak.hauling",
                        maximumAverageMicroseconds: 100,
                        maximumAverageAllocatedBytes: 25_000,
                        maximumSingleExecutionMilliseconds: 100),
                    new SystemPerformanceBudgetLimit(
                        "soak.invariants",
                        maximumAverageMicroseconds: 150,
                        maximumAverageAllocatedBytes: 10_000,
                        maximumSingleExecutionMilliseconds: 50),
                }));
    }

    private static HeadlessSoakProfile CreateLarge()
    {
        return new HeadlessSoakProfile(
            name: "large",
            defaultTickCount: 1_000,
            defaultResidentCount: 64,
            initialFoodQuantity: 64_000,
            maximumHaulingWorkers: 16,
            defaultMaximumElapsedSeconds: 10,
            performanceBudget: new SimulationPerformanceBudget(
                maximumAverageMicroseconds: 5_000,
                maximumAverageAllocatedBytes: 500_000,
                maximumSingleExecutionMilliseconds: 250,
                overrides: new[]
                {
                    new SystemPerformanceBudgetLimit(
                        "agents.settlement",
                        maximumAverageMicroseconds: 1_500,
                        maximumAverageAllocatedBytes: 80_000,
                        maximumSingleExecutionMilliseconds: 75),
                    new SystemPerformanceBudgetLimit(
                        "soak.hauling",
                        maximumAverageMicroseconds: 150,
                        maximumAverageAllocatedBytes: 20_000,
                        maximumSingleExecutionMilliseconds: 100),
                    new SystemPerformanceBudgetLimit(
                        "soak.invariants",
                        maximumAverageMicroseconds: 500,
                        maximumAverageAllocatedBytes: 40_000,
                        maximumSingleExecutionMilliseconds: 50),
                }));
    }
}
