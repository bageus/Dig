using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class WorkshopProductionUnityRuntimeContractTests
{
    [Fact]
    public void Demo_spawns_both_workshop_boxes_and_registers_workshop_content()
    {
        string inventory = ReadRuntime("DigTerrainWorkSession.ResidentInventoryDemo.cs");
        string production = ReadRuntime("DigBuildingProductionExecution.cs");

        Assert.Contains("CampfireProductionContent.WoodWorkshopBoxItemId", inventory);
        Assert.Contains("CampfireProductionContent.StoneMasonBoxItemId", inventory);
        Assert.Contains("WorkshopProductionContent.CreateItems()", inventory);
        Assert.Contains("WorkshopProductionContent.CreateRecipes", production);
        Assert.Contains("WorkshopProductionContent.CreateWorkstations", production);
    }

    private static string ReadRuntime(string file)
    {
        string root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(
            root,
            "Assets",
            "Dig.Unity",
            "Runtime",
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
