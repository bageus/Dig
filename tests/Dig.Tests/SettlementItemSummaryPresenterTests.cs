using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Buildings;
using Dig.Presentation.Management;
using Xunit;

namespace Dig.Tests
{

public sealed class SettlementItemSummaryPresenterTests
{
    private static readonly EntityId BuildingId = Id('b');
    private static readonly EntityId StorageId = Id('c');

    [Fact]
    public void Summary_groups_items_and_breaks_container_quantities_out()
    {
        ItemDefinition stone = Item("material.stone", "Stone", "material.raw");
        ItemDefinition ironOre = Item("ore.iron", "Iron ore", "material.raw");
        ItemDefinition sword = Item("weapon.sword", "Sword", "weapon");
        ItemDefinition meal = Item("food.stew", "Stew", "food");
        ItemDefinition potion = Item("potion.health", "Health potion", "potion");
        ItemDefinition pickaxe = new ItemDefinition(
            new ItemId("tool.pickaxe"),
            "Pickaxe",
            maximumStackSize: 1,
            isTool: true);
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            stone,
            ironOre,
            sword,
            meal,
            potion,
            pickaxe,
        }));
        Add(inventory, Id('1'), stone.Id, 4, ItemLocation.InWorld(new CellId(1, 1)));
        Add(inventory, Id('2'), stone.Id, 6, ItemLocation.InBuilding(BuildingId));
        Add(inventory, Id('3'), ironOre.Id, 3, ItemLocation.InStorage(StorageId));
        Add(inventory, Id('4'), sword.Id, 1, ItemLocation.InAgent(Id('a')));
        Add(inventory, Id('5'), meal.Id, 2, ItemLocation.InWorld(new CellId(2, 1)));
        Add(inventory, Id('6'), potion.Id, 1, ItemLocation.InWorld(new CellId(3, 1)));
        Add(inventory, Id('7'), pickaxe.Id, 1, ItemLocation.EquippedBy(Id('a')));

        SettlementItemSummaryViewModel result = new SettlementItemSummaryPresenter()
            .Present(
                inventory.CreateSnapshot(),
                inventory.Catalog,
                new[] { Building(BuildingId, "Stone Warehouse") });

        SettlementItemLocationViewModel warehouse = Assert.Single(
            result.Locations,
            value => value.Label == "Stone Warehouse");
        Assert.Contains(result.Locations, value => value.Label.StartsWith(
            "Storage ",
            StringComparison.Ordinal));
        SettlementItemSummaryRowViewModel stones = Assert.Single(
            result.GetRows(SettlementItemGroup.Materials),
            value => value.Id == "material.stone");
        Assert.Equal(10, stones.Total);
        Assert.Equal(6, stones.GetQuantity(warehouse.Id));
        Assert.Equal(
            3,
            Assert.Single(
                result.GetRows(SettlementItemGroup.Materials),
                value => value.Id == "ore.iron").Total);
        Assert.Equal(
            "Sword",
            Assert.Single(result.GetRows(SettlementItemGroup.Weapons)).Label);
        Assert.Equal(
            "Stew",
            Assert.Single(result.GetRows(SettlementItemGroup.Food)).Label);
        Assert.Equal(
            "Health potion",
            Assert.Single(result.GetRows(SettlementItemGroup.Potions)).Label);
        Assert.Contains(
            result.GetRows(SettlementItemGroup.Tools),
            value => value.Label == "Pickaxe");
    }

    [Fact]
    public void Materials_keep_the_requested_rows_when_inventory_is_empty()
    {
        ItemDefinition unrelated = Item("misc.test", "Other", "misc");
        InventoryState inventory = new InventoryState(new ItemCatalog(new[] { unrelated }));

        SettlementItemSummaryViewModel result = new SettlementItemSummaryPresenter()
            .Present(
                inventory.CreateSnapshot(),
                inventory.Catalog,
                Array.Empty<BuildingWorldViewModel>());

        string[] ids = result.GetRows(SettlementItemGroup.Materials)
            .Select(value => value.Id)
            .ToArray();
        Assert.Contains("material.stone", ids);
        Assert.Contains("material.mushroom_stem", ids);
        Assert.Contains("material.mushroom_cap", ids);
        Assert.Contains("ore.gold", ids);
        Assert.Contains("ore.iron", ids);
        Assert.Contains("material.coal", ids);
        Assert.Contains("ore.crystal", ids);
        Assert.Contains("material.gold", ids);
        Assert.Contains("material.iron", ids);
        Assert.Contains("material.crystal", ids);
        Assert.Contains("creature.hamster", ids);
        Assert.Contains("creature.grub", ids);
    }

    private static ItemDefinition Item(string id, string name, string category)
    {
        return new ItemDefinition(
            new ItemId(id),
            name,
            maximumStackSize: 100,
            isTool: false,
            new[] { new ItemCategoryId(category) });
    }

    private static void Add(
        InventoryState inventory,
        EntityId stackId,
        ItemId itemId,
        int quantity,
        ItemLocation location)
    {
        Assert.True(inventory.AddStack(stackId, itemId, quantity, location, tick: 0).IsSuccess);
    }

    private static BuildingWorldViewModel Building(EntityId id, string name)
    {
        BuildingDefinitionId definitionId = new BuildingDefinitionId("storage.test");
        return new BuildingWorldViewModel(
            id.ToString(),
            definitionId.ToString(),
            name,
            originX: 1,
            originY: 1,
            BuildingStatus.Completed,
            version: 1,
            new[] { new BuildingFootprintCellViewModel(1, 1) },
            new BuildingFunctionsViewModel(
                id,
                definitionId,
                BuildingStatus.Completed,
                durability: 100,
                maximumDurability: 100,
                isPacking: false,
                packingCompletedWork: 0,
                packingRequiredWork: 0,
                Array.Empty<BuildingFunctionActionViewModel>()));
    }

    private static EntityId Id(char prefix)
    {
        return EntityId.Parse(prefix + new string('0', 31));
    }
}

}
