using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class InventoryBarrelRoomRuntimeRegressionTests
{
    [Fact]
    public void Resident_layout_uses_one_quantity_one_stack_per_slot()
    {
        string layout = Read(
            "src/Dig.Domain/Inventory/InventoryState.ResidentLayout.Compaction.cs");
        string apply = Read(
            "src/Dig.Domain/Inventory/InventoryState.ResidentLayout.Compaction.Apply.cs");
        string stacking = Read(
            "src/Dig.Domain/Inventory/InventoryState.ResidentStacking.cs");
        string claims = Read(
            "src/Dig.Domain/Inventory/ResidentInventorySlotClaims.cs");
        string capacity = Read(
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigResidentInventory.Capacity.cs");

        Assert.Contains("pendingUnits", layout);
        Assert.Contains("candidate.Source.Split", apply);
        Assert.Contains("quantity: 1", apply);
        Assert.Contains("CreateResidentUnitId", stacking);
        Assert.DoesNotContain("GroupBy(value => value.ItemId)", stacking);
        Assert.Contains("occupied.ContainsKey(slot)", claims);
        Assert.Contains("availableQuantity: 1", claims);
        Assert.Contains("capacity = checked(capacity + 1)", capacity);
        Assert.DoesNotContain("definition.MaximumStackSize - slot.Quantity", capacity);
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
        string demoSkills = Read(
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigAgentSession.DemoSkills.cs");
        string excavation = Read(
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/"
                + "DigAgentSimulationDriverBase.Excavation.cs");

        Assert.Contains("_pausedCaveRoomPlans", session);
        Assert.Contains("_caveRoomResumePlanner.Plan", session);
        Assert.DoesNotContain("_caveRoomPlans.RemoveAll", session);
        Assert.Contains("using System.Collections.Generic;", input);
        Assert.Contains("IReadOnlyList<CellId> candidates", input);
        Assert.Contains("CaveRoomPlacementCandidateResolver.Resolve", input);
        Assert.Contains("for (int anchorX = minimumAnchor", resolver);
        Assert.Contains("row.RequiredQuartersByX.ContainsKey(pointerCell.X)", resolver);
        Assert.Contains("pausedPlan.ExcavationTargets", resume);
        Assert.Contains("IsComplete(target, cells)", resume);
        Assert.Contains("ResolveDemoSkills(index)", Read(
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigAgentSession.cs"));
        Assert.Contains("StoneworkThresholdUnits(3)", demoSkills);
        Assert.Contains("DisableCaveRoomPlanning();", input);
        Assert.Contains("InvalidateDesignationSynchronization", input);
        Assert.DoesNotContain("InvalidateDesignationSynchronization", excavation);
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
