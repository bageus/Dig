using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class VukerTopologyRefreshRuntimeContractTests
{
    [Fact]
    public void NavigationRebuildRefreshesEcologyAndCombatOnlyWhenTopologyChanges()
    {
        string topology = ReadRuntime("DigAgentSession.TunnelTopology.cs");
        string ecology = ReadRuntime("DigAgentSession.VukerNavigationRefresh.cs");
        string combat = Read(
            "src", "Dig.Application", "Combat",
            "CombatSpatialExecutionHandler.cs");
        string playMode = Read(
            "Assets", "Dig.Unity", "Tests", "PlayMode",
            "VukerTopologyRefreshPlayModeTests.cs");

        Assert.Contains("HasSameNavigationTopology(previous, refreshed)", topology,
            StringComparison.Ordinal);
        Assert.Contains("RefreshVukerEcologyNavigation(refreshed)", topology,
            StringComparison.Ordinal);
        Assert.Contains("ResolveVukerEcologyNavigation", ecology,
            StringComparison.Ordinal);
        Assert.Contains("TunnelNavigationVolume.CreateDemo", ecology,
            StringComparison.Ordinal);
        Assert.Contains("_combatExecution?.UpdateNavigationVolume(refreshed)", topology,
            StringComparison.Ordinal);
        Assert.Contains("new VukerCaveRegionResolver(ecologyNavigation)", ecology,
            StringComparison.Ordinal);
        Assert.Contains("new VukerBirthPlanner(_vukerRegions)", ecology,
            StringComparison.Ordinal);
        Assert.Contains("public void UpdateNavigationVolume", combat,
            StringComparison.Ordinal);
        Assert.Contains("TopologyRefreshAllowsVukerInNewlyExcavatedSupportedCell",
            playMode,
            StringComparison.Ordinal);
        Assert.Contains("agents.SynchronizeNavigation", playMode,
            StringComparison.Ordinal);
        Assert.Contains("agents.Advance()", playMode, StringComparison.Ordinal);
    }

    private static string ReadRuntime(string file) => Read(
        "Assets", "Dig.Unity", "Runtime", file);

    private static string Read(params string[] parts)
    {
        string path = FindRepositoryRoot();
        foreach (string part in parts)
        {
            path = Path.Combine(path, part);
        }

        return File.ReadAllText(path);
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
