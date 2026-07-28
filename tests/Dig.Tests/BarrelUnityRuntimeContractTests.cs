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
        string session = Read("unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigTerrainWorkSession.Barrels.cs");
        string navigation = Read("unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigTerrainWorkSession.BarrelNavigation.cs");
        string cursor = Read("unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldInteraction.DirectCommandCursor.cs");
        string visual = Read("unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigBarrelVisual.cs");
        string interaction = Read("unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldInteraction.Barrels.cs");

        Assert.Contains("FindBarrelDemoCells(surface: true, count: 2", session, StringComparison.Ordinal);
        Assert.Contains("surface: false", session, StringComparison.Ordinal);
        Assert.Contains("count: 2", session, StringComparison.Ordinal);
        Assert.Contains("BarrelStoneItemId", session, StringComparison.Ordinal);
        Assert.Contains("BarrelOreItemId", session, StringComparison.Ordinal);
        Assert.Contains("CompleteBarrelHitCommand", session, StringComparison.Ordinal);
        Assert.Contains("GenerationConflict", session, StringComparison.Ordinal);
        Assert.Contains("SettleUnsupportedBarrels", session, StringComparison.Ordinal);
        Assert.Contains("TryResolveBarrelLanding", navigation, StringComparison.Ordinal);
        Assert.Contains("DirectCommandCursorKind.Sword", cursor, StringComparison.Ordinal);
        Assert.Contains("SetHighlighted", cursor, StringComparison.Ordinal);
        Assert.Contains("VisualHeight => 1.05f", visual, StringComparison.Ordinal);
        Assert.Contains("Атакует бочку", interaction, StringComparison.Ordinal);
    }

    [Fact]
    public void Barrel_cells_block_buildings_without_becoming_movement_occupancy()
    {
        string state = Read("src/Dig.Domain/WorldObjects/BarrelState.cs");
        string job = Read("src/Dig.Domain/Jobs/BarrelAttackJobDefinition.cs");
        string placement = Read("unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigBuildingBoxPlacement.cs");

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
