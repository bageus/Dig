using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentUnitAndCaveRoomRuntimeRegressionContractTests
{
    [Fact]
    public void Every_resident_inventory_projection_normalizes_units_and_hides_quantities()
    {
        string runtime = RuntimeRoot();
        string session = Read(runtime, "DigResidentInventory.cs");
        string canvas = Read(runtime, "DigGameHudCanvas.Inventory.cs");
        string legacy = Read(runtime, "DigHudOverlay.ResidentInventory.cs");

        Assert.Equal(2, Count(session, "LoadNormalizedResidentInventory(id)"));
        Assert.Contains("NormalizeResidentInventory(residentId,tick:0)", session);
        Assert.DoesNotContain("slot.Quantity>1", canvas);
        Assert.DoesNotContain("×{slot.Quantity}", canvas);
        Assert.DoesNotContain("×{slot.Quantity}", legacy);
        Assert.DoesNotContain("available{slot.AvailableQuantity}", legacy);
    }

    [Fact]
    public void Cave_confirmation_refreshes_world_owned_designations_before_preview_clear()
    {
        string runtime = RuntimeRoot();
        string room = Read(runtime, "DigWorldInteraction.CaveRooms.cs");
        string driver = Read(runtime, "DigAgentSimulationDriverBase.Excavation.cs");
        string playMode = Read(
            RepositoryRoot(),
            "Assets/Dig.Unity/Tests/PlayMode/"
                + "CaveRoomReapplyAndMediumPreviewPlayModeTests.cs");

        int refresh = room.IndexOf(
            "RefreshPersistentCaveRoomDesignations();", StringComparison.Ordinal);
        int clear = room.IndexOf("DisableCaveRoomPlanning();", StringComparison.Ordinal);
        Assert.True(refresh >= 0 && clear > refresh);
        Assert.Contains("InvalidateDesignationSynchronization()", room);
        Assert.Contains("SynchronizeTunnelDesignations(_session!.LoadView())", room);
        Assert.DoesNotContain("GetComponent<DigExcavationCursorRenderer>()", driver);
        Assert.Contains("Pointer_on_each_front_silhouette_cell_resolves_the_same_anchor", playMode);
        Assert.Contains("TestCase(CaveRoomPresetKind.Tall)", playMode);
    }

    private static int Count(string source, string value)
    {
        int count = 0;
        int start = 0;
        while ((start = source.IndexOf(value, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += value.Length;
        }

        return count;
    }

    private static string Read(string root, string relativePath)
    {
        return Normalize(File.ReadAllText(Path.Combine(root, relativePath)));
    }

    private static string RuntimeRoot()
    {
        return Path.Combine(
            RepositoryRoot(),
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
