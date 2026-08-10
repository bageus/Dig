using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class MiningOutputSaveRuntimeContractTests
{
    [Fact]
    public void Unity_terrain_session_accepts_the_restored_exactly_once_owner()
    {
        string root = FindRepositoryRoot();
        string session = File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigTerrainWorkSession.cs"));
        string composition = File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/"
                + "DigTerrainWorkSession.Composition.cs"));

        Assert.Contains(
            "private readonly MiningOutputCommitState _miningOutputCommits;",
            session,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "_miningOutputCommits = new MiningOutputCommitState();",
            session,
            StringComparison.Ordinal);
        Assert.Contains(
            "_miningOutputCommits = miningOutputCommits ?? new MiningOutputCommitState();",
            session,
            StringComparison.Ordinal);
        Assert.Contains(
            "MiningOutputCommitState? miningOutputCommits = null",
            composition,
            StringComparison.Ordinal);
        Assert.Contains(
            "MiningOutputCommitState commits =",
            composition,
            StringComparison.Ordinal);
        Assert.Contains(
            "skills,\n                commits)",
            composition,
            StringComparison.Ordinal);
        Assert.Contains(
            "skills,\n            commits);",
            composition,
            StringComparison.Ordinal);
        Assert.Contains(
            "CompleteTerrainWorkCommand.FromPlan(",
            session,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "_miningOutputCommits.Record(output",
            session,
            StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Dig.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}

}
