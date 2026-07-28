using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace Dig.Tests
{
public sealed class PlayModeRuntimeVisibilityContractTests
{
    [Fact]
    public void Play_mode_fixtures_do_not_directly_access_internal_runtime_members()
    {
        string playMode = Path.Combine(
            FindRepositoryRoot(),
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Tests",
            "PlayMode");
        string barrel = File.ReadAllText(Path.Combine(
            playMode,
            "BarrelDestructionPlayModeTests.cs"));
        string mushroom = File.ReadAllText(Path.Combine(
            playMode,
            "MushroomDepthProjectionPlayModeTests.cs"));

        Assert.Contains("using System;", barrel);
        Assert.DoesNotContain("DigTunnelProjection.", barrel);
        Assert.Contains("GetProjectionConstant(\"DepthOrigin\")", barrel);
        Assert.Contains("GetProjectionConstant(\"DepthSpacing\")", barrel);
        Assert.DoesNotContain("value.Model", mushroom);
        Assert.DoesNotContain("visual.Model", mushroom);
        Assert.Contains("GetProperty(visual, \"Model\")", mushroom);
        Assert.Empty(Regex.Matches(
            barrel,
            @"(?<!UnityEngine\.)\bObject\.Destroy\("));
        Assert.Equal(3, Regex.Matches(
            barrel,
            @"UnityEngine\.Object\.Destroy\(").Count);
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
