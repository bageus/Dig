using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
public sealed class WorldItemAutomaticGroundingContractTests
{
    [Fact]
    public void Every_ordinary_item_projection_uses_one_geometry_grounding_owner()
    {
        string runtime = RuntimeRoot();
        string grounding = Read(runtime, "DigWorldItemGrounding.cs");
        string policy = Read(runtime, "DigWorldItemVisualPolicy.cs");
        string visual = Read(runtime, "DigWorldItemVisual.cs");
        string world = Read(runtime, "DigWorldItemRenderer.cs");
        string stock = Read(runtime, "DigBuildingInternalStockRenderer.cs");
        string inventoryGhost = Read(runtime, "DigInventoryItemGhostRenderer.cs");
        string boxGhost = Read(runtime, "DigBuildingBoxGhostRenderer.Items.cs");
        string playMode = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "Assets",
            "Dig.Unity",
            "Tests",
            "PlayMode",
            "WorldItemAutomaticGroundingPlayModeTests.cs"));

        Assert.Contains("GetComponentsInChildren<Renderer>", grounding);
        Assert.Contains("floorAnchor.y - bounds.min.y", grounding);
        Assert.Contains("ResolveLocalBounds", grounding);
        Assert.Contains("ResolveFloorAnchor", policy);
        Assert.DoesNotContain("WorldScale.y * 0.5f", policy);
        Assert.DoesNotContain("is_grounded", grounding, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ItemId", grounding);
        Assert.Contains("DigWorldItemGrounding.PlaceOnFloor", visual);
        Assert.Contains("visual.PlaceOnFloor", world);
        Assert.Contains("visual.PlaceOnFloor", stock);
        Assert.Contains("DigWorldItemGrounding.PlaceOnFloor", inventoryGhost);
        Assert.Equal(2, Count(boxGhost, "DigWorldItemGrounding.PlaceOnFloor"));
        Assert.Contains("Centered_and_bottom_pivots_touch_the_same_floor", playMode);
    }

    private static int Count(string source, string value)
    {
        int count = 0;
        int start = 0;
        while ((start = source.IndexOf(value, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += value.Length;
        }

        return count;
    }

    private static string Read(string root, string file)
    {
        return File.ReadAllText(Path.Combine(root, file));
    }

    private static string RuntimeRoot()
    {
        return Path.Combine(
            FindRepositoryRoot(),
            "Assets",
            "Dig.Unity",
            "Runtime");
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
