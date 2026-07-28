using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentHudInitializationGuardContractTests
{
    [Fact]
    public void Hud_bridge_returns_unavailable_projection_before_session_binding()
    {
        string root = FindRepositoryRoot();
        string bridge = File.ReadAllText(Path.Combine(
            root,
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime",
            "DigAgentSimulationDriverBase.Hud.cs"));
        string playMode = File.ReadAllText(Path.Combine(
            root,
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Tests",
            "PlayMode",
            "ResidentHudInitializationGuardPlayModeTests.cs"));

        Assert.Contains(
            "internal bool IsHudReady => AgentSession != null && TerrainSession != null;",
            bridge);
        Assert.Contains("ticksPerDay = 24;", bridge);
        Assert.Contains("startTickInclusive = 0;", bridge);
        Assert.Contains("endTickExclusive = 12;", bridge);
        Assert.Contains("enabled = true;", bridge);
        Assert.Contains("return Result.Failure(NotInitialized);", bridge);
        Assert.DoesNotContain("AgentSession!.TryGetWorkWindow", bridge);
        Assert.DoesNotContain("AgentSession!.TryGetAutomaticPlanning", bridge);
        Assert.Contains(
            "Uninitialized_driver_returns_unavailable_hud_projection_without_throwing",
            playMode);
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