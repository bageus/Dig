using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class SimulationDriverResilienceContractTests
{
    [Fact]
    public void Recoverable_tick_failures_do_not_disable_the_global_simulation_driver()
    {
        string root = FindRepositoryRoot();
        string loop = File.ReadAllText(Path.Combine(
            root,
            "Assets",
            "Dig.Unity",
            "Runtime",
            "DigAgentSimulationDriverBase.Loop.cs"));

        Assert.Contains("if (result.IsFailure)", loop);
        Assert.Contains("Hud!.SetCommandResult(result);", loop);
        Assert.DoesNotContain("enabled = false;", loop);
    }

    [Fact]
    public void Direct_world_item_pickup_runs_even_after_an_earlier_tick_failure()
    {
        string root = FindRepositoryRoot();
        string loop = File.ReadAllText(Path.Combine(
            root,
            "Assets",
            "Dig.Unity",
            "Runtime",
            "DigAgentSimulationDriverBase.Loop.cs"));

        Assert.Contains(
            "Result worldItemPickupResult = TerrainSession.AdvanceWorldItemPickup(",
            loop);
        Assert.Contains(
            "if (result.IsSuccess && worldItemPickupResult.IsFailure)",
            loop);
        Assert.DoesNotContain(
            "if (result.IsSuccess)\n            {\n"
                + "                result = TerrainSession.AdvanceWorldItemPickup",
            loop);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Dig.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}

}
