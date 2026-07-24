using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentExcavationFeedbackContractTests
{
    [Fact]
    public void Selected_resident_hover_uses_a_shovel_cursor_for_excavation()
    {
        string runtime = RuntimeRoot();
        string cursorLoop = File.ReadAllText(Path.Combine(
            runtime,
            "DigWorldInteraction.ExcavationCursor.cs"));
        string cursor = File.ReadAllText(Path.Combine(
            runtime,
            "DigWorldInteraction.DirectCommandCursor.cs"));

        Assert.Contains("UpdateSelectedResidentCommandCursor();", cursorLoop);
        Assert.Contains("_agentRenderer.SelectedCount > 0", cursor);
        Assert.Contains("TryResolveExplicitExcavationHoverTarget", cursor);
        Assert.Contains("CreateShovelCursorFrames", cursor);
        Assert.Contains("Cursor.SetCursor", cursor);
    }

    [Fact]
    public void Mining_faces_the_target_and_vertical_movement_uses_a_climb_pose()
    {
        string runtime = RuntimeRoot();
        string equipment = File.ReadAllText(Path.Combine(
            runtime,
            "DigAgentSimulationDriverBase.Equipment.cs"));
        string facing = File.ReadAllText(Path.Combine(
            runtime,
            "DigAgentRenderer.WorkFacing.cs"));
        string visual = File.ReadAllText(Path.Combine(
            runtime,
            "DigAgentVisual.WorkFacing.cs"));
        string movement = File.ReadAllText(Path.Combine(
            runtime,
            "DigAgentVisual.Movement.cs"));
        string rig = File.ReadAllText(Path.Combine(
            runtime,
            "DigResidentRig.cs"));

        Assert.Contains(
            "AgentRenderer.SynchronizeWorkFacing(TerrainSession.LoadJobs());",
            equipment);
        Assert.Contains("JobStageKind.PerformWork", facing);
        Assert.Contains("JobToolKind.Mining", facing);
        Assert.Contains("SetWorkTarget", facing);
        Assert.Contains("DigTunnelProjection.CellWorldPosition", visual);
        Assert.Contains("FaceAwayFromMainCamera", movement);
        Assert.Contains("ClimbWallDepthOffset", movement);
        Assert.Contains("ApplyClimbPose", movement);
        Assert.Contains("ApplyClimbPose", rig);
    }

    [Fact]
    public void Command_cursor_and_depth_jobs_use_world_targets_without_circles()
    {
        string root = FindRepositoryRoot();
        string runtime = RuntimeRoot();
        string cursor = File.ReadAllText(Path.Combine(
            runtime,
            "DigWorldInteraction.DirectCommandCursor.cs"));
        string textures = File.ReadAllText(Path.Combine(
            runtime,
            "DigWorldInteraction.DirectCommandCursor.Textures.cs"));
        string movement = File.ReadAllText(Path.Combine(
            runtime,
            "DigWorldInteraction.TunnelMovement.cs"));
        string excavationCursor = File.ReadAllText(Path.Combine(
            runtime,
            "DigWorldInteraction.ExcavationCursor.cs"));
        string jobRenderer = File.ReadAllText(Path.Combine(
            runtime,
            "DigJobRenderer.cs"));
        string activity = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Dig.Presentation.Abstractions",
            "Agents",
            "ResidentActivityPresenter.cs"));

        Assert.Contains("TryResolvePickableItemHoverTarget", cursor);
        Assert.Contains("DirectCommandCursorKind.Pickup", cursor);
        Assert.Contains("PlayMovementCursorFeedback", cursor);
        Assert.Contains("DirectCommandCursorKind.Movement", cursor);
        Assert.Contains("CreatePickupCursorFrames", textures);
        Assert.Contains("CreateMovementCursorFrames", textures);
        Assert.Contains("CreateShovelCursorFrames", textures);
        Assert.Contains("PlayMovementCursorFeedback();", movement);
        Assert.Contains("TryGetDepthDesignation(hit", movement);
        Assert.Contains("directDepthCommand", excavationCursor);
        Assert.Contains("ShouldRenderWorldMarker", jobRenderer);
        Assert.Contains("model.TargetZ.Value <= 0", jobRenderer);
        Assert.Contains("definition is SpatialDigJobDefinition", activity);
        Assert.Contains("JobToolKind.Mining", activity);
    }

    [Fact]
    public void Building_boxes_are_selectable_buildings_with_alt_pickup_feedback()
    {
        string runtime = RuntimeRoot();
        string priority = File.ReadAllText(Path.Combine(
            runtime,
            "DigWorldInteraction.ResidentCommandPriority.cs"));
        string cursor = File.ReadAllText(Path.Combine(
            runtime,
            "DigWorldInteraction.DirectCommandCursor.cs"));
        string roster = File.ReadAllText(Path.Combine(
            runtime,
            "DigGameHudCanvas.Roster.cs"));
        string management = File.ReadAllText(Path.Combine(
            runtime,
            "DigGameHudCanvas.ManagementBuildings.cs"));
        string gravity = File.ReadAllText(Path.Combine(
            runtime,
            "DigTerrainWorkSession.WorldItemGravity.cs"));

        Assert.Contains("TryResolveBuildingBoxHit", priority);
        Assert.Contains("IsAltPressed()", priority);
        Assert.Contains("SelectBuildingBox(buildingBox.Model)", priority);
        Assert.Contains("TryResolveBuildingBoxHoverTarget", cursor);
        Assert.Contains("SelectBuildingBoxFromHud", roster);
        Assert.Contains("SelectBuildingBoxFromManagement", management);
        Assert.Contains("WorldItemGravityPolicy.ResolveLandingCell", gravity);
        Assert.Contains("MoveAvailable", gravity);
    }

    private static string RuntimeRoot()
    {
        return Path.Combine(
            FindRepositoryRoot(),
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime");
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
