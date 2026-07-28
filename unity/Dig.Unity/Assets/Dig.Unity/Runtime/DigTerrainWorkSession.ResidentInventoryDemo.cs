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
        CellId residentStartCell)
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
            new ItemDefinition(
                MushroomCapItemId,
                "Mushroom cap",
                100,
                false,
                new[]
                {
                    ResidentInventoryExpansionContent.RawMaterialCategoryId,
                }),
            new ItemDefinition(
                MushroomLegItemId,
                "Mushroom leg",
                100,
                false,
                new[]
                {
                    ResidentInventoryExpansionContent.RawMaterialCategoryId,
                }),
        };
        ItemDefinition[] allItems = resourceItems
            .Concat(baseItems)
            .Append(CampfireBuildingBoxContent.Definition.BoxItem)
            .Concat(expansions.Items)
            .Concat(CampfireProductionContent.CreateItems())
            .GroupBy(value => value.Id)
            .Select(group => group.First())
            .OrderBy(value => value.Id)
            .ToArray();
        InventoryState inventory = new InventoryState(new ItemCatalog(allItems));
        EntityId residentId = DemoId('a', 1);
        AddResidentUnit(
            inventory,
            DemoId('3', 1),
            DemoLargeBasketItemId,
            residentId,
            ResidentInventoryCompartment.Main,
            0);
        AddResidentUnit(
            inventory,
            DemoId('4', 1),
            DemoHarnessItemId,
            residentId,
            ResidentInventoryCompartment.Main,
            1);
        AddResidentUnit(
            inventory,
            DemoId('5', 1),
            DemoBasketItemId,
            residentId,
            ResidentInventoryCompartment.Main,
            2);
        AddResidentUnit(
            inventory,
            DemoId('6', 1),
            DemoScabbardItemId,
            residentId,
            ResidentInventoryCompartment.Main,
            3);
        AddResidentUnit(
            inventory,
            DemoId('1', 1),
            DemoResidentToolItemId,
            residentId,
            ResidentInventoryCompartment.Weapon,
            0);
        AddResidentUnit(
            inventory,
            DemoId('2', 1),
            DemoResidentHammerItemId,
            residentId,
            ResidentInventoryCompartment.Weapon,
            1);
        CellId campfireBoxCell = new CellId(
            residentStartCell.X - 1,
            residentStartCell.Y,
            residentStartCell.Z);
        Require(inventory.AddUnit(
            DemoId('7', 1),
            CampfireBuildingBoxContent.CampfireBoxItemId,
            ItemLocation.InWorld(campfireBoxCell),
            tick: 0));
        return inventory;
    }

    private static void AddResidentUnit(
        InventoryState inventory,
        EntityId itemEntityId,
        ItemId itemId,
        EntityId residentId,
        ResidentInventoryCompartment compartment,
        int slotIndex)
    {
        Require(inventory.AddUnit(
            itemEntityId,
            itemId,
            ItemLocation.InResidentSlot(residentId, compartment, slotIndex),
            tick: 0));
    }
}

}
