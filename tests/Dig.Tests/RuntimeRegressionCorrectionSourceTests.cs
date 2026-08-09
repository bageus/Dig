using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class RuntimeRegressionCorrectionSourceTests
{
    [Fact]
    public void Vertical_idle_recovery_and_centered_climb_are_wired()
    {
        string navigation = ReadRuntime("DigTerrainWorkNavigation.cs");
        string surface = ReadRuntime("DigAgentVisual.SurfacePose.cs");

        Assert.Contains(
            "requireFloorRecovery: agent.SurfaceFace != SurfaceFace.Floor",
            navigation,
            StringComparison.Ordinal);
        Assert.Contains(
            "private const double VerticalTunnelFaceBias = 0.18d;",
            surface,
            StringComparison.Ordinal);
        Assert.DoesNotContain("x -= 0.5d;", surface, StringComparison.Ordinal);
        Assert.DoesNotContain("x += 0.5d;", surface, StringComparison.Ordinal);
    }

    [Fact]
    public void Direct_and_automatic_pickup_accept_supported_floor_pose_in_source_cell()
    {
        string movement = ReadRuntime("DigTerrainSpatialExcavation.Movement.cs");
        string pickup = ReadRuntime("DigWorldItemPickupExecution.cs");

        Assert.Contains(
            "job.Definition is WorldItemPickupJobDefinition",
            movement,
            StringComparison.Ordinal);
        Assert.Contains(
            "job.Definition is HaulJobDefinition",
            movement,
            StringComparison.Ordinal);
        Assert.Contains("HasFullStandingSupport(required.Cell)", movement,
            StringComparison.Ordinal);
        Assert.Contains("IsAtPreciseWorkPose(job, agent)", pickup,
            StringComparison.Ordinal);
    }

    private static string ReadRuntime(string fileName)
    {
        string? current = AppContext.BaseDirectory;
        while (current != null)
        {
            string candidate = Path.Combine(
                current,
                "unity",
                "Dig.Unity",
                "Assets",
                "Dig.Unity",
                "Runtime",
                fileName);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            current = Directory.GetParent(current)?.FullName;
        }

        throw new DirectoryNotFoundException("Unity runtime source root was not found.");
    }
}

}
