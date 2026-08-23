using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingBoxLabelsAndLadderDepthContractTests
{
    [Fact]
    public void Building_boxes_use_front_contents_icons_without_floating_labels()
    {
        string label = ReadRuntime("DigBuildingBoxLabel.cs");

        Assert.Contains("DigWorldItemVisualPolicy.IsBuildingBox(itemId)", label);
        Assert.Contains("RequireComponent(typeof(DigBuildingBoxLabel))", label);
        Assert.Contains("Building box contents icon", label);
        Assert.Contains("CreateFlame(parent)", label);
        Assert.Contains("CreateStoneAndHammer(parent)", label);
        Assert.Contains("CreateHammerAndSaw(parent)", label);
        Assert.Contains("CreateFood(parent)", label);
        Assert.Contains("\"package.food\"", label);
        Assert.DoesNotContain("PrimitiveType.Sphere", label);
        Assert.Contains("FaceCameraOnFrontSurface", label);
        Assert.Contains("ResolveFrontDepth", label);
        Assert.Contains("Quaternion.LookRotation", label);
        Assert.Contains("PrimitiveType.Quad", label);
        Assert.Contains("CreateFlatPart", label);
        Assert.DoesNotContain("CreatePart(parent", label);
        Assert.DoesNotContain("AddComponent<TextMesh>()", label);
        Assert.DoesNotContain("ResolveBuildingName", label);
        Assert.DoesNotContain("LabelOffset", label);
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
