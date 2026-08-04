using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class FullDepthEatingTentSourceContractTests
{
    [Fact]
    public void Z0_to_Z3_use_one_full_depth_cell_contract()
    {
        string projection = ReadRuntime("DigTunnelProjection.cs");
        string geometry = ReadRuntime("DigTerrainChunkMeshBuilder.Geometry.cs");

        Assert.Contains("DepthOrigin = 0.50f", projection);
        Assert.Contains("DepthSpacing = -1.00f", projection);
        Assert.Contains("RockCellHalfExtent = 0.50f", projection);
        Assert.Contains("FloorDepth = 1.00f", projection);
        Assert.DoesNotContain("if (z == 0)", geometry);
        Assert.DoesNotContain("FrontRockDepth", geometry);
    }

    [Fact]
    public void Eating_uses_a_seated_bite_pose_and_collider_free_meal_portion()
    {
        string visualModels = ReadSource(
            "src", "Dig.Presentation.Abstractions", "Agents", "ResidentVisualModels.cs");
        string presenter = ReadSource(
            "src", "Dig.Presentation.Abstractions", "Agents", "ResidentVisualPresenter.cs");
        string eating = ReadRuntime("DigAgentVisual.Eating.cs");
        string hands = ReadRuntime("DigAgentVisual.HandTools.cs");
        string rig = ReadRuntime("DigResidentRig.cs");
        string equipment = ReadRuntime("DigAgentEquipmentVisual.cs");

        Assert.Contains("Eat = 11", visualModels);
        Assert.Contains("ResidentActionVisualState.Eat", presenter);
        Assert.Contains("EatingBitePeriodSeconds", eating);
        Assert.Contains("Model.ActionProgress", eating);
        Assert.Contains("MealVisualId", hands);
        Assert.Contains("transform.localPosition = new Vector3(0f, -0.31f, 0f)", rig);
        Assert.Contains("Meal Portion", equipment);
        Assert.Contains("Destroy(collider)", equipment);
    }

    [Fact]
    public void Tent_profiles_face_the_camera_without_changing_bounds_or_footprint()
    {
        string profiles = ReadRuntime("DigBuildingVisualProfile.cs");
        string library = ReadRuntime("DigRepresentativeBuildingPrefabLibrary.cs");
        string templates = ReadRuntime("DigRepresentativeBuildingPrefabLibrary.Templates.cs");
        string visual = ReadRuntime("DigBuildingVisual.cs");
        string builtIns = ReadRuntime(
            "DigRepresentativeBuildingPrefabLibrary.BuiltInProfiles.cs");

        Assert.Contains("FacesCamera", profiles);
        Assert.Contains("kind == DigBuildingProfileKind.Tent", profiles);
        Assert.Contains("profileKind == DigBuildingProfileKind.Tent", templates);
        Assert.Contains("Quaternion.Euler(0f, 180f, 0f)", templates);
        Assert.Contains("resolution.FacesCamera", visual);
        Assert.Contains("new Vector3(3f, 2f, 2f)", builtIns);
        Assert.Contains("Vector2Int.one", builtIns);
        Assert.Contains("facesCamera: facesCamera", library);
    }

    [Fact]
    public void Unmineable_front_columns_are_propagated_after_resources()
    {
        string generator = ReadSource(
            "src", "Dig.Domain", "Generation", "WorldGenerator.cs");
        string demo = ReadRuntime("DigWorldSession.TerrainDemo.cs");

        Assert.Contains("PropagateFrontUnmineableColumns(buffer, request.Materials);", generator);
        Assert.Contains("!material.IsSolid || material.IsMineable", generator);
        Assert.Contains("buffer.Get(deepCell).WithTerrain(front.MaterialId)", generator);
        Assert.Contains("z < world.Size.Depth", demo);
        Assert.DoesNotContain("z <= 1", demo);
    }

    private static string ReadRuntime(string file)
    {
        return ReadSource(
            "unity", "Dig.Unity", "Assets", "Dig.Unity", "Runtime", file);
    }

    private static string ReadSource(params string[] parts)
    {
        string path = FindRepositoryRoot();
        for (int index = 0; index < parts.Length; index++)
        {
            path = Path.Combine(path, parts[index]);
        }

        return File.ReadAllText(path);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src"))
                && Directory.Exists(Path.Combine(current.FullName, "unity")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}

}
