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
        Assert.Contains("CreateShovelCursorTexture", cursor);
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
