using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class RuntimeBuildingUiHealthPickupContractTests
{
    [Fact]
    public void Demo_contains_only_authoritative_campfire_building_content()
    {
        string runtime = ReadRuntime("DigTerrainWorkSession.Buildings.cs");
        string inventory = ReadRuntime("DigTerrainWorkSession.ResidentInventoryDemo.cs");
        string builtIns = ReadRuntime(
            "DigRepresentativeBuildingPrefabLibrary.BuiltInProfiles.cs");
        string catalog = Read(
            "unity", "Dig.Unity", "Assets", "Dig.Unity", "Resources", "Dig",
            "VisualCatalogs", "RepresentativeBuildings.json");

        Assert.DoesNotContain("demo.workshop.box", runtime + inventory + builtIns + catalog);
        Assert.DoesNotContain("demo.building_box.workshop", runtime + inventory);
        Assert.DoesNotContain("Box Workshop", runtime + inventory);
        Assert.DoesNotContain("CreateDemoBuildingDefinition", runtime);
        Assert.Contains("new[] { campfire }", runtime);
        Assert.Contains(
            "InitializeBuildingBoxWorldInput(catalog, campfireDefinition, journal)",
            runtime);
    }

    [Fact]
    public void Building_selection_has_no_service_platforms_and_production_has_centered_hover_information()
    {
        string overlay = ReadRuntime("DigWorldOverlayRenderer.Render.cs");
        string overlayOwner = ReadRuntime("DigWorldOverlayRenderer.cs");
        string overlayEnums = Read(
            "src", "Dig.Presentation.Abstractions", "Overlays", "OverlayEnums.cs");
        string overlayStyles = Read(
            "src", "Dig.Presentation.Abstractions", "Overlays", "DefaultOverlayStyles.cs");
        string productionHud = ReadRuntime("DigGameHudCanvas.BuildingProduction.cs");
        string contextHover = ReadRuntime("DigGameHudCanvas.ContextHover.cs");
        string stockRenderer = ReadRuntime("DigBuildingInternalStockRenderer.cs");
        string zones = ReadRuntime("DigBuildingInternalStockRenderer.Zones.cs");

        Assert.DoesNotContain("Building Selection", overlay);
        Assert.DoesNotContain("Building Footprint", overlay + overlayOwner);
        Assert.DoesNotContain("_buildingFootprints", overlay + overlayOwner);
        Assert.DoesNotContain(
            "OverlaySemanticKind.BuildingFootprint",
            overlayEnums + overlayStyles);
        Assert.Contains("string.Empty", productionHud);
        Assert.DoesNotContain("Hover an icon", productionHud);
        Assert.DoesNotContain("BindIconTooltip", productionHud);
        Assert.Contains("product.Tooltip", productionHud);
        Assert.Contains("product.DisplayName", productionHud);
        Assert.Contains("SetProductionHoverInfo", productionHud + contextHover);
        Assert.Contains("TextAnchor.MiddleCenter", contextHover);
        Assert.DoesNotContain("RenderZones", stockRenderer + zones);
        Assert.DoesNotContain("DigBuildingInternalStockBayVisual", stockRenderer + zones);
        Assert.False(File.Exists(Path.Combine(
            RuntimeRoot(),
            "DigBuildingInternalStockBayVisual.cs")));
    }

    [Fact]
    public void Central_hover_region_is_reserved_without_conditional_content_resizing()
    {
        string contextHover = ReadRuntime("DigGameHudCanvas.ContextHover.cs");
        string layout = ReadRuntime("DigGameHudCanvas.Layout.cs");
        string playMode = ReadPlayMode("Issue14HudPlayModeTests.cs");

        Assert.Contains("ContextHoverContentOffsetMaxY = -52f", contextHover);
        Assert.Contains(
            "_contextHoverPanel!.gameObject.SetActive(_bottomPanel.gameObject.activeSelf)",
            contextHover);
        Assert.Contains("offsetMax.y = ContextHoverContentOffsetMaxY", contextHover);
        Assert.DoesNotContain("visible ? -52f : -8f", contextHover);
        Assert.Contains("RefreshContextHoverInfo();", layout);
        Assert.Contains(
            "Context_hover_keeps_content_and_icon_geometry_stable",
            playMode);
    }

    [Fact]
    public void Health_bars_are_world_scale_invariant_and_above_owner_renderers()
    {
        string health = ReadRuntime("DigCombatHealthBar.cs");
        string resident = ReadRuntime("DigAgentVisual.CombatHealth.cs");
        string creature = ReadRuntime("DigCreatureVisual.cs");
        string playMode = ReadPlayMode("CombatHealthBarPresentationPlayModeTests.cs");

        Assert.Contains("renderer.bounds.max.y + OwnerTopGap", health);
        Assert.Contains("owner.lossyScale", health);
        Assert.Contains("SafeInverse(ownerScale.x)", health);
        Assert.Contains("AlignAboveOwner();", health);
        Assert.Contains("verticalOffset: 1.45f", resident);
        Assert.Contains("verticalOffset: 1.45f", creature);
        Assert.Contains(
            "Different_actor_scales_keep_equal_world_width_and_place_bar_above_renderers",
            playMode);
    }

    [Fact]
    public void Output_click_and_forced_pickup_use_visible_and_post_cancellation_state()
    {
        string placement = ReadRuntime("DigWorldInteraction.BuildingBoxes.cs");
        string direct = ReadRuntime("DigTerrainWorkSession.DirectCommands.cs");
        string output = Read(
            "src", "Dig.Domain", "Production", "ProductionOutputPlacement.cs");
        string pickupPlayMode = ReadPlayMode("ForcedPickupReplacementPlayModeTests.cs");

        int clickStart = placement.IndexOf(
            "private bool TryHandleBuildingPlacementClick()",
            StringComparison.Ordinal);
        int hoverStart = placement.IndexOf(
            "private void UpdateBuildingPlacementHover()",
            clickStart,
            StringComparison.Ordinal);
        string clickMethod = placement.Substring(clickStart, hoverStart - clickStart);
        Assert.Contains("BuildingBoxGhostViewModel? visiblePreview", clickMethod);
        Assert.Contains("_inputRouter.Route", clickMethod);
        Assert.Contains("ApplyDecision(decision)", clickMethod);
        Assert.DoesNotContain("UpdateBuildingPlacementHover()", clickMethod);

        Assert.DoesNotContain("InventoryState terrainInventory", direct);
        Assert.DoesNotContain("Save(terrainInventory)", direct);
        Assert.Contains("CancelPickupForDirectCommand", direct);
        Assert.Contains(
            "Second_direct_pickup_releases_first_job_reservation_before_new_claim",
            pickupPlayMode);

        int rowLoop = output.IndexOf(
            "for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)",
            StringComparison.Ordinal);
        int distanceLoop = output.IndexOf(
            "for (int distance = 1; distance <= maximumLateralDistance + 1; distance++)",
            StringComparison.Ordinal);
        Assert.True(rowLoop >= 0 && distanceLoop > rowLoop);
    }

    private static string ReadRuntime(string file)
    {
        return File.ReadAllText(Path.Combine(RuntimeRoot(), file));
    }

    private static string ReadPlayMode(string file)
    {
        return Read(
            "unity", "Dig.Unity", "Assets", "Dig.Unity", "Tests", "PlayMode", file);
    }

    private static string Read(params string[] parts)
    {
        string path = FindRepositoryRoot();
        for (int index = 0; index < parts.Length; index++)
        {
            path = Path.Combine(path, parts[index]);
        }
        return File.ReadAllText(path);
    }

    private static string RuntimeRoot()
    {
        return Path.Combine(
            FindRepositoryRoot(),
            "unity", "Dig.Unity", "Assets", "Dig.Unity", "Runtime");
    }

    private static string FindRepositoryRoot()
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
}

}
