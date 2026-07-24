using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class CampfireExecutionRuntimeContractTests
{
    [Fact]
    public void Assembly_and_packing_runtime_record_authoritative_iterations_once()
    {
        string root = FindRepositoryRoot();
        string runtime = Path.Combine(
            root,
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime");
        string assembly = File.ReadAllText(Path.Combine(
            runtime,
            "DigBuildingBoxAssemblyExecution.cs"));
        string packing = File.ReadAllText(Path.Combine(
            runtime,
            "DigBuildingPackingExecution.cs"));

        Assert.Contains("PackableBuildingExecutionRegistry", assembly);
        Assert.Contains("PackableBuildingOperationKind.Unpack", assembly);
        Assert.Contains("StartOrResume(assembly.Id, workerId)", assembly);
        Assert.Contains("CompleteIteration(assembly.Id, workerId)", assembly);
        Assert.True(
            assembly.IndexOf("_buildingBoxAssemblyWork!.Handle", StringComparison.Ordinal)
            < assembly.IndexOf("CompleteIteration(assembly.Id, workerId)", StringComparison.Ordinal));

        Assert.Contains("PackableBuildingOperationKind.Pack", packing);
        Assert.Contains("StartOrResume(jobId, workerId)", packing);
        Assert.Contains("CompleteIteration(jobId, workerId)", packing);
        Assert.True(
            packing.IndexOf("_buildingPackingWork!.Handle", StringComparison.Ordinal)
            < packing.IndexOf("CompleteIteration(jobId, workerId)", StringComparison.Ordinal));
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
