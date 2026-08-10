using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class BarrelUnityRuntimeContractTests
{
    [Fact]
    public void Runtime_wires_four_demo_barrels_attack_cursor_and_safe_falling()
    {
        string session = Read("Assets/Dig.Unity/Runtime/DigTerrainWorkSession.Barrels.cs");
        string navigation = Read("Assets/Dig.Unity/Runtime/DigTerrainWorkSession.BarrelNavigation.cs");
        string cursor = Read("Assets/Dig.Unity/Runtime/DigWorldInteraction.DirectCommandCursor.cs");
        string visual = Read("Assets/Dig.Unity/Runtime/DigBarrelVisual.cs");
        string renderer = Read("Assets/Dig.Unity/Runtime/DigBarrelRenderer.cs");
        string interaction = Read("Assets/Dig.Unity/Runtime/DigWorldInteraction.Barrels.cs");
        string playMode = Read(
            "Assets/Dig.Unity/Tests/PlayMode/"
                + "BarrelAttackSurfacePlayModeTests.cs");

        Assert.Contains("FindBarrelDemoCells(surface: true, count: 2", session, StringComparison.Ordinal);
        Assert.Contains("surface: false", session, StringComparison.Ordinal);
        Assert.Contains("count: 2", session, StringComparison.Ordinal);
        Assert.Contains("BarrelStoneItemId", session, StringComparison.Ordinal);
        Assert.Contains("BarrelOreItemId", session, StringComparison.Ordinal);
        Assert.Contains("MiningOutputWorldSeed", session, StringComparison.Ordinal);
        Assert.Contains("RandomStreamCatalog", session, StringComparison.Ordinal);
        Assert.Contains("barrel.contents.", session, StringComparison.Ordinal);
        Assert.Contains("CompleteBarrelHitCommand", session, StringComparison.Ordinal);
        Assert.Contains("GenerationConflict", session, StringComparison.Ordinal);
        Assert.Contains("SettleUnsupportedBarrels", session, StringComparison.Ordinal);
        Assert.Contains("TryResolveBarrelLanding", navigation, StringComparison.Ordinal);
        Assert.Contains("IsSupportedBarrelAttackPath", navigation, StringComparison.Ordinal);
        Assert.Contains("HasFullStandingSupport", navigation, StringComparison.Ordinal);
        Assert.Contains("TunnelTraversalKind.SupportedWalk", navigation, StringComparison.Ordinal);
        Assert.Contains(".Where(HasFullStandingSupport)", navigation, StringComparison.Ordinal);
        Assert.Equal(2, Count(navigation, "IsSupportedBarrelAttackPath(navigation, path.Path)"));
        Assert.Contains("DirectCommandCursorKind.Sword", cursor, StringComparison.Ordinal);
        Assert.Contains("Sword = 5", cursor, StringComparison.Ordinal);
        Assert.Contains("Eat = 6", cursor, StringComparison.Ordinal);
        Assert.Contains("SetHighlighted", cursor, StringComparison.Ordinal);
        Assert.Contains("PresentationScale = 0.70f", visual, StringComparison.Ordinal);
        Assert.Contains("VisualHeight => 0.49f", visual, StringComparison.Ordinal);
        Assert.DoesNotContain("FrontOffset", renderer, StringComparison.Ordinal);
        Assert.Contains("DigTunnelProjection.ResidentFootSink", renderer, StringComparison.Ordinal);
        Assert.Contains("worldPositionStays: true", renderer, StringComparison.Ordinal);
        Assert.Contains("visual.transform.rotation = Quaternion.identity", renderer, StringComparison.Ordinal);
        Assert.Contains("TryResolveBarrelHit", interaction, StringComparison.Ordinal);
        Assert.Contains("Атакует бочку", interaction, StringComparison.Ordinal);
        Assert.Contains(
            "Barrel_attack_requires_supported_route_and_supported_adjacent_work_cell",
            playMode,
            StringComparison.Ordinal);
        Assert.Contains("IsSupportedBarrelAttackPath", playMode, StringComparison.Ordinal);
        Assert.Contains("HasFullActorSupport", playMode, StringComparison.Ordinal);
    }

    [Fact]
    public void Barrel_hover_click_and_job_presentation_use_one_complete_contract()
    {
        string worldInteraction = Read(
            "Assets/Dig.Unity/Runtime/DigWorldInteraction.cs");
        string decisions = Read(
            "Assets/Dig.Unity/Runtime/DigWorldInteraction.Decisions.cs");
        string priority = Read(
            "Assets/Dig.Unity/Runtime/DigWorldInteraction.ResidentCommandPriority.cs");
        string overlay = Read(
            "src/Dig.Presentation.Abstractions/Jobs/JobOverlayPresenter.cs");
        string activity = Read(
            "src/Dig.Presentation.Abstractions/Agents/ResidentActivityPresenter.cs");
        string facing = Read(
            "Assets/Dig.Unity/Runtime/DigAgentRenderer.WorkFacing.cs");
        string visualFacing = Read(
            "Assets/Dig.Unity/Runtime/DigAgentVisual.WorkFacing.cs");

        Assert.Contains("DigBarrelRenderer barrelRenderer", worldInteraction, StringComparison.Ordinal);
        Assert.Contains("_barrelRenderer = barrelRenderer", worldInteraction, StringComparison.Ordinal);
        Assert.Contains("&& _barrelRenderer != null", worldInteraction, StringComparison.Ordinal);
        Assert.Contains("case ApplicationInputCommandKind.AttackBarrel", decisions, StringComparison.Ordinal);
        Assert.Contains("ApplyBarrelAttack(decision)", decisions, StringComparison.Ordinal);
        Assert.Contains("TryResolveBarrelHit", priority, StringComparison.Ordinal);
        Assert.Contains("isBarrelAttack: job.Definition is BarrelAttackJobDefinition", overlay, StringComparison.Ordinal);
        Assert.Contains("BarrelAttackJobDefinition barrel => barrel.TargetCell", activity, StringComparison.Ordinal);
        Assert.Contains("Атакует бочку", activity, StringComparison.Ordinal);
        Assert.Contains("job.IsBarrelAttack", facing, StringComparison.Ordinal);
        Assert.Contains("ResidentActionVisualState.Hit", visualFacing, StringComparison.Ordinal);
    }

    [Fact]
    public void Barrel_cells_block_buildings_without_becoming_movement_occupancy()
    {
        string state = Read("src/Dig.Domain/WorldObjects/BarrelState.cs");
        string job = Read("src/Dig.Domain/Jobs/BarrelAttackJobDefinition.cs");
        string placement = Read("Assets/Dig.Unity/Runtime/DigBuildingBoxPlacement.cs");

        Assert.Contains("GetBuildingBlockedCells", state, StringComparison.Ordinal);
        Assert.Contains("ReservationKey.ForPosition(WorkPosition)", job, StringComparison.Ordinal);
        Assert.DoesNotContain("ReservationKey.ForEcologyTarget(BarrelId)", job, StringComparison.Ordinal);
        Assert.Contains("BuildingPlacementBlockedCells", placement, StringComparison.Ordinal);
    }

    [Fact]
    public void Barrel_save_builder_keeps_the_instance_partial_contract()
    {
        string builder = Read("src/Dig.Application/Saving/SaveGameBuilder.cs");
        string barrelBuilder = Read("src/Dig.Application/Saving/SaveGameBuilder.Barrels.cs");
        string barrelData = Read("src/Dig.Application/Saving/BarrelSaveData.cs");

        Assert.Contains("public sealed partial class SaveGameBuilder", builder, StringComparison.Ordinal);
        Assert.Contains("public sealed partial class SaveGameBuilder", barrelBuilder, StringComparison.Ordinal);
        Assert.DoesNotContain("public static partial class SaveGameBuilder", barrelBuilder, StringComparison.Ordinal);
        Assert.Contains("List<BarrelEntitySaveData> Barrels", barrelData, StringComparison.Ordinal);
    }


    private static int Count(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string Read(string relativePath)
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Dig.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory!.FullName, relativePath));
    }
}

}
