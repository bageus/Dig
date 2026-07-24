using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class PackableBuildingPresentationRuntimeContractTests
{
    [Fact]
    public void Unity_session_exposes_data_driven_packable_progress_and_restore_boundary()
    {
        string root = FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            root,
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime",
            "DigPackableBuildingPresentation.cs"));

        Assert.Contains("LoadPackableBuildingExecutions", source);
        Assert.Contains("CapturePackableBuildingExecutions", source);
        Assert.Contains("RestorePackableBuildingExecutions", source);
        Assert.Contains("CampfireBuildingBoxContent.Catalog", source);
        Assert.Contains("visual.campfire.active", source);
        Assert.Contains("visual.campfire.box.world", source);
        Assert.Contains("visual.campfire.box.inventory", source);
        Assert.Contains("visual.campfire.site.planned", source);
        Assert.Contains("visual.campfire.unpack.partial", source);
        Assert.Contains("visual.campfire.pack.partial", source);
        Assert.Contains("effect.campfire.iteration", source);
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