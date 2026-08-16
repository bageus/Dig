using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class FarmUnityRuntimeContractTests
{
    [Fact]
    public void Runtime_projects_authoritative_farm_snapshots_after_buildings_exist()
    {
        string runtime = RuntimeRoot();
        string session = Read(runtime, "DigTerrainWorkSession.Farming.cs");
        string renderer = Read(runtime, "DigBuildingRenderer.Farming.cs");
        string loop = Read(runtime, "DigAgentSimulationDriverBase.Loop.cs");
        string bootstrap = Read(runtime, "DigUnityBootstrap.cs");

        Assert.Contains("LoadAllFarmSnapshots()", session);
        Assert.Contains("CreateSnapshot()", session);
        Assert.Contains("GetComponent<DigFarmVisualDecoration>()", renderer);
        Assert.Contains("decoration.SetState(snapshot)", renderer);
        Assert.True(
            loop.IndexOf("BuildingRenderer.Render(buildings)", StringComparison.Ordinal)
            < loop.IndexOf("BuildingRenderer.RenderFarmContents(", StringComparison.Ordinal));
        Assert.True(
            bootstrap.IndexOf("buildingRenderer.Render(buildings)", StringComparison.Ordinal)
            < bootstrap.IndexOf("buildingRenderer.RenderFarmContents(", StringComparison.Ordinal));
    }

    [Fact]
    public void Decoration_visualizes_stock_and_escaping_animals_without_physics()
    {
        string decoration = Read(RuntimeRoot(), "DigFarmVisualDecoration.cs");

        Assert.Contains("new GameObject[3]", decoration);
        Assert.Equal(2, Count(decoration, "new GameObject[8]"));
        Assert.Contains("new GameObject[2]", decoration);
        Assert.Contains("snapshot.MushroomSlotsOccupied + snapshot.ResidualMushrooms", decoration);
        Assert.Contains("snapshot.HamsterCount + snapshot.EscapingHamsterCount", decoration);
        Assert.Contains("snapshot.GrubCount + snapshot.EscapingGrubCount", decoration);
        Assert.Contains("SetVisible(_mushrooms, mushrooms)", decoration);
        Assert.Contains("SetVisible(_hamsters, hamsters)", decoration);
        Assert.Contains("SetVisible(_grubs, grubs)", decoration);
        Assert.Contains("snapshot.FeedCount", decoration);
        Assert.Contains("SetVisible(_feedCaps", decoration);
        Assert.Contains("-0.09f + (index * 0.18f)", decoration);
        Assert.Contains("Destroy(collider)", decoration);
        Assert.DoesNotContain("AddForce", decoration);
        Assert.DoesNotContain("Rigidbody", decoration);
    }

    [Fact]
    public void Farm_mushroom_harvest_creates_physical_cap_and_leg_outputs()
    {
        string runtime = RuntimeRoot();
        string session = Read(runtime, "DigTerrainWorkSession.Farming.cs");
        string hud = Read(runtime, "DigGameHudCanvas.Farming.cs");

        Assert.Contains("HarvestFarmMushroom(string buildingId, long tick)", session);
        Assert.Contains("CollectFarmProductCommand", session);
        Assert.Contains("FarmDeliveryKind.MushroomSeed", session);
        Assert.Contains("_farmItems.MushroomCap", session);
        Assert.Contains("CampfireProductionContent.MushroomLegItemId", session);
        Assert.Equal(2, Count(session, "ItemLocation.InWorld(farm.Origin)"));
        Assert.Contains("Harvest mushroom (", hud);
        Assert.Contains("() => HarvestFarmMushroom(building.Id)", hud);
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

    private static string Read(string root, string file) =>
        File.ReadAllText(Path.Combine(root, file));

    private static string RuntimeRoot() => Path.Combine(
        FindRepositoryRoot(),
        "Assets",
        "Dig.Unity",
        "Runtime");

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "Assets")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}

}
