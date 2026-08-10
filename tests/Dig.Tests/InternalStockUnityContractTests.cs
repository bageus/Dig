using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class InternalStockUnityContractTests
{
    [Fact]
    public void Internal_stock_uses_exact_world_item_identity_for_hover_and_pickup()
    {
        string runtime = RuntimeRoot();
        string renderer = Read(runtime, "DigBuildingInternalStockRenderer.cs");
        string interaction = Read(runtime, "DigWorldInteraction.BuildingInternalStock.cs");
        string cursor = Read(runtime, "DigWorldInteraction.ItemInteractionCursor.cs");
        string pickup = Read(runtime, "DigWorldItemPickupSession.cs");
        string synchronization = Read(runtime, "DigBuildingProductionSynchronization.cs");

        Assert.Contains("DigWorldItemVisual", renderer);
        Assert.Contains("unit.StackId", renderer);
        Assert.Contains("interactionProfile: unit.InteractionProfile", renderer);
        Assert.Contains("TryResolveBuildingInternalStockHit", interaction);
        Assert.Contains("stock.StackId", cursor);
        Assert.Contains("stock.WorldItemVisual", cursor);
        Assert.Contains("CanSelectedResidentPickup(item)", cursor);
        Assert.Contains("GetStack(EntityId.Parse(stackId))", pickup);
        Assert.DoesNotContain(
            "snapshot.HasActiveSupply\n                || _productionRepository!.Get().HasActiveOrder",
            synchronization);
    }


    [Fact]
    public void Production_and_direct_pickup_use_one_internal_stock_unit_contract()
    {
        string runtime = RuntimeRoot();
        string capacity = Read(runtime, "DigResidentInventory.Capacity.cs");
        string zones = Read(runtime, "DigBuildingProductionZones.cs");
        string transit = Read(runtime, "DigBuildingProductionMaterialTransit.cs");
        string lifecycle = Read(
            runtime,
            "DigBuildingProductionMaterialLifecycle.cs");

        Assert.Contains("ItemPickupQuantityPolicy", capacity);
        Assert.Contains("AcquireProductionMaterialCommand", lifecycle);
        Assert.Contains("StageProductionMaterialCommand", lifecycle);
        Assert.Contains("DepositProductionMaterialCommand", lifecycle);
        Assert.Contains("ProcessedAwaitingPackage", lifecycle);
        Assert.Contains("ResolveBuildingInternalStockCell", lifecycle + transit);
        Assert.Contains("HasCarriedProductionMaterial", lifecycle + transit);
        Assert.DoesNotContain("requireResidentCarriedMaterial: true", lifecycle);
    }

    [Fact]
    public void Product_hud_projects_material_segments()
    {
        string production = Read(
            RuntimeRoot(),
            "DigGameHudCanvas.BuildingProduction.cs");

        Assert.Contains("CreateProductionProgressSegments", production);
        Assert.Contains("product.ProgressCurrent", production);
        Assert.Contains("product.ProgressTotal", production);
    }

    private static string Read(string root, string file)
    {
        return File.ReadAllText(Path.Combine(root, file));
    }

    private static string RuntimeRoot()
    {
        return Path.Combine(
            FindRepositoryRoot(),
            "Assets",
            "Dig.Unity",
            "Runtime");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src"))
                && File.Exists(Path.Combine(current.FullName, "Dig.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}

}
