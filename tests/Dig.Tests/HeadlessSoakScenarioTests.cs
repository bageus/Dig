using System;
using Dig.Headless.Soak;
using Xunit;

namespace Dig.Tests
{

public sealed class HeadlessSoakScenarioTests
{
    [Fact]
    public void Registered_residents_complete_hauling_within_invariant_budget()
    {
        HeadlessSoakConfiguration configuration = new HeadlessSoakConfiguration(
            HeadlessSoakProfile.Parse("standard"),
            seed: 4242,
            tickCount: 500,
            residentCount: 8,
            reportPath: "unused-soak-report.json",
            maximumElapsedSeconds: 60);

        HeadlessSoakReport report = HeadlessSoakScenario.Execute(configuration);

        Assert.Equal(4, report.HaulingWorkerCount);
        Assert.True(
            report.CompletedHaulingJobs > 0,
            "The scenario must complete hauling so skill grants validate worker registration.");
        Assert.DoesNotContain(
            report.InvariantViolations,
            violation => violation.Contains(
                "agents.repository.not_found",
                StringComparison.Ordinal));
        Assert.DoesNotContain(
            report.BudgetViolations,
            violation => violation.StartsWith(
                "soak.invariants:",
                StringComparison.Ordinal));
    }
}
}
