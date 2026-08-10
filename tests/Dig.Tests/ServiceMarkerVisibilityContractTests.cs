using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class ServiceMarkerVisibilityContractTests
{
    [Fact]
    public void Normal_gameplay_hides_job_cylinders_and_building_overhead_selection()
    {
        string root = FindRepositoryRoot();
        string defaults = Read(root,
            "src/Dig.Presentation.Abstractions/Overlays/DefaultOverlayDefinitions.cs");
        string jobRenderer = Read(root,
            "Assets/Dig.Unity/Runtime/DigJobRenderer.cs");
        string selection = Read(root,
            "Assets/Dig.Unity/Runtime/DigWorldOverlayRenderer.Render.cs");

        Assert.Contains(
            "OverlayLayerKind.Jobs,500,false,false,3",
            Normalize(defaults));
        Assert.Contains("!model.IsTunnelInfrastructure", jobRenderer);
        Assert.DoesNotContain("Building Selection", selection);
    }

    [Fact]
    public void Item_interaction_uses_an_invisible_box_collider_not_rendered_cylinder()
    {
        string root = FindRepositoryRoot();
        string itemVisual = Read(root,
            "Assets/Dig.Unity/Runtime/DigWorldItemVisual.cs");

        Assert.Contains("RequireComponent(typeof(BoxCollider))", itemVisual);
        Assert.Contains("_interactionCollider.isTrigger = true", itemVisual);
        Assert.DoesNotContain("PrimitiveType.Cylinder", itemVisual);
    }

    private static string Read(string root, string relativePath) =>
        File.ReadAllText(Path.Combine(
            root,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string Normalize(string source) => source
        .Replace(" ", string.Empty, StringComparison.Ordinal)
        .Replace("\t", string.Empty, StringComparison.Ordinal)
        .Replace("\r", string.Empty, StringComparison.Ordinal)
        .Replace("\n", string.Empty, StringComparison.Ordinal);

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
