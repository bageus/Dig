using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class ContextHoverLayoutContractTests
{
    [Fact]
    public void Central_hover_region_is_reserved_without_conditional_content_resizing()
    {
        string contextHover = ReadRuntime("DigGameHudCanvas.ContextHover.cs");
        string layout = ReadRuntime("DigGameHudCanvas.Layout.cs");
        string playMode = ReadPlayMode("ContextHoverLayoutPlayModeTests.cs");

        Assert.Contains("ContextHoverContentOffsetMaxY = -52f", contextHover);
        Assert.Contains("SetActive(_bottomPanel.gameObject.activeSelf)", contextHover);
        Assert.Contains("offsetMax.y = ContextHoverContentOffsetMaxY", contextHover);
        Assert.DoesNotContain("visible ? -52f : -8f", contextHover);
        Assert.Contains("RefreshContextHoverInfo();", layout);
        Assert.Contains("Context_hover_keeps_content_and_icon_geometry_stable", playMode);
    }

    private static string ReadRuntime(string file)
    {
        return Read("unity", "Dig.Unity", "Assets", "Dig.Unity", "Runtime", file);
    }

    private static string ReadPlayMode(string file)
    {
        return Read(
            "unity", "Dig.Unity", "Assets", "Dig.Unity", "Tests", "PlayMode", file);
    }

    private static string Read(params string[] parts)
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
