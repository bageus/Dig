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
    public void Hud_projects_generic_product_and_internal_stock_icons()
    {
        string runtime = RuntimeRoot();
        string context = Read(runtime, "DigGameHudCanvas.Context.cs");
        string production = Read(runtime, "DigGameHudCanvas.BuildingProduction.cs");
        string pointer = Read(runtime, "DigProductionIconPointer.cs");

        Assert.Contains("TryShowBuildingProduction", context);
        Assert.Contains("production.Products", production);
        Assert.Contains("production.Stocks", production);
        Assert.Contains("product.IsOrange", production);
        Assert.Contains("product.Tooltip", production);
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
    }

    [Fact]
    public void Renderer_projects_left_input_and_right_output_zones()
    {
        string runtime = RuntimeRoot();
        string renderer = Read(runtime, "DigBuildingInternalStockRenderer.cs");
        string zones = Read(runtime, "DigBuildingInternalStockRenderer.Zones.cs");
        string visual = Read(runtime, "DigBuildingInternalStockVisual.cs");
        string bay = Read(runtime, "DigBuildingInternalStockBayVisual.cs");
        string interaction = Read(runtime, "DigWorldInteraction.BuildingInternalStock.cs");
        string pickup = Read(runtime, "DigWorldItemPickupSession.cs");
        string loop = Read(runtime, "DigAgentSimulationDriverBase.Loop.cs");

        Assert.Contains("model.Stocks", renderer);
        Assert.Contains("stockIndex", renderer);
        Assert.Contains("unitIndex", renderer);
        Assert.Contains("TryGetStock", renderer);
        Assert.Contains("Internal Storage Zone", zones);
        Assert.Contains("Finished Output Zone", zones);
        Assert.Contains("ResolveInternalZoneCell", zones);
        Assert.Contains("ResolveOutputZoneCell", zones);
        Assert.Contains("leftEdge - 1", zones);
        Assert.Contains("rightEdge + 1", zones);
        Assert.Contains("VisibleDepthOffset = 0.12f", renderer);
        Assert.Contains("DigBuildingInternalStockBayVisual", renderer + zones);
        Assert.Contains("Storage tray", bay);
        Assert.DoesNotContain("Storage back rail", bay);
        Assert.Contains("collider.enabled = false", bay);
        Assert.Contains("collider.isTrigger = true", visual);
        Assert.Contains("TryResolveBuildingInternalStockPickup", interaction);
        Assert.Contains("ContextWorldTargetKind.GenericItem", interaction);
        Assert.Contains("Footprint.Min(value => value.X) - 1", pickup);
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
            "unity",
            "Dig.Unity",
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
                && Directory.Exists(Path.Combine(current.FullName, "unity")))
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
