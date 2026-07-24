using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private static InventoryState CreateDemoResidentInventory(
        IReadOnlyList<TerrainDepositDefinition> depositDefinitions,
        CellId campfireBoxCell)
    {
        if (depositDefinitions == null)
        {
            throw new ArgumentNullException(nameof(depositDefinitions));
        }

        ResidentInventoryExpansionContent expansions =
            new ResidentInventoryExpansionContent();
        ItemDefinition[] resourceItems = depositDefinitions
            .Select(value => new ItemDefinition(
                value.OutputItemId,
                value.DisplayName,
                maximumStackSize: 100,
                isTool: false,
                new[]
                {
                    ResidentInventoryExpansionContent.RawMaterialCategoryId,
                }))
            .ToArray();
        ItemDefinition[] baseItems =
        {
            new ItemDefinition(
                DemoBuildingBoxItemId,
                "Workshop BuildingBox",
                1,
                false,
                new[]
                {
                    ResidentInventoryExpansionContent.GeneralItemCategoryId,
                }),
            new ItemDefinition(
                DemoResidentToolItemId,
                "Resident pickaxe",
                1,
                true,
                new[]
                {
                    ResidentInventoryExpansionContent.WeaponCategoryId,
                }),
            new ItemDefinition(
                DemoResidentHammerItemId,
                "Resident hammer",
                1,
                true,
                new[]
                {
                    ResidentInventoryExpansionContent.WeaponCategoryId,
                }),
        };
        InventoryState inventory = new InventoryState(new ItemCatalog(
            resourceItems
                .Concat(baseItems)
                .Append(CampfireBuildingBoxContent.Definition.BoxItem)
                .Concat(expansions.Items)));
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
        Require(inventory.AddStack(
            DemoId('7', 1),
            CampfireBuildingBoxContent.CampfireBoxItemId,
            1,
            ItemLocation.InWorld(campfireBoxCell),
            tick: 0));
        return inventory;
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
