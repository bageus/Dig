using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private InventoryState CreateDemoResidentInventory()
    {
        ItemCategoryId raw = new ItemCategoryId("raw.stone");
        ItemCategoryId weapon = new ItemCategoryId("weapon");
        ItemCategoryId buildingBox = new ItemCategoryId("building.box");
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(
                DemoBuildingBoxItemId,
                "Workshop BuildingBox",
                1,
                false,
                new[] { buildingBox }),
            new ItemDefinition(
                DemoResidentToolItemId,
                "Resident pickaxe",
                1,
                true,
                new[] { weapon }),
            new ItemDefinition(
                DemoResidentHammerItemId,
                "Resident hammer",
                1,
                true,
                new[] { weapon }),
            CreateExpansion(
                DemoBasketItemId,
                "Basket",
                InventoryExpansionGroup.Cargo,
                1,
                4,
                0.75d,
                new[] { raw, buildingBox }),
            CreateExpansion(
                DemoLargeBasketItemId,
                "Large basket",
                InventoryExpansionGroup.Cargo,
                2,
                6,
                0.65d,
                new[] { raw, buildingBox }),
            CreateExpansion(
                DemoScabbardItemId,
                "Scabbard",
                InventoryExpansionGroup.Weapon,
                1,
                2,
                1d,
                new[] { weapon }),
            CreateExpansion(
                DemoHarnessItemId,
                "Weapon harness",
                InventoryExpansionGroup.Weapon,
                2,
                4,
                1d,
                new[] { weapon }),
        }));
        EntityId residentId = DemoId('a', 1);
        AddResidentStack(
            inventory,
            DemoId('3', 1),
            DemoLargeBasketItemId,
            residentId,
            ResidentInventoryCompartment.Main,
            0);
        AddResidentStack(
            inventory,
            DemoId('4', 1),
            DemoHarnessItemId,
            residentId,
            ResidentInventoryCompartment.Main,
            1);
        AddResidentStack(
            inventory,
            DemoId('5', 1),
            DemoBasketItemId,
            residentId,
            ResidentInventoryCompartment.Main,
            2);
        AddResidentStack(
            inventory,
            DemoId('6', 1),
            DemoScabbardItemId,
            residentId,
            ResidentInventoryCompartment.Main,
            3);
        AddResidentStack(
            inventory,
            DemoId('1', 1),
            DemoResidentToolItemId,
            residentId,
            ResidentInventoryCompartment.Weapon,
            0);
        AddResidentStack(
            inventory,
            DemoId('2', 1),
            DemoResidentHammerItemId,
            residentId,
            ResidentInventoryCompartment.Weapon,
            1);
        return inventory;
    }

    private static ItemDefinition CreateExpansion(
        ItemId id,
        string name,
        InventoryExpansionGroup group,
        int tier,
        int slots,
        double speedMultiplier,
        IReadOnlyCollection<ItemCategoryId> acceptedCategories)
    {
        return new ItemDefinition(
            id,
            name,
            1,
            false,
            acceptedCategories,
            new InventoryExpansionDefinition(
                group,
                tier,
                slots,
                acceptedCategories,
                speedMultiplier,
                $"visual.{id}"));
    }

    private static void AddResidentStack(
        InventoryState inventory,
        EntityId stackId,
        ItemId itemId,
        EntityId residentId,
        ResidentInventoryCompartment compartment,
        int slotIndex)
    {
        Require(inventory.AddStack(
            stackId,
            itemId,
            1,
            ItemLocation.InResidentSlot(residentId, compartment, slotIndex),
            tick: 0));
    }
}

}
