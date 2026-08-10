using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingProductionUnityRuntimeContractTests
{
    [Fact]
    public void Campfire_production_is_connected_without_building_specific_runtime_branches()
    {
        string runtime = RuntimeRoot();
        string bootstrap = Read(runtime, "DigUnityBootstrap.cs");
        string execution = Read(runtime, "DigBuildingProductionExecution.cs");
        string commands = Read(runtime, "DigBuildingProductionCommands.cs");
        string synchronization = Read(runtime, "DigBuildingProductionSynchronization.cs");
        string deferredSupply = Read(runtime, "DigBuildingProductionDeferredSupply.cs");
        string runtimeExecution = Read(runtime, "DigBuildingProductionRuntime.cs");
        string zones = Read(runtime, "DigBuildingProductionZones.cs");
        string supplyCheck = Read(runtime, "DigBuildingProductionSupplyCheck.cs");
        string loop = Read(runtime, "DigAgentSimulationDriverBase.Loop.cs");
        string placement = Read(runtime, "DigBuildingBoxPlacement.cs");

        Assert.Contains("InitializeBuildingProductionDemo", bootstrap);
        Assert.Contains("SynchronizeBuildingProduction", loop);
        Assert.Contains("AdvanceBuildingProduction", loop);
        Assert.Contains("using Dig.Application.Jobs;", execution);
        Assert.Contains("using Dig.Application.Jobs;", synchronization);
        Assert.Contains(
            "new Dig.Application.Jobs.AdvanceJobCommand(",
            runtimeExecution);
        Assert.Contains("JobStageKind.TravelToTarget", supplyCheck);
        Assert.Contains("AdvanceJobCommand", supplyCheck);
        Assert.DoesNotContain("new AdvanceJobCommand(", runtimeExecution);
        Assert.Contains("CreateBuildingSupplyJobHandler", execution);
        Assert.Contains("AcquireBuildingSupplySourceHandler", execution);
        Assert.Contains("ThenByDescending(value => value.Sequence)", commands);
        Assert.Contains("PrepareEligibleProductionOrders", synchronization);
        Assert.Contains("AssignProductionJobs", synchronization);
        Assert.Contains("JobStageKind.Finalize", zones);
        Assert.Contains("EnsureProductionOutputPackage", zones);
        Assert.Contains("ResolveProductionPackageCell", zones);
        Assert.Contains("package.StackId", zones);
        Assert.Contains("ResolveEligibleDeferredSupplyJobs", synchronization + deferredSupply);
        Assert.Contains("HasNonTerminalBuildingSupplyJob", deferredSupply);
        Assert.Contains("FindByBoxItemId", placement);
        Assert.DoesNotContain("if(buildingboxitemid==campfire", Normalize(placement));
    }

    [Fact]
    public void Supply_uses_navigation_connectivity_and_recovers_blocking_jobs()
    {
        string runtime = RuntimeRoot();
        string synchronization = Read(
            runtime,
            "DigBuildingProductionSynchronization.cs");
        string recovery = Read(
            runtime,
            "DigBuildingProductionSupplyRecovery.cs");
        string productionRuntime = Read(
            runtime,
            "DigBuildingProductionRuntime.cs");

        Assert.Contains("TryLoadBuildingPlacementNavigation", synchronization);
        Assert.Contains("GetProductionReachableCells", synchronization);
        Assert.Contains("using Dig.Domain.Production;", productionRuntime);
        Assert.Contains("BuildingSupplyReachability.ResolveConnectedCells",
            productionRuntime);
        Assert.DoesNotContain(
            "value.State.IsExplored && !value.IsSolid",
            productionRuntime);
        Assert.Contains("SynchronizeRequiredProductionInputDelivery", recovery);
        Assert.Contains("EnableProductionInputDeliveryCommand", recovery);
        Assert.Contains("RecoverBlockedBuildingSupplyJobs", recovery);
        Assert.Contains("blocked_supply_replanned", recovery);
        Assert.Contains("route_unavailable", synchronization);
        string playMode = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "Assets",
            "Dig.Unity",
            "Tests",
            "PlayMode",
            "BuildingSupplyRuntimePlayModeTests.cs"));
        Assert.Contains(
            "Queued_recipe_force_enables_required_internal_stock_delivery",
            playMode);
        Assert.Contains(
            "Carried_mushroom_supply_is_deposited_into_internal_building_stock",
            playMode);
    }

    [Fact]
    public void Hud_projects_generic_product_and_internal_stock_icons()
    {
        string runtime = RuntimeRoot();
        string context = Read(runtime, "DigGameHudCanvas.Context.cs");
        string production = Read(runtime, "DigGameHudCanvas.BuildingProduction.cs");
        string hover = Read(runtime, "DigGameHudCanvas.ContextHover.cs");
        string pointer = Read(runtime, "DigProductionIconPointer.cs");

        Assert.Contains("TryShowBuildingProduction", context);
        Assert.Contains("production.Products", production);
        Assert.Contains("production.Stocks", production);
        Assert.Contains("product.IsOrange", production);
        Assert.Contains("product.DisplayName", production);
        Assert.Contains("product.Tooltip", production);
        Assert.Contains(
            "SetProductionHoverInfo(product.DisplayName, product.Tooltip)",
            production);
        Assert.Contains("SetProductionHoverInfo", hover);
        Assert.Contains("ClearProductionHoverInfo", hover);
        Assert.Contains("TextAnchor.MiddleCenter", hover);
        Assert.Contains("_productionHoverTitle", hover);
        Assert.Contains("_worldTargetHoverTitle", hover);
        Assert.Contains("hasProduction", hover);
        Assert.Contains("product.HasProductionOverlay", production);
        Assert.Contains("CreateProductionProgressOverlay", production);
        Assert.Contains("Image.FillMethod.Vertical", production);
        Assert.Contains("Image.OriginVertical.Bottom", production);
        Assert.Contains("fill.fillAmount", production);
        Assert.Contains("EnqueueBuildingProduction", production);
        Assert.Contains("CancelOneBuildingProduction", production);
        Assert.Contains("SetBuildingStockDelivery", production);
        Assert.Contains("IPointerEnterHandler", pointer);
        Assert.Contains("IPointerExitHandler", pointer);
        Assert.Contains("HoverChanged?.Invoke(false)", pointer);
    }

    [Fact]
    public void Renderer_projects_left_input_and_right_output_zones()
    {
        string runtime = RuntimeRoot();
        string renderer = Read(runtime, "DigBuildingInternalStockRenderer.cs");
        string zones = Read(runtime, "DigBuildingInternalStockRenderer.Zones.cs");
        string visual = Read(runtime, "DigBuildingInternalStockVisual.cs");
        string interaction = Read(runtime, "DigWorldInteraction.BuildingInternalStock.cs");
        string itemCursor = Read(runtime, "DigWorldInteraction.ItemInteractionCursor.cs");
        string pickup = Read(runtime, "DigWorldItemPickupSession.cs");
        string transit = Read(runtime, "DigBuildingProductionMaterialTransit.cs");
        string loop = Read(runtime, "DigAgentSimulationDriverBase.Loop.cs");

        Assert.Contains("model.Stocks", renderer);
        Assert.Contains("stockIndex", renderer);
        Assert.Contains("unitIndex", renderer);
        Assert.Contains("TryGetStock", renderer);
        Assert.Contains("ResolveInternalZoneCell", zones);
        Assert.Contains("leftEdge - 1", zones);
        Assert.Contains("VisibleDepthOffset = 0.12f", renderer);
        Assert.DoesNotContain("RenderZones", renderer + zones);
        Assert.DoesNotContain("DigBuildingInternalStockBayVisual", renderer + zones);
        Assert.False(File.Exists(Path.Combine(
            runtime,
            "DigBuildingInternalStockBayVisual.cs")));
        Assert.Contains("collider.isTrigger = true", visual);
        Assert.Contains("TryResolveBuildingInternalStockHit", interaction);
        Assert.Contains("TryResolveBuildingInternalStockPickup", itemCursor);
        Assert.Contains("ContextWorldTargetKind.GenericItem", itemCursor);
        Assert.Contains("ResolveBuildingInternalStockCell", pickup);
        Assert.Contains("Footprint.Min(value => value.X) - 1", transit);
        Assert.Contains("buildinginternalstockrenderer!.render", Normalize(loop));
    }

    [Fact]
    public void Finished_output_interaction_is_owned_by_the_authoritative_world_item_model()
    {
        string visual = Read(RuntimeRoot(), "DigWorldItemVisual.cs");

        Assert.Contains("bool interactive = Model.IsInteractive;", visual);
        Assert.DoesNotContain("&& resolution.ColliderPolicy", visual);
        Assert.Contains("_interactionCollider!.enabled = interactive;", visual);
    }

    [Fact]
    public void Completed_production_worker_waits_offset_facing_camera()
    {
        string runtime = RuntimeRoot();
        string zones = Read(runtime, "DigBuildingProductionZones.cs");
        string renderer = Read(runtime, "DigAgentRenderer.ProductionWait.cs");
        string visual = Read(runtime, "DigAgentVisual.ProductionWait.cs");
        string movement = Read(runtime, "DigAgentVisual.Movement.cs");
        string loop = Read(runtime, "DigAgentSimulationDriverBase.Loop.cs");

        Assert.Contains("ProductionWaitOffset", zones);
        Assert.Contains("LoadProductionWaitOffsets", zones);
        Assert.Contains("SynchronizeProductionWaitOffsets", renderer);
        Assert.Contains("SetProductionWaitPose", visual);
        Assert.Contains("FaceTowardMainCamera", visual);
        Assert.Contains("_productionWaitPose", movement);
        Assert.Contains("SynchronizeProductionWaitOffsets", loop);
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

    private static string Normalize(string value)
    {
        return value.Replace(" ", string.Empty).Replace("\r", string.Empty)
            .Replace("\n", string.Empty).ToLowerInvariant();
    }
}

}
