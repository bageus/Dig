using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class RoomInfrastructureUnityPresentationContractTests
{
    [Fact]
    public void Runtime_wires_marker_hud_commands_visual_progress_and_input_shielding()
    {
        string runtime = ResolveRuntimeRoot();
        string bootstrap = Read(runtime, "DigUnityBootstrap.cs");
        string interaction = Read(runtime, "DigWorldInteraction.cs");
        string roomInput = Read(runtime, "DigWorldInteraction.RoomInfrastructure.cs");
        string buildingBox = Read(runtime, "DigWorldInteraction.BuildingBoxSelection.cs");
        string marquee = Read(runtime, "DigWorldInteraction.MarqueeSelection.cs");
        string canvasHud = Read(runtime, "DigWorldInteraction.CanvasHud.cs");
        string vukers = Read(runtime, "DigWorldInteraction.Vukers.cs");
        string hudContext = Read(runtime, "DigGameHudCanvas.Context.cs");
        string hudRoom = Read(runtime, "DigGameHudCanvas.RoomInfrastructure.cs");
        string renderer = Read(runtime, "DigRoomInfrastructureRenderer.cs");
        string visuals = Read(runtime, "DigRoomInfrastructureRenderer.Visuals.cs");
        string driver = Read(runtime, "DigRoomInfrastructurePresentationDriver.cs");
        string session = Read(runtime, "DigTerrainRoomInfrastructure.Presentation.cs");

        Assert.Contains("DigRoomInfrastructurePresentationDriver", bootstrap);
        Assert.Contains(
            "roomPresentation.Initialize(terrainSession, roomInfrastructureRenderer)",
            bootstrap);
        Assert.Contains("SetRoomInfrastructureRenderer", bootstrap);
        Assert.Contains("SynchronizeRoomInfrastructureRuntime", bootstrap);
        Assert.Contains("LoadRoomInfrastructurePresentation", driver);
        Assert.Contains("_renderer.Render(rooms)", driver);
        Assert.True(
            interaction.IndexOf(
                "TryHandleRoomInfrastructureMarker(hits, left)",
                StringComparison.Ordinal)
            < interaction.IndexOf(
                "TryResolveAgentHit(hits",
                StringComparison.Ordinal));
        Assert.Contains("_roomInfrastructureRenderer.Select(marker)", roomInput);
        Assert.Contains("return true;", roomInput);
        Assert.Contains("ClearRoomInfrastructureSelection();", buildingBox);
        Assert.Contains("ClearRoomInfrastructureSelection();", marquee);
        Assert.Contains("ClearRoomInfrastructureSelection();", canvasHud);
        Assert.Contains("ClearRoomInfrastructureSelection();", vukers);
        Assert.Contains("SelectedRoomInfrastructure", hudContext);
        Assert.Contains("Improve", hudRoom);
        Assert.Contains("Cancel improvement", hudRoom);
        Assert.Contains("OrderRoomUpgrade(", session);
        Assert.Contains("ChangeRoomRequestedPurpose(", session);
        Assert.Contains("CancelRoomUpgrade(", session);
        Assert.Contains("Collider collider = piece.GetComponent<Collider>();", visuals);
        Assert.Contains("collider.enabled = false;", visuals);
        Assert.Contains("TryGetMarker", renderer);
    }

    private static string Read(string root, string file)
    {
        return File.ReadAllText(Path.Combine(root, file));
    }

    private static string ResolveRuntimeRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int index = 0; index < 8; index++)
        {
            string candidate = Path.Combine(
                current,
                "unity",
                "Dig.Unity",
                "Assets",
                "Dig.Unity",
                "Runtime");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = Directory.GetParent(current)?.FullName
                ?? throw new DirectoryNotFoundException();
        }

        throw new DirectoryNotFoundException(
            "Unity runtime source root was not found.");
    }
}

}
