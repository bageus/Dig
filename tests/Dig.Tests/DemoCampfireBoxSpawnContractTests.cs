using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class DemoCampfireBoxSpawnContractTests
{
    [Fact]
    public void Demo_places_a_packed_campfire_at_the_starting_resident_cell()
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
        Assert.Contains("ItemLocation.InWorld(campfireBoxCell)", inventory);
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
