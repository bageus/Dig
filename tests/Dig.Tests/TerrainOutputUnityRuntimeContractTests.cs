using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class TerrainOutputUnityRuntimeContractTests
{
    [Fact]
    public void Unity_uses_shared_typed_catalog_and_application_owned_output_commit()
    {
        string root = FindRepositoryRoot();
        string worldSession = File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigWorldSession.cs"));
        string terrainSession = File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigTerrainWorkSession.cs"));
        string composition = File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigTerrainWorkSession.Composition.cs"));
        string playMode = File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Tests/PlayMode/TerrainOutputCatalogPlayModeTests.cs"));

        Assert.Contains("DefaultTerrainMaterials.CreateCatalog()", worldSession);
        Assert.Contains("CompleteTerrainWorkCommand.FromPlan", terrainSession);
        Assert.DoesNotContain("_miningOutputCommits.Record", terrainSession);
        Assert.Contains("MiningOutputCommitState commits", composition);
        Assert.Contains("skills,\n                commits)", composition);
        Assert.Contains(
            "Demo_uses_six_typed_terrain_profiles_with_raw_ore_outputs",
            playMode);
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
