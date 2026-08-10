using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
public sealed class SpatialExcavationDesignationRuntimeContractTests
{
    [Fact]
    public void Spatial_excavation_designates_world_before_job_and_swing_progress()
    {
        string runtime = RuntimeRoot();
        string spatial = Normalize(Read(runtime, "DigTerrainSpatialExcavation.cs"));
        string designation = Normalize(Read(
            runtime,
            "DigTerrainSpatialExcavation.Designations.cs"));
        string quarters = Normalize(Read(
            runtime,
            "DigTerrainWorkExcavationQuarters.cs"));
        string sync = Normalize(Read(
            RepositoryRoot(),
            "src/Dig.Application/Jobs/DigDesignationJobSyncHandler.cs"));
        string playMode = Normalize(Read(
            RepositoryRoot(),
            "Assets/Dig.Unity/Tests/PlayMode/"
                + "SpatialExcavationDesignationPlayModeTests.cs"));

        Assert.Contains(
            "Resultdesignated=EnsureSpatialExcavationDesignation(plan.Target,tick);",
            spatial);
        Assert.True(
            spatial.IndexOf("EnsureSpatialExcavationDesignation", StringComparison.Ordinal)
            < spatial.IndexOf("TryGetActiveSpatialJob", StringComparison.Ordinal));
        Assert.Contains("world.SetDigDesignation(target,designated:true,tick)", designation);
        Assert.Contains("activeSpatialTargets.Contains(cellId)", sync);
        const string designationGuard =
            "before.State.Designation!=CellDesignation.Dig";
        int guardIndex = quarters.IndexOf(designationGuard, StringComparison.Ordinal);
        int guardedSwingIndex = quarters.IndexOf(
            "ApplyWork(workerId)",
            guardIndex,
            StringComparison.Ordinal);
        Assert.True(guardIndex >= 0);
        Assert.True(guardedSwingIndex > guardIndex);
        Assert.Contains(
            "Spatial_designation_precedes_first_world_quarter_commit",
            playMode);
        Assert.Contains("DesignateSpatialExcavation", playMode);
        Assert.Contains("AdvanceExcavationQuarterWork", playMode);
    }

    private static string Read(string root, string file)
    {
        return File.ReadAllText(Path.Combine(root, file));
    }

    private static string RuntimeRoot()
    {
        return Path.Combine(
            RepositoryRoot(),
            "Assets",
            "Dig.Unity",
            "Runtime");
    }

    private static string RepositoryRoot()
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

    private static string Normalize(string source)
    {
        return source
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\t", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
    }
}
}
