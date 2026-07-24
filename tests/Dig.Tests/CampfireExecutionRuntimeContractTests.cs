using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class CampfireExecutionRuntimeContractTests
{
    [Fact]
    public void Assembly_and_packing_runtime_gate_and_record_authoritative_iterations_once()
    {
        string root = FindRepositoryRoot();
        string runtime = Path.Combine(
            root,
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime");
        string assembly = Normalize(File.ReadAllText(Path.Combine(
            runtime,
            "DigBuildingBoxAssemblyExecution.cs")));
        string packing = Normalize(File.ReadAllText(Path.Combine(
            runtime,
            "DigBuildingPackingExecution.cs")));
        string shared = Normalize(File.ReadAllText(Path.Combine(
            runtime,
            "DigPackableBuildingExecution.cs")));

        Assert.Contains("PackableBuildingExecutionRegistry", assembly);
        Assert.Contains("PackableBuildingOperationKind.Unpack", assembly);
        Assert.Contains("StartOrResume(assembly.Id,workerId)", assembly);
        Assert.Contains("ExecutePackableBuildingIteration(assembly.Id,workerId,tick", assembly);
        Assert.Contains("_buildingBoxAssemblyWork!.Handle", assembly);

        Assert.Contains("PackableBuildingOperationKind.Pack", packing);
        Assert.Contains("StartOrResume(jobId,workerId)", packing);
        Assert.Contains("ExecutePackableBuildingIteration(jobId,workerId,tick", packing);
        Assert.Contains("_buildingPackingWork!.Handle", packing);

        Assert.Contains("ResolveDurationSeconds(workerId)", shared);
        Assert.Contains("applyAuthoritativeWork()", shared);
        Assert.Contains("_campfireIterationProgression.CompleteIteration", shared);
        Assert.True(
            shared.IndexOf("applyAuthoritativeWork()", StringComparison.Ordinal)
            < shared.IndexOf(
                "_campfireIterationProgression.CompleteIteration",
                StringComparison.Ordinal));
    }

    private static string Normalize(string source)
    {
        return source
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\t", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
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