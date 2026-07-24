using System;
using System.IO;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Presentation.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class CampfireInventoryPlacementTests
{
    private static readonly EntityId ResidentId =
        EntityId.Parse("a1000000000000000000000000000001");
    private static readonly EntityId StackId =
        EntityId.Parse("b1000000000000000000000000000001");
    private static readonly ItemId LegacyBoxId =
        new ItemId("demo.building_box.workshop");
    private static readonly ItemId CampfireBoxId =
        new ItemId("building_box.campfire");

    [Fact]
    public void Categorized_campfire_box_can_start_placement_from_resident_slot()
    {
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(
                LegacyBoxId,
                "Workshop BuildingBox",
                maximumStackSize: 1,
                isTool: false),
            new ItemDefinition(
                CampfireBoxId,
                "Packed campfire",
                maximumStackSize: 1,
                isTool: false,
                new[] { new ItemCategoryId("building.box") }),
        }));
        Assert.True(inventory.AddStack(
            StackId,
            CampfireBoxId,
            quantity: 1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                index: 0),
            tick: 0).IsSuccess);

        ResidentInventoryLayoutViewModel layout =
            new ResidentInventoryLayoutPresenter(LegacyBoxId)
                .Present(inventory, ResidentId);
        ResidentInventoryLayoutSlotViewModel slot = layout.Slots.Single(
            value => value.StackId == StackId.ToString());

        Assert.Equal(ResidentInventorySlotVisualKind.BuildingBox, slot.VisualKind);
        Assert.True(slot.CanStartPlacement);
    }

    [Fact]
    public void Demo_runtime_resolves_the_campfire_definition_from_the_carried_box()
    {
        string root = FindRepositoryRoot();
        string runtime = Path.Combine(
            root,
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime");
        string buildings = File.ReadAllText(Path.Combine(
            runtime,
            "DigTerrainWorkSession.Buildings.cs"));
        string placement = File.ReadAllText(Path.Combine(
            runtime,
            "DigBuildingBoxPlacement.cs"));

        Assert.Contains(
            "CampfireBuildingBoxContent.Definition.Building",
            buildings);
        Assert.Contains("ResolveBuildingBoxDefinition(stack.ItemId)", placement);
        Assert.Contains(
            "CampfireBuildingBoxContent.CampfireBoxItemId",
            placement);
        Assert.Contains("_buildingBoxCatalog!.Get(mode.DefinitionId)", placement);
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