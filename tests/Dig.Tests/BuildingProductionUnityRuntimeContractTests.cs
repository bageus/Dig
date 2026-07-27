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
        string synchronization = Read(runtime, "DigBuildingProductionSynchronization.cs");
        string loop = Read(runtime, "DigAgentSimulationDriverBase.Loop.cs");
        string placement = Read(runtime, "DigBuildingBoxPlacement.cs");

        Assert.Contains("InitializeBuildingProductionDemo", bootstrap);
        Assert.Contains("SynchronizeBuildingProduction", loop);
        Assert.Contains("AdvanceBuildingProduction", loop);
        Assert.Contains("using Dig.Application.Jobs;", execution);
        Assert.Contains("CreateBuildingSupplyJobHandler", execution);
        Assert.Contains("AcquireBuildingSupplySourceHandler", execution);
        Assert.Contains("PrepareEligibleProductionOrders", synchronization);
        Assert.Contains("AssignProductionJobs", synchronization);
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
        Assert.Contains("EnqueueBuildingProduction", production);
        Assert.Contains("SetBuildingStockDelivery", production);
        Assert.Contains("IPointerEnterHandler", pointer);
        Assert.Contains("IPointerExitHandler", pointer);
    }

    [Fact]
    public void Internal_stock_renderer_has_separate_non_interactive_piles()
    {
        string renderer = Read(RuntimeRoot(), "DigBuildingInternalStockRenderer.cs");
        string loop = Read(RuntimeRoot(), "DigAgentSimulationDriverBase.Loop.cs");

        Assert.Contains("model.Stocks", renderer);
        Assert.Contains("stockIndex", renderer);
        Assert.Contains("unitIndex", renderer);
        Assert.Contains("destroy(collider)", Normalize(renderer));
        Assert.Contains("buildinginternalstockrenderer!.render", Normalize(loop));
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
