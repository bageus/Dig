using System.IO;
using Xunit;

namespace Dig.Tests;

public sealed class DepthDesignationOverlayContractTests
{
    [Fact]
    public void World_overlay_projects_designations_to_authoritative_depth()
    {
        string root = FindRepositoryRoot();
        string render = File.ReadAllText(Path.Combine(
            root,
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime",
            "DigWorldOverlayRenderer.Render.cs"));
        string renderer = File.ReadAllText(Path.Combine(
            root,
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime",
            "DigWorldOverlayRenderer.cs"));

        Assert.Contains("PlaceCellAtDepth(marker, cell.X, cell.Y, cell.Z", render);
        Assert.Contains("DigTunnelProjection.FloorWorldPosition(new CellId(x, y, z))", renderer);
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
