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

        Assert.Contains("GameTimeCadence.NormalTickDuration", session);
        Assert.Contains("DailySchedule.CreateBalanced(GameTimeCadence.TicksPerDay)", session);
        Assert.Contains("_simulationState.Clock.TickDuration", sessionNeeds);
        Assert.Contains("agentSession.TickDuration.TotalSeconds", driver);
    }

    [Fact]
    public void Agent_session_advances_the_authoritative_simulation_clock()
    {
        string session = ReadRuntime("DigAgentSession.cs");

        Assert.Contains(
            "public long Tick => _simulationState.Clock.Tick;",
            session);
        Assert.Contains(
            "long tick = _simulationState.Clock.AdvanceOneTick();",
            session);
        Assert.DoesNotContain("private long _tick;", session);
        Assert.DoesNotContain("_tick = checked(_tick + 1);", session);
    }

    [Fact]
    public void Global_clock_and_passive_needs_share_one_time_scale()
    {
        string clock = ReadRuntime("DigGameHudCanvas.Clock.cs");
        string driverHud = ReadRuntime("DigAgentSimulationDriverBase.Hud.cs");
        string needs = ReadRepository(
            "src/Dig.Domain/Agents/AgentState.NeedThresholds.cs");

        Assert.Contains("GameTimeCadence.Project(tick)", clock);
        Assert.Contains("GameTimeCadence.GameSecondsPerDay", clock);
        Assert.DoesNotContain("int ticksPerDay = 24", clock);
        Assert.Contains("GameTimeCadence.TicksPerDay", driverHud);
        Assert.Contains("GameTimeCadence.TicksPerDay", needs);
        Assert.DoesNotContain("Schedule.TicksPerDay", needs);
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
        return ReadRepository(Path.Combine(
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime",
            file));
    }

    private static string ReadRepository(string relativePath)
    {
        return File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            relativePath));
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
