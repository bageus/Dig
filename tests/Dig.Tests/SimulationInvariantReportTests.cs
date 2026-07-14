using Dig.Application.Diagnostics;
using Dig.Domain.Core;
using Xunit;

namespace Dig.Tests;

public sealed class SimulationInvariantReportTests
{
    [Fact]
    public void Report_uses_shared_empty_result_for_valid_state()
    {
        SimulationInvariantReport first = new SimulationInvariantReport(
            tick: 1,
            Array.Empty<SimulationInvariantViolation>());
        SimulationInvariantReport second = new SimulationInvariantReport(
            tick: 2,
            Array.Empty<SimulationInvariantViolation>());

        Assert.True(first.IsValid);
        Assert.True(second.IsValid);
        Assert.Same(first.Violations, second.Violations);
    }

    [Fact]
    public void Report_sorts_violation_codes_entities_and_details_stably()
    {
        EntityId first = EntityId.Parse("81000000000000000000000000000001");
        EntityId second = EntityId.Parse("81000000000000000000000000000002");
        SimulationInvariantReport report = new SimulationInvariantReport(
            tick: 4,
            new[]
            {
                new SimulationInvariantViolation("z.code", "last", second),
                new SimulationInvariantViolation("a.code", "second detail", second),
                new SimulationInvariantViolation("a.code", "first detail", first),
                new SimulationInvariantViolation("a.code", "another detail", first),
            });

        Assert.Collection(
            report.Violations,
            value =>
            {
                Assert.Equal("a.code", value.Code);
                Assert.Equal(first, value.EntityId);
                Assert.Equal("another detail", value.Detail);
            },
            value =>
            {
                Assert.Equal("a.code", value.Code);
                Assert.Equal(first, value.EntityId);
                Assert.Equal("first detail", value.Detail);
            },
            value =>
            {
                Assert.Equal("a.code", value.Code);
                Assert.Equal(second, value.EntityId);
            },
            value => Assert.Equal("z.code", value.Code));
    }
}
