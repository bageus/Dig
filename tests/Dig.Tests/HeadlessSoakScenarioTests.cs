using System;
using Dig.Headless.Soak;
using Xunit;

namespace Dig.Tests
{

public sealed class HeadlessSoakScenarioTests
{
    [Fact]
    public void Hauling_workers_are_registered_residents()
    {
        HeadlessSoakConfiguration configuration = new HeadlessSoakConfiguration(
            HeadlessSoakProfile.Parse("standard"),
            seed: 4242,
            tickCount: 120,
            residentCount: 2,
            reportPath: "unused-soak-report.json",
            maximumElapsedSeconds: 60);

        HeadlessSoakReport report = HeadlessSoakScenario.Execute(configuration);

        Assert.Equal(2, report.HaulingWorkerCount);
        Assert.True(
            report.CompletedHaulingJobs > 0,
            "The scenario must complete hauling so skill grants validate worker registration.");
        Assert.DoesNotContain(
            report.InvariantViolations,
            violation => violation.Contains(
                "agents.repository.not_found",
                StringComparison.Ordinal));
    }
}
}
