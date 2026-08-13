using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class FogNavigationUnityRuntimeContractTests
{
    [Fact]
    public void Exploration_change_rebuilds_shared_manual_and_automatic_navigation()
    {
        string loop = ReadRuntime("DigAgentSimulationDriverBase.Loop.cs");
        string topology = ReadRuntime("DigAgentSession.TunnelTopology.cs");

        Assert.Contains("if (explorationChanged)", loop);
        Assert.Contains("SynchronizeExcavatedTunnelNavigation();", loop);
        Assert.Contains("CreateTunnelRoutePlanners();", topology);
        Assert.Contains("_combatExecution?.UpdateNavigationVolume(refreshed);", topology);
    }

    private static string ReadRuntime(string file)
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Dig.sln")))
            {
                return File.ReadAllText(Path.Combine(
                    current.FullName,
                    "Assets",
                    "Dig.Unity",
                    "Runtime",
                    file));
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}

}
