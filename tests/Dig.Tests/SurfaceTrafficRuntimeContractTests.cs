using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class SurfaceTrafficRuntimeContractTests
{
    [Fact]
    public void Every_surface_corridor_reserves_pose_before_confirming_progress()
    {
        string runtime = RuntimeRoot();
        string traffic = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigAgentSession.SurfaceTraffic.cs")));
        string automatic = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigAgentSession.AutomaticSurfaceCorridor.cs")));
        string manual = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigAgentSession.SurfaceCorridor.cs")));
        string vertical = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigAgentSession.VerticalSurfaceCorridor.cs")));

        Assert.Contains("_surfaceTraffic.RecordPose", traffic);
        Assert.Contains("_surfaceTraffic.CanOccupy", automatic);
        Assert.Contains("MoveOnReservedSurface(agent,exitPose)", automatic);
        Assert.Contains("MoveOnReservedSurface(agent,entryPose)", automatic);
        Assert.Contains("MoveOnReservedSurface(agent,exitPose)", manual);
        Assert.Contains("MoveOnReservedSurface(agent,entryPose)", manual);
        Assert.Contains("MoveOnReservedSurface(agent,verticalPose)", vertical);
    }

    private static string RuntimeRoot()
    {
        return Path.Combine(
            FindRepositoryRoot(),
            "Assets",
            "Dig.Unity",
            "Runtime");
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
