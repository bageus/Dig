using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingBoxRuntimeLifecycleRegressionContractTests
{
    [Fact]
    public void Placement_uses_supported_navigation_and_holder_owned_jobs()
    {
        string runtime = RuntimeRoot();
        string placement = Read(runtime, "DigBuildingBoxPlacement.Navigation.cs");
        string interaction = Read(runtime, "DigWorldInteraction.BuildingBoxes.cs");
        string confirmation = Read(
            RepositoryRoot(),
            "src/Dig.Application/Buildings/ConfirmBuildingBoxPlacementHandler.cs");
        string execution = Read(
            RepositoryRoot(),
            "src/Dig.Application/Buildings/BuildingBoxAssemblyExecutionPolicy.cs");
        string commit = Read(
            RepositoryRoot(),
            "src/Dig.Application/Buildings/CommitBuildingBoxToSiteHandler.cs");

        Assert.Contains("chunk.WalkableCells", placement);
        Assert.Contains("Where(HasFullStandingSupport)", placement);
        Assert.Contains(
            "GetBuildingPlacementReachableCells(EntityIdsourceStackId",
            placement);
        Assert.Contains("source.Location.OwnerId", placement);
        Assert.Contains("_agentSession!.LoadView()", interaction);
        Assert.Contains("jobs.Claim(command.JobId,source.Location.OwnerId", confirmation);
        Assert.Contains("IsOwnedByResident", execution);
        Assert.Contains("IsOwnedByResident", commit);
    }

    [Fact]
    public void Natural_arrival_is_committed_before_unrelated_tick_work()
    {
        string root = RepositoryRoot();
        string loop = Read(
            RuntimeRoot(),
            "DigAgentSimulationDriverBase.Loop.cs");
        string placement = Read(
            root,
            "src/Dig.Domain/Buildings/BuildingPlacement.cs");
        string playMode = Read(
            root,
            "unity/Dig.Unity/Assets/Dig.Unity/Tests/PlayMode/"
                + "BuildingBoxRuntimeLifecyclePlayModeTests.cs");

        int assembly = loop.IndexOf(
            "AdvanceBuildingBoxAssembly(AgentSession.Tick,agents)",
            StringComparison.Ordinal);
        int excavation = loop.IndexOf(
            "AdvanceReadyManualQuarterExcavations", StringComparison.Ordinal);
        Assert.True(assembly >= 0 && excavation > assembly);
        Assert.Contains(".Where(reachable.Contains)", placement);
        Assert.Contains("OrderByDescending(sideWorkPositionSet.Contains)", placement);
        Assert.DoesNotContain("legacyConfiguredPositionIsReachable", placement);
        Assert.Contains("Held_box_natural_route_commits_immediately_on_arrival", playMode);
        Assert.Contains("AdvanceAssemblyTick(runtime", playMode);
    }

    [Fact]
    public void Direct_move_cancels_box_work_and_refreshes_every_planned_projection()
    {
        string runtime = RuntimeRoot();
        string selection = Read(runtime, "DigWorldInteraction.Selection.cs");
        string movement = Read(runtime, "DigWorldInteraction.TunnelMovement.cs");
        string interaction = Read(runtime, "DigWorldInteraction.cs");
        string cancellation = Read(
            runtime,
            "DigTerrainWorkSession.BuildingBoxDirectCancellation.cs");

        int prepare = selection.IndexOf(
            "PrepareResidentsForDirectCommand", StringComparison.Ordinal);
        int move = selection.IndexOf("MoveResident(", StringComparison.Ordinal);
        Assert.True(prepare >= 0 && move > prepare);
        Assert.Contains("RefreshDirectCommandPresentation()", selection);
        Assert.Contains("RefreshBuildingBoxRelocationPlans()", movement);
        Assert.Contains("RefreshBuildingBoxRelocationPlans()", interaction);
        Assert.Contains("CancelBuildingBoxPlanHandler", cancellation);
        Assert.Contains("inventory.ReleaseReservations(job.Id,tick)", cancellation);
    }

    [Fact]
    public void Relocation_requires_a_supported_adjacent_deposit_position()
    {
        string navigation = Read(
            RuntimeRoot(),
            "DigBuildingBoxRelocationNavigation.cs");

        Assert.Contains("navigation.IsWalkable(candidate)", navigation);
        Assert.Contains("HasFullStandingSupport(candidate)", navigation);
        Assert.Contains("PathFailureReason.InvalidGoal", navigation);
        Assert.Contains("ResolveBuildingBoxRelocationWorkTarget", navigation);
    }

    [Fact]
    public void Buildings_roster_projects_one_box_transformation_instead_of_duplicate_plan()
    {
        string root = RepositoryRoot();
        string models = Read(
            root,
            "src/Dig.Presentation.Abstractions/Buildings/BuildingWorldModels.cs");
        string roster = Read(
            RuntimeRoot(),
            "DigGameHudCanvas.Roster.cs");
        string management = Read(
            RuntimeRoot(),
            "DigGameHudCanvas.ManagementBuildings.cs");
        string transformations = Read(
            RuntimeRoot(),
            "DigGameHudCanvas.BuildingBoxTransformations.cs");

        Assert.Contains("snapshot.BoxPlan?.SourceStackId.ToString()", models);
        Assert.Contains("IsPendingBuildingBoxLifecycle", models);
        Assert.Contains("IndexPendingBuildingBoxTransformations(allBuildings)", roster);
        Assert.Contains("!building.IsPendingBuildingBoxLifecycle", roster);
        Assert.Contains("FormatBuildingBoxTransformationLabel", roster);
        Assert.Contains("IndexPendingBuildingBoxTransformations(allBuildings)", management);
        Assert.Contains("!value.IsPendingBuildingBoxLifecycle", management);
        Assert.Contains("BuildingBoxCommitState.AtSite", transformations);
    }

    [Fact]
    public void Play_mode_regressions_cover_arrival_completion_cancel_and_roster_identity()
    {
        string playMode = Read(
            RepositoryRoot(),
            "unity/Dig.Unity/Assets/Dig.Unity/Tests/PlayMode/"
                + "BuildingBoxRuntimeLifecyclePlayModeTests.cs");

        Assert.Contains("Held_box_arrival_starts_unpack_and_completes_same_lifecycle", playMode);
        Assert.Contains("Held_box_natural_route_commits_immediately_on_arrival", playMode);
        Assert.Contains("Held_box_relocation_deposits_from_adjacent_supported_cell", playMode);
        Assert.Contains("Direct_move_cancellation_removes_plan_and_keeps_same_box", playMode);
        Assert.Contains("SourceBuildingBoxStackId", playMode);
        Assert.Contains("BuildingBoxCommitState.AtSite", playMode);
        Assert.Contains("ItemLocation.InBuilding", playMode);
        Assert.Contains("BuildingVisualState.Assembly", playMode);
    }

    private static string Read(string root, string relativePath)
    {
        return Normalize(File.ReadAllText(Path.Combine(root, relativePath)));
    }

    private static string RuntimeRoot()
    {
        return Path.Combine(
            RepositoryRoot(),
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime");
    }

    private static string RepositoryRoot()
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

    private static string Normalize(string source)
    {
        return source
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\t", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
    }
}

}
