using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingBoxLabelsAndLadderDepthContractTests
{
    [Fact]
    public void Every_building_box_gets_a_visible_building_name_label()
    {
        string label = ReadRuntime("DigBuildingBoxLabel.cs");

        Assert.Contains("DigWorldItemVisualPolicy.IsBuildingBox(itemId)", label);
        Assert.Contains("RequireComponent(typeof(DigBuildingBoxLabel))", label);
        Assert.Contains("AddComponent<TextMesh>()", label);
        Assert.Contains("\"building_box.campfire\" => \"Campfire\"", label);
        Assert.Contains("\"building_box.stone_mason\" => \"Stone mason workshop\"", label);
        Assert.Contains("\"building_box.wood_workshop\" => \"Wooden workshop\"", label);
        Assert.Contains("\"building_box.ladder\" => \"Ladder\"", label);
        Assert.Contains("HumanizeBuildingBoxId(itemId)", label);
    }

    [Fact]
    public void Ladder_preview_and_completed_visual_stay_on_local_z0()
    {
        string projection = ReadRuntime("DigTunnelProjection.cs");
        string preview = ReadRuntime("DigBuildingBoxGhostRenderer.cs");
        string completed = ReadRuntime("DigBuildingVisual.cs");

        Assert.Contains("LadderWallDepthOffset = 0f;", projection);
        Assert.Contains("DigTunnelProjection.LadderWallDepthOffset", preview);
        Assert.Contains("DigTunnelProjection.LadderWallDepthOffset", completed);
        Assert.DoesNotContain("LadderWallDepthOffset = 0.42f", projection);
    }

    private static string ReadRuntime(string file)
    {
        return File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
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
            if (Directory.Exists(Path.Combine(current.FullName, "src"))
                && File.Exists(Path.Combine(current.FullName, "Dig.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}

}
