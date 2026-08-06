using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
public sealed class RoomPurposeManualReinforcementUnityContractTests
{
    [Fact]
    public void Room_marker_precedes_excavation_and_opens_authoritative_context()
    {
        string interaction = Runtime("DigWorldInteraction.cs");
        string rooms = Runtime("DigWorldInteraction.RoomPurposes.cs");
        string hud = Runtime("DigGameHudCanvas.Context.cs");

        Assert.Contains("TryHandleRoomPurposeMarker", interaction);
        Assert.True(
            interaction.IndexOf("TryHandleRoomPurposeMarker", StringComparison.Ordinal)
            < interaction.IndexOf("TryHandleCaveRoomPlacement", StringComparison.Ordinal));
        Assert.Contains("_terrainSession?.LoadRoomPurpose", rooms);
        Assert.Contains("TryShowSelectedRoomPurpose", hud);
    }

    [Fact]
    public void B_chord_routes_before_ordinary_inventory_placement()
    {
        string layout = Runtime("DigWorldInteraction.CanvasHud.cs");
        string legacy = Runtime("DigWorldInteraction.ResidentInventory.cs");
        string reinforcement = Runtime(
            "DigWorldInteraction.TunnelReinforcementPlacement.cs");
        string execution = Runtime("DigTerrainManualTunnelReinforcement.cs");

        Assert.Contains("TryBeginTunnelReinforcementPlacement(slot)", layout);
        Assert.Contains("TryBeginTunnelReinforcementPlacement(slot)", legacy);
        Assert.Contains("Input.GetKey(KeyCode.B)", reinforcement);
        Assert.Contains("ValidateTunnelManualReinforcement", execution);
        Assert.True(
            execution.IndexOf("ValidateTunnelManualReinforcement", StringComparison.Ordinal)
            < execution.IndexOf("PrepareResidentsForDirectCommand", StringComparison.Ordinal));
        Assert.Contains("CreateResidentInventoryPlacement", Runtime(
            "DigResidentInventoryPlacementExecution.cs"));
    }

    private static string Runtime(string file)
    {
        return File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "unity", "Dig.Unity", "Assets", "Dig.Unity", "Runtime", file));
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "Dig.sln")))
        {
            current = current.Parent;
        }

        return current?.FullName
            ?? throw new InvalidOperationException("Repository root not found.");
    }
}
}
