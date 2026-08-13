using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class ProductionPackageUnityContractTests
{
    [Fact]
    public void Runtime_creates_and_finalizes_the_same_authoritative_package()
    {
        string runtime = RuntimeRoot();
        string zones = Read(runtime, "DigBuildingProductionZones.cs");
        string loop = Read(runtime, "DigAgentSimulationDriverBase.Loop.cs");

        Assert.Contains("EnsureProductionOutputPackage", zones);
        Assert.Contains("ResolveProductionPackageCell", zones);
        Assert.Contains("package.StackId", zones);
        Assert.Contains("new CompleteProductionOrderCommand", zones);
        Assert.Contains("AdvanceProductionPackages", loop);
    }

    [Fact]
    public void Unity_partials_use_authoritative_package_content_and_production_route_owner()
    {
        string runtime = RuntimeRoot();
        string zones = Read(runtime, "DigBuildingProductionZones.cs");
        string packages = Read(
            runtime,
            "DigTerrainWorkSession.ProductionPackages.cs");
        string materialization = Read(
            RepositoryPath(
                "src",
                "Dig.Application",
                "Production",
                "ProductionPackageUseLifecycle.cs"));

        Assert.Contains("using Dig.Domain.Content;", zones);
        Assert.Contains("ProductionPackageContent.ResolveKind", zones);
        Assert.Contains("PlanBuildingProductionRoute", packages);
        Assert.Contains("_buildingProductionRoutes", packages);
        Assert.Contains(
            "ProductionPackageMaterialization.RequiredOutputStackCount(package)",
            packages);
        Assert.Contains("Enumerable.Range(0, outputCount)", packages);
        Assert.Contains("ProductionOutputPackageKind.Food", materialization);
        Assert.Contains("item.Quantity", materialization);
        Assert.Contains("new ItemStackCreation", materialization);
        Assert.DoesNotContain("TerrainWorkRoutePlan", packages);
        Assert.DoesNotContain("using Dig.Application.Navigation;", packages);
    }

    [Fact]
    public void Forced_move_resets_production_through_interruption_handler()
    {
        string direct = Read(
            RuntimeRoot(),
            "DigTerrainWorkSession.DirectCommands.cs");

        Assert.Contains("ProductionWorkJobDefinition production", direct);
        Assert.Contains("InterruptProductionForDirectCommand", direct);
        Assert.Contains("new InterruptProductionOrderCommand", direct);
        Assert.Contains("production_worker_forced_move", direct);
    }

    [Fact]
    public void Closed_packages_use_animated_use_cursor_and_never_generic_pickup()
    {
        string runtime = RuntimeRoot();
        string interaction = Read(runtime, "DigWorldInteraction.ProductionPackages.cs");
        string cursor = Read(runtime, "DigWorldInteraction.DirectCommandCursor.cs");
        string textures = Read(
            runtime,
            "DigWorldInteraction.DirectCommandCursor.Textures.cs");
        string presenter = Read(
            RepositoryPath(
                "src",
                "Dig.Presentation.Abstractions",
                "Inventory",
                "InventoryWorldPresenter.cs"));

        string itemResolver = Read(runtime, "DigWorldInteraction.ItemInteractionCursor.cs");
        string content = Read(
            RepositoryPath(
                "src",
                "Dig.Domain",
                "Content",
                "ProductionPackageContent.cs"));

        Assert.Contains("ApplyProductionPackageUse", interaction);
        Assert.Contains("StartDirectProductionPackageUse", interaction);
        Assert.Contains("ItemWorldInteractionAction.UseProductionPackage", itemResolver);
        Assert.Contains("DirectCommandCursorKind.Use", cursor);
        Assert.Contains("CreateUseCursorFrames", textures);
        Assert.Contains("ItemInteractionProfiles.NonInteractive", content);
        Assert.Contains("ItemInteractionProfiles.ClosedProductionPackage", content);
        Assert.Contains("ItemDefinition definition = _catalog.Get(stack.ItemId);", presenter);
        Assert.Contains("definition.Interactions", presenter);
        Assert.DoesNotContain("ProductionPackageContent.FoodPackageItemId", presenter);
    }

    [Fact]
    public void Food_weapon_tool_and_building_boxes_have_stable_family_visuals()
    {
        string policy = Read(RuntimeRoot(), "DigWorldItemVisualPolicy.cs");
        string content = Read(RepositoryPath(
            "src", "Dig.Domain", "Content", "ProductionPackageContent.cs"));

        Assert.Contains("visual.box.building", policy);
        Assert.Contains("visual.box.food", policy);
        Assert.Contains("visual.box.weapon", policy);
        Assert.Contains("visual.box.tool", policy);
        Assert.Contains(
            "DigVisualAsset.CreateRuntimeFallback(visualId, tint)",
            policy);
        Assert.DoesNotContain("resolution.Asset.IsFallback", policy);
        Assert.Contains("ProductionOutputPackageKind.Weapon", content);
        Assert.Contains("ProductionOutputPackageKind.Tool", content);
        string zones = Read(RuntimeRoot(), "DigBuildingProductionZones.cs");
        Assert.Contains("StartAutomaticProductionPackageUse", zones);
        Assert.Contains("ProductionOutputPackageKind.Weapon", zones);
        Assert.Contains("ProductionOutputPackageKind.Tool", zones);
    }

    [Fact]
    public void Demo_surface_contains_an_installable_ladder_box()
    {
        string demo = Read(RuntimeRoot(),
            "DigTerrainWorkSession.ResidentInventoryDemo.cs");

        Assert.Contains("WorkshopProductionContent.LadderBoxItemId", demo);
        Assert.Contains("ladderBoxCell", demo);
        Assert.Contains("ItemLocation.InWorld(ladderBoxCell)", demo);
    }

    [Fact]
    public void Front_z0_is_box_relocation_only_and_never_building_assembly()
    {
        string presenter = Read(RepositoryPath(
            "src", "Dig.Presentation.Abstractions", "Buildings",
            "BuildingBoxPlacementPresenter.cs"));

        Assert.Contains("origin.Z == 0", presenter);
        Assert.Contains("BuildingBoxPlacementKind.RelocateBox", presenter);
        Assert.Contains("BuildingBoxPlacementKind.AssembleBuilding", presenter);
    }

    private static string Read(string root, string file)
    {
        return File.ReadAllText(Path.Combine(root, file));
    }

    private static string Read(string path)
    {
        return File.ReadAllText(path);
    }

    private static string RuntimeRoot()
    {
        return RepositoryPath(
            "Assets",
            "Dig.Unity",
            "Runtime");
    }

    private static string RepositoryPath(params string[] parts)
    {
        string path = FindRepositoryRoot();
        for (int index = 0; index < parts.Length; index++)
        {
            path = Path.Combine(path, parts[index]);
        }

        return path;
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
