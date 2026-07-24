using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class CampfirePlacementPresentationContractTests
{
    [Fact]
    public void Placement_uses_building_ghost_then_translucent_delivery_box()
    {
        string runtime = Path.Combine(
            FindRepositoryRoot(),
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime");
        string ghost = File.ReadAllText(Path.Combine(
            runtime,
            "DigBuildingBoxGhostRenderer.Representatives.cs"));
        string factory = File.ReadAllText(Path.Combine(
            runtime,
            "DigVisualPrefabFactory.cs"));
        string transparency = File.ReadAllText(Path.Combine(
            runtime,
            "DigTransparentVisualSurface.cs"));
        string tint = File.ReadAllText(Path.Combine(
            runtime,
            "DigVisualTintTarget.cs"));

        Assert.Contains("BuildingVisualState.Completed", ghost);
        Assert.Contains("instanceName.StartsWith(", factory);
        Assert.Contains("\"Building ghost \"", factory);
        Assert.Contains("instanceName.EndsWith(", factory);
        Assert.Contains("\" BuildingBox\"", factory);
        Assert.Contains("PlannedBuildingBoxOpacity = 0.48f", factory);
        Assert.Contains("RenderQueue.Transparent", transparency);
        Assert.Contains("LateUpdate()", transparency);
        Assert.Contains("CurrentTint", tint);
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
