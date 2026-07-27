using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
public sealed class MushroomUnityRuntimeContractTests
{
    [Fact]
    public void Runtime_routes_mushroom_before_ground_movement_and_excavation()
    {
        string runtime = RuntimeRoot();
        string priority = Read(runtime, "DigWorldInteraction.ResidentCommandPriority.cs");
        string decisions = Read(runtime, "DigWorldInteraction.Decisions.cs");
        string cursor = Read(runtime, "DigWorldInteraction.DirectCommandCursor.cs");

        Assert.True(
            priority.IndexOf("TryResolveMushroomHit", StringComparison.Ordinal)
            < priority.IndexOf("_excavationMode!=DigExcavationDrawingMode.None", StringComparison.Ordinal));
        Assert.Contains("ContextWorldTargetKind.Mushroom", priority);
        Assert.Contains("ApplicationInputCommandKind.ChopMushroom", decisions);
        Assert.Contains("DirectCommandCursorKind.Axe", cursor);
        Assert.Contains("TryResolveMushroomHoverTarget", cursor);
    }

    [Fact]
    public void Runtime_uses_authoritative_mushroom_jobs_drops_skills_and_growth()
    {
        string runtime = RuntimeRoot();
        string mushrooms = Read(runtime, "DigTerrainWorkSession.Mushrooms.cs");
        string navigation = Read(runtime, "DigTerrainWorkSession.MushroomNavigation.cs");
        string loop = Read(runtime, "DigAgentSimulationDriverBase.Loop.cs");
        string bootstrap = Read(runtime, "DigUnityBootstrap.cs");
        string inventory = Read(runtime, "DigTerrainWorkSession.ResidentInventoryDemo.cs");

        Assert.Contains("StartDirectMushroomChopCommandHandler", mushrooms);
        Assert.Contains("CompleteMushroomChopCommandHandler", mushrooms);
        Assert.Contains("AdvanceMushroomGrowthCommand", mushrooms);
        Assert.Contains("MushroomChopJobDefinition", navigation);
        Assert.Contains("AdvanceMushrooms(AgentSession.Tick,agents)", loop);
        Assert.Contains("MushroomRenderer!.Render(TerrainSession!.LoadMushrooms())", loop);
        Assert.Contains("InitializeMushroomDemo(agentSession.Tick)", bootstrap);
        Assert.Contains("mushroomRenderer.Render(terrainSession.LoadMushrooms())", bootstrap);
        Assert.Contains("MushroomCapItemId", inventory);
        Assert.Contains("MushroomLegItemId", inventory);
    }

    [Fact]
    public void Mushroom_site_blocks_buildings_but_not_inventory_items()
    {
        string runtime = RuntimeRoot();
        string placement = Read(runtime, "DigBuildingBoxPlacement.cs");
        string items = Read(runtime, "DigTerrainWorkSession.ResidentInventoryDemo.cs");

        Assert.Equal(2, Count(placement, "MushroomBuildingBlockedCells"));
        Assert.DoesNotContain("MushroomBuildingBlockedCells", items);
    }

    [Fact]
    public void PlayMode_tests_respect_domain_and_runtime_visibility_boundaries()
    {
        string playMode = PlayModeRoot();
        string topology = Read(playMode, "PostExcavationTopologyPlayModeTests.cs");
        string mushrooms = Read(playMode, "MushroomChoppingPlayModeTests.cs");

        Assert.DoesNotContain(".Offset(", topology);
        Assert.Contains("newCellId(cell.X-1,cell.Y,cell.Z)", topology);
        Assert.Contains("newCellId(cell.X,cell.Y+1,cell.Z)", topology);
        Assert.DoesNotContain("renderer.Render(", mushrooms);
        Assert.DoesNotContain("renderer.ActiveCount", mushrooms);
        Assert.Contains("Invoke(renderer,\"Render\",(object)new[]{large})", mushrooms);
        Assert.Contains("GetProperty(renderer,\"ActiveCount\")", mushrooms);
    }

    private static int Count(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string Read(string root, string file) => Normalize(
        File.ReadAllText(Path.Combine(root, file)));

    private static string RuntimeRoot() => Path.Combine(
        FindRepositoryRoot(),
        "unity",
        "Dig.Unity",
        "Assets",
        "Dig.Unity",
        "Runtime");

    private static string PlayModeRoot() => Path.Combine(
        FindRepositoryRoot(),
        "unity",
        "Dig.Unity",
        "Assets",
        "Dig.Unity",
        "Tests",
        "PlayMode");

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

    private static string Normalize(string source) => source
        .Replace(" ", string.Empty, StringComparison.Ordinal)
        .Replace("\t", string.Empty, StringComparison.Ordinal)
        .Replace("\r", string.Empty, StringComparison.Ordinal)
        .Replace("\n", string.Empty, StringComparison.Ordinal);
}
}
