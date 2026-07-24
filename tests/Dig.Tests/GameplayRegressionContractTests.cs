using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class GameplayRegressionContractTests
{
    [Fact]
    public void Excavation_finalizes_after_work_and_requires_the_exact_depth()
    {
        string runtime = RuntimeRoot();
        string session = Normalize(File.ReadAllText(Path.Combine(
            runtime,
            "DigTerrainWorkSession.cs")));

        Assert.Contains("agent.CellZ==route.WorkCell.Value.Z", session);
        Assert.Contains("updated.Stage==JobStageKind.Finalize", session);
        Assert.Contains("CompleteTerrainJobAtWorkCell(updated,tick)", session);
        Assert.Contains("job.Stage==JobStageKind.Finalize", session);
    }

    [Fact]
    public void Surface_excavation_rebinds_remaining_cells_after_job_reconciliation()
    {
        string runtime = RuntimeRoot();
        string manual = Normalize(File.ReadAllText(Path.Combine(
            runtime,
            "DigTerrainWorkManualExcavation.cs")));
        string state = Normalize(File.ReadAllText(Path.Combine(
            runtime,
            "DigTerrainWorkManualExcavation.State.cs")));

        Assert.Contains("RefreshManualExcavationGroupJobs(group)", manual);
        Assert.Contains("group.TargetCells", manual);
        Assert.Contains("HasPendingManualTargets(group)", manual);
        Assert.Contains("internalIReadOnlyList<CellId>TargetCells", state);
        Assert.Contains("internalvoidAdd(EntityIdjobId)", state);
    }

    [Fact]
    public void Vertical_climbing_uses_authoritative_cell_transition_despite_visual_offset()
    {
        string movement = Normalize(File.ReadAllText(Path.Combine(
            RuntimeRoot(),
            "DigAgentVisual.Movement.cs")));

        Assert.Contains("_previousX==_currentX", movement);
        Assert.Contains("_previousZ==_currentZ", movement);
        Assert.Contains("_previousY!=_currentY", movement);
        Assert.Contains("_climbingAscending=_currentY<_previousY", movement);
        Assert.DoesNotContain("Mathf.Abs(direction.x)<0.001f", movement);
    }

    [Fact]
    public void Movement_cursor_is_cleared_as_soon_as_command_feedback_expires()
    {
        string runtime = RuntimeRoot();
        string cursor = Normalize(File.ReadAllText(Path.Combine(
            runtime,
            "DigWorldInteraction.DirectCommandCursor.cs")));
        string movement = Normalize(File.ReadAllText(Path.Combine(
            runtime,
            "DigWorldInteraction.TunnelMovement.cs")));

        Assert.Contains("Time.unscaledTime<_movementCursorExpiresAt", cursor);
        Assert.Contains("kind==DirectCommandCursorKind.Default", cursor);
        Assert.Contains("Cursor.SetCursor(null,Vector2.zero,CursorMode.Auto)", cursor);
        Assert.Contains("PlayMovementCursorFeedback()", movement);
    }

    [Fact]
    public void Building_box_left_click_starts_cursor_placement_instead_of_drop()
    {
        string runtime = RuntimeRoot();
        string hud = Normalize(File.ReadAllText(Path.Combine(
            runtime,
            "DigGameHudCanvas.Inventory.cs")));
        string interaction = Normalize(File.ReadAllText(Path.Combine(
            runtime,
            "DigWorldInteraction.ResidentInventory.cs")));

        Assert.Contains("slot.CanStartPlacement", hud);
        Assert.Contains("BeginResidentInventoryBuildingPlacement(slot)", hud);
        Assert.Contains("internalvoidBeginResidentInventoryBuildingPlacement", interaction);
        Assert.Contains("ResetInventoryClickSequence()", interaction);
        Assert.Contains("PointerInputSurface.ResidentInventory", interaction);
        Assert.Contains("PointerButtonKind.Left", interaction);
    }

    [Fact]
    public void Building_placement_follows_side_view_cells_and_uses_the_building_silhouette()
    {
        string runtime = RuntimeRoot();
        string interaction = Normalize(File.ReadAllText(Path.Combine(
            runtime,
            "DigWorldInteraction.BuildingBoxes.cs")));
        string ghost = Normalize(File.ReadAllText(Path.Combine(
            runtime,
            "DigBuildingBoxGhostRenderer.cs")));
        string representatives = Normalize(File.ReadAllText(Path.Combine(
            runtime,
            "DigBuildingBoxGhostRenderer.Representatives.cs")));

        Assert.Contains("TryResolveBuildingPlacementOrigin(GetPointerHits()", interaction);
        Assert.Contains("TryResolveTunnelDestination(hit,outorigin,out_)", interaction);
        Assert.Contains("DigTunnelProjection.CellWorldPosition(preview.Origin)", ghost);
        Assert.Contains("DigTunnelProjection.CellWorldPosition(cell)", ghost);
        Assert.Contains("BuildingVisualState.Completed", representatives);
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
