using System.Linq;
using Dig.Application.Inventory;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class WorldItemInteractionTests
{
    [Fact]
    public void Generic_item_definition_automatically_supports_pickup_and_inventory_drop()
    {
        ItemId material = new ItemId("material.test");
        ItemCatalog catalog = new ItemCatalog(new[]
        {
            new ItemDefinition(material, "Test material", 20, false),
        });
        WorldItemViewModel item = Project(catalog, material, Id(1));

        Assert.Equal(
            ItemWorldInteractionAction.Pickup,
            item.ResolveWorldAction(altPressed: false));
        Assert.Equal(
            ItemInventoryInteractionAction.PlaceItem,
            item.InteractionProfile.InventoryPrimaryAction);
        Assert.True(item.InteractionProfile.InventoryQuickDropAllowed);
        Assert.True(item.CanPickup);
    }

    [Fact]
    public void Building_box_category_automatically_selects_on_lmb_and_picks_up_on_alt()
    {
        ItemId box = new ItemId("building_box.test");
        ItemCatalog catalog = new ItemCatalog(new[]
        {
            new ItemDefinition(
                box,
                "Test box",
                1,
                false,
                new[] { ItemInteractionCategoryIds.BuildingBox }),
        });
        WorldItemViewModel item = Project(catalog, box, Id(2));

        Assert.True(item.IsBuildingBox);
        Assert.Equal(
            ItemWorldInteractionAction.SelectBuildingBox,
            item.ResolveWorldAction(altPressed: false));
        Assert.Equal(
            ItemWorldInteractionAction.Pickup,
            item.ResolveWorldAction(altPressed: true));
        Assert.Equal(
            ItemInventoryInteractionAction.PlaceBuilding,
            item.InteractionProfile.InventoryPrimaryAction);
        Assert.True(item.InteractionProfile.InventoryQuickDropAllowed);
    }

    [Fact]
    public void Food_definition_automatically_supports_pickup_direct_use_and_drop()
    {
        ItemId food = new ItemId("meal.no_prefix_required");
        ItemCatalog catalog = new ItemCatalog(new[]
        {
            new ItemDefinition(
                food,
                "Test food",
                10,
                false,
                foodUse: new ItemFoodUseDefinition(1_800, 3)),
        });
        WorldItemViewModel item = Project(catalog, food, Id(3));

        Assert.Equal(
            ItemWorldInteractionAction.Pickup,
            item.ResolveWorldAction(altPressed: false));
        Assert.Equal(
            ItemWorldInteractionAction.DirectUse,
            item.ResolveWorldAction(altPressed: true));
        Assert.Equal(
            ItemInventoryInteractionAction.DirectUse,
            item.InteractionProfile.InventoryAltAction);
        Assert.Equal(
            ItemInteractionFeedbackKind.Eat,
            item.InteractionProfile.DirectUseFeedback);
        Assert.True(item.InteractionProfile.InventoryQuickDropAllowed);
    }

    [Fact]
    public void Club_automatically_uses_tool_interactions_without_unity_id_rules()
    {
        ItemDefinition club = Assert.Single(CombatEquipmentContent.CreateItems());
        ItemCatalog catalog = new ItemCatalog(new[] { club });
        WorldItemViewModel item = Project(catalog, club.Id, Id(4));

        Assert.Equal(new ItemId("weapon.club"), club.Id);
        Assert.Equal(
            ItemWorldInteractionAction.Pickup,
            item.ResolveWorldAction(altPressed: false));
        Assert.Equal(
            ItemInventoryInteractionAction.PlaceItem,
            club.Interactions.InventoryPrimaryAction);
        Assert.Equal(
            ItemInventoryInteractionAction.DirectUse,
            club.Interactions.InventoryAltAction);
        Assert.True(club.Interactions.InventoryQuickDropAllowed);
        Assert.Equal(ItemInteractionFeedbackKind.Use, club.Interactions.DirectUseFeedback);
    }

    [Fact]
    public void Production_packages_project_their_definition_owned_policy()
    {
        ItemCatalog catalog = new ItemCatalog(ProductionPackageContent.CreateItems());
        InventoryState inventory = new InventoryState(catalog);
        Assert.True(inventory.AddStack(
            Id(11),
            ProductionPackageContent.UnfinishedPackageItemId,
            1,
            ItemLocation.InWorld(new CellId(1, 1)),
            0).IsSuccess);
        Assert.True(inventory.AddStack(
            Id(12),
            ProductionPackageContent.FoodPackageItemId,
            1,
            ItemLocation.InWorld(new CellId(2, 1)),
            0).IsSuccess);
        WorldItemViewModel[] items = new InventoryWorldPresenter(
            new GetInventorySnapshotQueryHandler(
                new InMemoryInventoryRepository(inventory)),
            catalog).Load().ToArray();

        WorldItemViewModel unfinished = items.Single(value =>
            value.ItemId == ProductionPackageContent.UnfinishedPackageItemId.ToString());
        WorldItemViewModel closed = items.Single(value =>
            value.ItemId == ProductionPackageContent.FoodPackageItemId.ToString());
        Assert.False(unfinished.IsInteractive);
        Assert.Equal(
            ItemWorldInteractionAction.UseProductionPackage,
            closed.ResolveWorldAction(altPressed: false));
        Assert.True(closed.CanUse);
        Assert.False(closed.CanPickup);
    }

    private static WorldItemViewModel Project(
        ItemCatalog catalog,
        ItemId itemId,
        EntityId stackId)
    {
        InventoryState inventory = new InventoryState(catalog);
        Assert.True(inventory.AddStack(
            stackId,
            itemId,
            1,
            ItemLocation.InWorld(new CellId(3, 4)),
            0).IsSuccess);
        return Assert.Single(new InventoryWorldPresenter(
            new GetInventorySnapshotQueryHandler(
                new InMemoryInventoryRepository(inventory)),
            catalog).Load());
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
