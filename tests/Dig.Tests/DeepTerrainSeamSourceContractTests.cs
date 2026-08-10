using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class DeepTerrainSeamSourceContractTests
{
    [Fact]
    public void Deep_terrain_slices_fill_the_complete_depth_spacing()
    {
        string builder = ReadRuntime("DigTerrainChunkMeshBuilder.cs");
        string geometry = ReadRuntime("DigTerrainChunkMeshBuilder.Geometry.cs");

        Assert.Contains("private const float DepthLayerScale = 1f;", builder);
        Assert.DoesNotContain("DepthLayerScale = 0.94f", builder);
        Assert.Contains(
            "Mathf.Abs(DigTunnelProjection.DepthSpacing)\n                * DepthLayerScale\n                * 0.5f",
            geometry);
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
