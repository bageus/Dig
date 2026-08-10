using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
public sealed class StylizedMaterialRuntimeContractTests
{
    [Fact]
    public void Runtime_material_tint_uses_explicit_shader_properties()
    {
        string root = FindRepositoryRoot();
        string runtime = Path.Combine(
            root,
            "Assets",
            "Dig.Unity",
            "Runtime");
        string utility = File.ReadAllText(Path.Combine(
            runtime,
            "DigMaterialColorUtility.cs"));
        string library = File.ReadAllText(Path.Combine(
            runtime,
            "DigRenderMaterialLibrary.cs"));
        string barrel = File.ReadAllText(Path.Combine(
            runtime,
            "DigBarrelVisual.cs"));
        string resident = File.ReadAllText(Path.Combine(
            runtime,
            "DigAgentVisual.cs"));
        string itemHover = File.ReadAllText(Path.Combine(
            runtime,
            "DigWorldItemHoverExtensions.cs"));

        Assert.Contains("Shader.PropertyToID(\"_BaseColor\")", utility);
        Assert.Contains("Shader.PropertyToID(\"_Color\")", utility);
        Assert.Contains("material.HasProperty(BaseColorId)", utility);
        Assert.Contains("material.GetColor(BaseColorId)", utility);
        Assert.Contains("material.SetColor(BaseColorId, color)", utility);
        Assert.Contains("DigMaterialColorUtility.SetColor(material, tint)", library);
        Assert.Contains("DigMaterialColorUtility.GetColor(material, Color.white)", barrel);
        Assert.Contains("DigMaterialColorUtility.GetColor", resident);
        Assert.Contains("DigMaterialColorUtility.GetColor", itemHover);
        Assert.DoesNotContain("color = tint", library);
        Assert.DoesNotContain("material.color", barrel);
        Assert.DoesNotContain("sharedMaterial.color", resident);
        Assert.DoesNotContain("sharedMaterial.color", itemHover);
    }

    [Fact]
    public void Optional_visual_catalog_fallback_is_quiet_but_authored_catalogs_are_validated()
    {
        string diagnostics = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "Assets",
            "Dig.Unity",
            "Runtime",
            "DigVisualCatalogDiagnostics.cs"));

        Assert.Contains("if (catalog == null)", diagnostics);
        Assert.Contains("catalog.ValidateCatalog()", diagnostics);
        Assert.Contains("Debug.LogError", diagnostics);
        Assert.DoesNotContain("Debug.LogWarning", diagnostics);
        Assert.DoesNotContain("visual catalog is not assigned", diagnostics);
        Assert.DoesNotContain("runtime fallback visuals remain active", diagnostics);
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
