using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
public sealed class RuntimeRoomToggleHoverAndPickBoundsContractTests
{
    [Fact]
    public void Room_mode_uses_one_unlock_gated_toggle()
    {
        string source = ReadRuntime("DigGameHudCanvas.RoomPlanningMode.cs");

        Assert.Contains("if (_interaction.IsRoomUpgradeModeUnlocked)", source);
        Assert.Contains("AddRoomPlanningModeToggle(row);", source);
        Assert.Contains("Room Types Toggle", source);
        Assert.Contains("SetRoomUpgradeMode(!_interaction.RoomUpgradeMode)", source);
        Assert.DoesNotContain("Dig Mode", source);
        Assert.DoesNotContain("Upgrade Mode", source);
    }

    [Fact]
    public void Hover_is_selection_independent_while_action_cursor_remains_selected()
    {
        string pointer = ReadRuntime("DigWorldInteraction.PointerHits.cs");
        string cursor = ReadRuntime("DigWorldInteraction.DirectCommandCursor.cs");

        Assert.Contains("TryResolveMushroomHit(hits", pointer);
        Assert.Contains("TryResolveAnyWorldItemHit(", pointer);
        Assert.DoesNotContain("_agentRenderer.SelectedCount > 0\n                    && TryResolve", pointer);
        Assert.Contains("TryResolveBarrelHit(hits", cursor);
        Assert.Contains("if (_agentRenderer != null && _agentRenderer.SelectedCount > 0)", cursor);
        Assert.Contains("ApplyCommandCursor(kind);", cursor);
    }

    [Fact]
    public void Pickup_collider_uses_renderer_bounds_with_small_tolerance()
    {
        string source = ReadRuntime("DigWorldItemVisual.cs");

        Assert.Contains("local.size.x + 0.02f", source);
        Assert.Contains("local.size.y + 0.02f", source);
        Assert.Contains("local.size.z + 0.02f", source);
        Assert.DoesNotContain("Mathf.Max(0.28f", source);
    }

    private static string ReadRuntime(string fileName)
    {
        string? current = AppContext.BaseDirectory;
        while (current != null)
        {
            string candidate = Path.Combine(current, "unity", "Dig.Unity", "Assets",
                "Dig.Unity", "Runtime", fileName);
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
