using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class EarlyBuildingVisualDimensionsContractTests
{
    [Fact]
    public void Early_building_visual_profiles_have_exact_sizes_and_distinct_silhouettes()
    {
        using JsonDocument document = JsonDocument.Parse(Read(
            "Assets/Dig.Unity/Resources/Dig/VisualCatalogs/RepresentativeBuildings.json"));

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
    public void Visual_dimensions_match_authoritative_xyz_building_volumes()
    {
        AssertVolume(CampfireProductionContent.TentBuildingId, 3, 2);
        AssertVolume(CampfireProductionContent.StoneMasonBuildingId, 4, 3);
        AssertVolume(CampfireProductionContent.WoodWorkshopBuildingId, 3, 2);
    }

    [Fact]
    public void Placement_rejects_visual_volume_overlap_and_depth_overflow()
    {
        BuildingDefinition workshop = CampfireProductionContent.CreateBuildings()
            .Single(value => value.Id == CampfireProductionContent.WoodWorkshopBuildingId);
        MaterialId air = new MaterialId("placement.air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(air, isSolid: false, hardness: 0),
        });
        WorldSnapshot world = WorldState.CreateFilled(
            new WorldSize(10, 10, 4),
            chunkSize: 5,
            materials,
            air,
            explored: true).Value.CreateSnapshot();
        BuildingPlacementValidator validator = new BuildingPlacementValidator();

        BuildingPlacementResult overlap = validator.Validate(
            workshop,
            new CellId(5, 5, 1),
            BuildingOrientation.North,
            world,
            new[] { new CellId(6, 5, 2) },
            new[] { new CellId(3, 5, 1) });
        BuildingPlacementResult overflow = validator.Validate(
            workshop,
            new CellId(5, 5, 3),
            BuildingOrientation.North,
            world,
            Array.Empty<CellId>(),
            new[] { new CellId(3, 5, 3) });

        Assert.Equal(BuildingErrors.PlacementOccupied, overlap.Error);
        Assert.Equal(BuildingErrors.PlacementOutOfBounds, overflow.Error);
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
            "Assets/Dig.Unity/Tests/PlayMode/EarlyBuildingVisualDimensionsPlayModeTests.cs");

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

        Assert.Equal((int)Math.Ceiling(width), footprint.GetProperty("x").GetInt32());
        Assert.Equal((int)Math.Ceiling(depth), footprint.GetProperty("y").GetInt32());
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

    private static void AssertVolume(
        BuildingDefinitionId id,
        int width,
        int depth)
    {
        BuildingDefinition definition = CampfireProductionContent.CreateBuildings()
            .Single(value => value.Id == id);
        CellId origin = new CellId(10, 5, 0);
        CellId[] cells = definition.ResolveFootprint(
            origin,
            BuildingOrientation.North).ToArray();

        Assert.Equal(width * depth, cells.Length);
        Assert.Equal(width, cells.Select(value => value.X).Distinct().Count());
        Assert.Equal(depth, cells.Select(value => value.Z).Distinct().Count());
        Assert.All(cells, value => Assert.Equal(origin.Y, value.Y));
    }

    private static void AssertClose(float expected, float actual)
    {
        Assert.InRange(Math.Abs(expected - actual), 0f, 0.001f);
    }

    private static string ReadRuntime(string file)
    {
        return Read("Assets/Dig.Unity/Runtime/" + file);
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
