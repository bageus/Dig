using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class CampfireOperationHudContractTests
{
    [Fact]
    public void Building_context_shows_authoritative_iteration_progress()
    {
        string root = FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            root,
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime",
            "DigGameHudCanvas.Context.cs"));

        Assert.Contains("LoadPackableBuildingExecutions(tick)", source);
        Assert.Contains("operation.IterationProgressBasisPoints", source);
        Assert.Contains("operation.CurrentIteration", source);
        Assert.Contains("operation.TotalIterations", source);
        Assert.Contains("operation.ActiveWorkerId", source);
        Assert.Contains("operation?.IsInterrupted == true", source);
        Assert.Contains("SetButtonActive(pack, operation != null && !operation.IsTerminal)", source);
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