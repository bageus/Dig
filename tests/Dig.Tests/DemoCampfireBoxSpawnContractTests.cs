using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class DemoCampfireBoxSpawnContractTests
{
    [Fact]
    public void Demo_places_a_visible_packed_campfire_beside_starting_resident()
    {
        string root = FindRepositoryRoot();
        string runtime = Path.Combine(
            root,
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime");
        string composition = File.ReadAllText(Path.Combine(
            runtime,
            "DigTerrainWorkSession.Composition.cs"));
        string inventory = File.ReadAllText(Path.Combine(
            runtime,
            "DigTerrainWorkSession.ResidentInventoryDemo.cs"));
        string renderer = File.ReadAllText(Path.Combine(
            runtime,
            "DigWorldItemRenderer.cs"));

        Assert.Contains("AgentViewModel firstResident = agents[0];", composition);
        Assert.Contains("firstResident.CellX", composition);
        Assert.Contains("firstResident.CellY", composition);
        Assert.Contains("firstResident.CellZ", composition);
        Assert.Contains(
            "CampfireBuildingBoxContent.Definition.BoxItem",
            inventory);
        Assert.Contains(
            "CampfireBuildingBoxContent.CampfireBoxItemId",
            inventory);
        Assert.Contains("residentStartCell.X - 1", inventory);
        Assert.Contains("residentStartCell.Y", inventory);
        Assert.Contains("residentStartCell.Z", inventory);
        Assert.Contains("ItemLocation.InWorld(campfireBoxCell)", inventory);
        Assert.Contains("CampfireBoxFootprintSide = 0.35355339f", renderer);
        Assert.Contains("CampfireBoxHeight = 0.30f", renderer);
        Assert.Contains("IsCampfireBox(item.ItemId)", renderer);
        Assert.Contains("? Vector2.zero", renderer);
        Assert.Contains(
            "DigVisualAsset.CreateRuntimeFallback(itemId, CampfireBoxTint)",
            renderer);
        Assert.Contains("maxVisibleInstances: 1", renderer);
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
