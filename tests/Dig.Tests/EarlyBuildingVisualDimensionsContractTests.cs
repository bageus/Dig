using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Xunit;

namespace Dig.Tests
{

public sealed class EarlyBuildingVisualDimensionsContractTests
{
    [Fact]
    public void Early_building_visual_profiles_have_exact_sizes_and_distinct_silhouettes()
    {
        using JsonDocument document = JsonDocument.Parse(Read(
            "unity/Dig.Unity/Assets/Dig.Unity/Resources/Dig/VisualCatalogs/RepresentativeBuildings.json"));

        AssertProfile(
            document,
            "building.tent",
            3.0f,
            2.0f,
            2.0f,
            "Tent Roof Left",
            "Tent Roof Right",
            "Tent Entrance Flap");
        AssertProfile(
            document,
            "building.stone_mason",
            3.5f,
            2.5f,
            2.5f,
            "Stone Foundation",
            "Stone Workbench",
            "Mason Roof");
        AssertProfile(
            document,
            "building.wood_workshop",
            2.5f,
            2.0f,
            2.0f,
            "Wood Foundation",
            "Saw Bench",
            "Timber Log");
    }

    [Fact]
    public void Visual_dimensions_do_not_change_authoritative_building_footprints()
    {
        BuildingDefinitionId[] ids =
        {
            CampfireProductionContent.TentBuildingId,
            CampfireProductionContent.StoneMasonBuildingId,
            CampfireProductionContent.WoodWorkshopBuildingId,
        };
        BuildingDefinition[] definitions = CampfireProductionContent.CreateBuildings()
            .Where(value => ids.Contains(value.Id))
            .ToArray();

        Assert.Equal(ids.Length, definitions.Length);
        Assert.All(definitions, definition =>
        {
            Assert.Single(definition.Footprint);
            Assert.Equal(new CellOffset(0, 0), definition.Footprint[0]);
        });
    }

    [Fact]
    public void Runtime_collider_and_ghost_use_profile_visual_bounds_and_completed_geometry()
    {
        string data = ReadRuntime("DigRepresentativeBuildingData.cs");
        string templates = ReadRuntime(
            "DigRepresentativeBuildingPrefabLibrary.Templates.cs");
        string ghost = ReadRuntime(
            "DigBuildingBoxGhostRenderer.Representatives.cs");
        string playMode = Read(
            "unity/Dig.Unity/Assets/Dig.Unity/Tests/PlayMode/EarlyBuildingVisualDimensionsPlayModeTests.cs");

        Assert.Contains("visualBoundsCenter", data);
        Assert.Contains("visualBoundsSize", data);
        Assert.Contains("selection.center = profile.visualBoundsCenter", templates);
        Assert.Contains("selection.size = profile.visualBoundsSize", templates);
        Assert.Contains("BuildingVisualState.Completed", ghost);
        Assert.Contains("Completed_profiles_match_declared_visual_bounds", playMode);
        Assert.Contains("Building_box_profiles_remain_compact", playMode);
    }

    private static void AssertProfile(
        JsonDocument document,
        string stableId,
        float width,
        float height,
        float depth,
        params string[] requiredParts)
    {
        JsonElement profile = document.RootElement.GetProperty("profiles")
            .EnumerateArray()
            .Single(value => value.GetProperty("stableIds")
                .EnumerateArray()
                .Any(id => id.GetString() == stableId));
        JsonElement footprint = profile.GetProperty("footprintSize");
        JsonElement center = profile.GetProperty("visualBoundsCenter");
        JsonElement size = profile.GetProperty("visualBoundsSize");

        Assert.Equal(1, footprint.GetProperty("x").GetInt32());
        Assert.Equal(1, footprint.GetProperty("y").GetInt32());
        AssertClose(width, size.GetProperty("x").GetSingle());
        AssertClose(height, size.GetProperty("y").GetSingle());
        AssertClose(depth, size.GetProperty("z").GetSingle());
        AssertClose(0f, center.GetProperty("y").GetSingle() - (height * 0.5f));

        string[] names = profile.GetProperty("parts")
            .EnumerateArray()
            .Select(value => value.GetProperty("name").GetString() ?? string.Empty)
            .ToArray();
        Assert.All(requiredParts, name => Assert.Contains(name, names));
    }

    private static void AssertClose(float expected, float actual)
    {
        Assert.InRange(Math.Abs(expected - actual), 0f, 0.001f);
    }

    private static string ReadRuntime(string file)
    {
        return Read("unity/Dig.Unity/Assets/Dig.Unity/Runtime/" + file);
    }

    private static string Read(string relative)
    {
        return File.ReadAllText(Path.Combine(FindRepositoryRoot(), relative));
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
