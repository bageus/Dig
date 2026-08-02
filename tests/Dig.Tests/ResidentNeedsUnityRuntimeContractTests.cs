using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentNeedsUnityRuntimeContractTests
{
    [Fact]
    public void Unity_uses_session_tick_duration_instead_of_independent_cadence()
    {
        string session = ReadRuntime("DigAgentSession.cs");
        string sessionNeeds = ReadRuntime("DigAgentSession.ResidentNeeds.cs");
        string driver = ReadRuntime("DigAgentSimulationDriverBase.cs");

        Assert.Contains("TimeSpan.FromSeconds(2)", session);
        Assert.Contains("_simulationState.Clock.TickDuration", sessionNeeds);
        Assert.Contains("agentSession.TickDuration.TotalSeconds", driver);
    }

    [Fact]
    public void Tent_sleep_and_food_package_use_are_real_runtime_targets()
    {
        string needs = ReadRuntime("DigTerrainWorkSession.ResidentNeeds.cs");
        string sleep = ReadRuntime("DigTerrainWorkSession.ResidentSleep.cs");
        string food = ReadRuntime("DigTerrainWorkSession.ResidentFood.cs");
        string navigation = ReadRuntime("DigTerrainWorkNavigation.cs");

        Assert.Contains("TryExecuteResidentNeedsAction", needs);
        Assert.Contains("CampfireProductionContent.TentBuildingId", sleep);
        Assert.Contains("CreateTentSlotId", sleep);
        Assert.Contains("TryPlanResidentSleepMovement", navigation);
        Assert.Contains("StartAutomaticProductionPackageUse", food);
        Assert.Contains("eatAfterPickup: true", food);
        Assert.Contains("automatic: true", food);
    }

    private static string ReadRuntime(string file)
    {
        return File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime",
            file));
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
