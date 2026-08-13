using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class RuntimeRegressionCorrectionSourceTests
{
    [Fact]
    public void Resident_steps_fill_the_tick_without_per_cell_recentering()
    {
        string loop = ReadRuntime("DigAgentSimulationDriverBase.Loop.cs");
        string visual = ReadRuntime("DigAgentVisual.cs");

        Assert.Contains("float movementDuration = TickIntervalSeconds;", loop);
        Assert.DoesNotContain("RecenterAfterHorizontalMovement", visual);
        Assert.Contains("_directionalLaneOffsetX = 0f;", visual);
    }

    [Fact]
    public void Surface_designation_does_not_tint_the_whole_terrain_cube_green()
    {
        string cell = ReadRuntime("DigCellVisual.cs");

        Assert.Contains("_baseColor = baseColor;", cell);
        Assert.DoesNotContain("TunnelDesignationColor", cell);
    }

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

    [Fact]
    public void Direct_commands_cancel_hauling_and_general_hover_shows_world_names()
    {
        string direct = ReadRuntime("DigTerrainWorkSession.DirectCommands.cs");
        string hover = ReadRuntime("DigWorldInteraction.ContextHover.cs");
        string cursor = ReadRuntime("DigWorldInteraction.DirectCommandCursor.cs");
        string canvas = ReadRuntime("DigGameHudCanvas.ContextHover.cs");

        Assert.Contains("HaulJobDefinition", direct, StringComparison.Ordinal);
        Assert.Contains("CancelHaulingForDirectCommand", direct, StringComparison.Ordinal);
        Assert.Contains("ReleaseRoomUpgradeAssignment(job,tick)", direct,
            StringComparison.Ordinal);
        Assert.DoesNotContain("_haulingCancellation", direct, StringComparison.Ordinal);
        Assert.Contains("SetGeneralWorldTargetHoverInfo(hits)", cursor,
            StringComparison.Ordinal);
        Assert.Contains("item.Model.DisplayName", hover, StringComparison.Ordinal);
        Assert.Contains("building.Model.Name", hover, StringComparison.Ordinal);
        Assert.Contains("SetHostileTargetHoverInfo(creature)", hover,
            StringComparison.Ordinal);
        Assert.Contains("resident.Model.Name", hover, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectedCount != 1", canvas, StringComparison.Ordinal);
    }

    private static string ReadRuntime(string fileName)
    {
        string? current = AppContext.BaseDirectory;
        while (current != null)
        {
            string candidate = Path.Combine(
                current,
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
