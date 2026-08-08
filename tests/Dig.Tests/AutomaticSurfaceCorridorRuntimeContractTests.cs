using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class AutomaticSurfaceCorridorRuntimeContractTests
{
    [Fact]
    public void Automatic_and_spatial_work_movement_share_surface_corridor()
    {
        string root = RepositoryRoot();
        string movement = Normalize(File.ReadAllText(Path.Combine(
            root,
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/"
                + "DigAgentSession.AutomaticSurfaceCorridor.cs")));
        string spatial = Normalize(File.ReadAllText(Path.Combine(
            root,
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigAgentSession.SpatialWorkMovement.cs")));
        string corridor = Normalize(File.ReadAllText(Path.Combine(
            root,
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigAgentSession.SurfaceCorridor.cs")));

        Assert.Contains("SurfaceCorridorSteering.TryBuildBoundaryPoses(", movement);
        Assert.Contains("MoveOnReservedSurface(agent,exitPose)", movement);
        Assert.Contains("MoveOnReservedSurface(agent,entryPose)", movement);
        Assert.Contains("MoveOnReservedSurface(agent,exitPose)", corridor);
        Assert.Contains("_automaticBoundaryApproaches.Remove(agent.Id)", movement);
        Assert.Contains("MoveThroughTunnelTraffic(agent,next)", spatial);
        Assert.DoesNotContain("agent.MoveTo(next,_tick)", spatial);
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "Dig.sln")))
        {
            current = current.Parent;
        }

        return current?.FullName
            ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }

    private static string Normalize(string value)
    {
        return value
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\t", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
    }
}

}
