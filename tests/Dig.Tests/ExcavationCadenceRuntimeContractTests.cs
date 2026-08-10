using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class ExcavationCadenceRuntimeContractTests
{
    [Fact]
    public void All_excavation_paths_use_one_deterministic_cadence_owner()
    {
        string runtime = RuntimeRoot();
        string session = Read(runtime, "DigTerrainWorkSession.cs");
        string spatial = Read(runtime, "DigTerrainSpatialExcavation.cs");
        string quarters = Read(runtime, "DigTerrainWorkExcavationQuarters.cs");
        string cadence = Read(runtime, "DigTerrainWorkExcavationCadence.cs");

        Assert.DoesNotContain("tick%3", session);
        Assert.DoesNotContain("SpatialExcavationWorkCadence", spatial);
        Assert.Contains("ResolveExcavationCadence", quarters);
        Assert.Contains("ExcavationCadenceResolver.IsDue", quarters);
        Assert.Contains("_excavationCadenceResolver.Resolve", cadence);
        Assert.Contains("target.Hardness", cadence);
        Assert.Contains("ResolveMiningWorkInterval", cadence);
        Assert.Contains("TerrainWorkPostureposture", cadence);
        Assert.Contains("_excavationQuarterWork.ApplyWork", quarters);
        Assert.DoesNotContain("ApplySwing(workerId", quarters);
    }

    [Fact]
    public void Quarter_commit_owns_skill_and_finalization_does_not_duplicate_it()
    {
        string root = RepositoryRoot();
        string quarter = Read(root,
            "src/Dig.Application/Jobs/CommitExcavationQuarter.cs");
        string full = Read(root,
            "src/Dig.Application/Jobs/TerrainWorkCompletionUseCases.cs");
        string partial = Read(root,
            "src/Dig.Application/Jobs/PartialTerrainWorkCompletion.cs");
        string playMode = Read(root,
            "Assets/Dig.Unity/Tests/PlayMode/"
                + "ExcavationCadenceProfilesPlayModeTests.cs");

        Assert.Contains("SkillGrantSourceKind.ExcavationQuarterCommitted", quarter);
        Assert.Contains("committed.Value.ChangedCellCount>0", quarter);
        Assert.DoesNotContain("SkillGrantBundle", full);
        Assert.DoesNotContain("ApplyConfirmed", full);
        Assert.DoesNotContain("SkillGrantBundle", partial);
        Assert.DoesNotContain("ApplyConfirmed", partial);
        Assert.Contains("Cadence_and_quarter_grants_are_deterministic", playMode);
    }

    private static string Read(string root, string relative)
    {
        return Normalize(File.ReadAllText(Path.Combine(root, relative)));
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
