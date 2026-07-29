using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class InventoryBarrelRoomRuntimeRegressionTests
{
    [Fact]
    public void Resident_layout_normalization_consolidates_compatible_stacks()
    {
        string layout = Read(
            "src/Dig.Domain/Inventory/InventoryState.ResidentLayout.cs");
        string stacking = Read(
            "src/Dig.Domain/Inventory/InventoryState.ResidentStacking.cs");
        string capacity = Read(
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigResidentInventory.Capacity.cs");

        Assert.Contains("ConsolidateResidentStacks(residentId, tick)", layout);
        Assert.Contains("GroupBy(value => value.ItemId)", stacking);
        Assert.Contains("definition.MaximumStackSize - target.Quantity", stacking);
        Assert.Contains("source.ConsumeAvailable(quantity)", stacking);
        Assert.Contains("target.AddQuantity(quantity)", stacking);
        Assert.Contains("slot.ItemId == definition.Id", capacity);
        Assert.Contains("definition.MaximumStackSize - slot.Quantity", capacity);
    }

    [Fact]
    public void Barrel_route_accepts_supported_depth_without_accepting_air_paths()
    {
        string navigation = Read(
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/"
                + "DigTerrainWorkSession.BarrelNavigation.cs");

        Assert.Contains("TunnelTraversalKind.SupportedWalk", navigation);
        Assert.Contains("TunnelTraversalKind.DepthTraverse", navigation);
        Assert.Contains("path.Cells.Any(cell => !HasFullStandingSupport(cell))", navigation);
        Assert.DoesNotContain("TunnelTraversalKind.ShaftGapTraverse", navigation);
        Assert.DoesNotContain("TunnelTraversalKind.VerticalClimb", navigation);
        Assert.DoesNotContain("new CellId(target.X, target.Y - 1, target.Z)", navigation);
        Assert.DoesNotContain("new CellId(target.X, target.Y + 1, target.Z)", navigation);
    }

    [Fact]
    public void Erased_room_keeps_paused_provenance_and_medium_preview_searches_anchors()
    {
        string session = Read(
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldSession.CaveRooms.cs");
        string input = Read(
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldInteraction.CaveRooms.cs");
        string resolver = Read(
            "src/Dig.Application/World/CaveRoomPlacementCandidateResolver.cs");
        string resume = Read(
            "src/Dig.Application/World/CaveRoomResumePlanner.cs");

        Assert.Contains("_pausedCaveRoomPlans", session);
        Assert.Contains("_caveRoomResumePlanner.Plan", session);
        Assert.DoesNotContain("_caveRoomPlans.RemoveAll", session);
        Assert.Contains("CaveRoomPlacementCandidateResolver.Resolve", input);
        Assert.Contains("for (int anchorX = minimumAnchor", resolver);
        Assert.Contains("row.RequiredQuartersByX.ContainsKey(pointerCell.X)", resolver);
        Assert.Contains("pausedPlan.ExcavationTargets", resume);
        Assert.Contains("IsComplete(target, cells)", resume);
    }

    private static string Read(string relativePath)
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "Dig.sln")))
        {
            current = current.Parent;
        }

        Assert.NotNull(current);
        return File.ReadAllText(Path.Combine(current!.FullName, relativePath));
    }
}

}
