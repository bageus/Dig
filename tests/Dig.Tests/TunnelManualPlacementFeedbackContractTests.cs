using System.IO;
using System;
using Xunit;

namespace Dig.Tests
{

public sealed class TunnelManualPlacementFeedbackContractTests
{
    [Fact]
    public void Holding_b_over_tunnel_material_uses_animated_hammer_cursor()
    {
        string cursor = Read("DigWorldInteraction.DirectCommandCursor.cs");
        string item = Read("DigWorldInteraction.ItemInteractionCursor.cs");
        string textures = Read("DigWorldInteraction.DirectCommandCursor.Textures.cs");

        Assert.Contains("Hammer = 9", cursor);
        Assert.Contains("CreateHammerCursorFrames", cursor);
        Assert.Contains("Input.GetKey(KeyCode.B)", item);
        Assert.Contains("material.mushroom_leg", item);
        Assert.Contains("material.stone", item);
        Assert.Contains("DrawHammer", textures);
    }

    [Fact]
    public void Valid_and_pending_manual_infrastructure_keeps_green_ghost_until_completion()
    {
        string interaction = Read("DigWorldInteraction.TunnelManualPlacement.cs");
        string ghost = Read("DigTunnelManualPlacementGhostRenderer.cs");
        string session = Read("DigTerrainTunnelManualInfrastructure.cs");

        Assert.Contains("ValidTint", ghost);
        Assert.Contains("Junction reinforcement", ghost);
        Assert.Contains("CancelTunnelManualPlacement(clearGhost: false)", interaction);
        Assert.Contains("SynchronizePendingTunnelManualGhost", interaction);
        Assert.Contains("HasActiveTunnelManualWork", session);
        Assert.Contains("valid: true", interaction);
    }

    private static string Read(string file)
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(
            Path.Combine(current.FullName, "Dig.sln")))
        {
            current = current.Parent;
        }

        string root = current?.FullName
            ?? throw new DirectoryNotFoundException("Repository root was not found.");
        return File.ReadAllText(Path.Combine(
            root, "Assets", "Dig.Unity", "Runtime", file));
    }
}

}
