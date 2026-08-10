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
    private static readonly ItemId DemoClubItemId =
        CombatEquipmentContent.ClubItemId;

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
            .Concat(CombatEquipmentContent.CreateItems())
            .Concat(LivingMaterialContent.CreateItems())
            .Concat(CampfireProductionContent.CreateItems())
            .GroupBy(value => value.Id)
            .Select(group => group.First())
            .OrderBy(value => value.Id)
            .ToArray();
        InventoryState inventory = new InventoryState(new ItemCatalog(allItems));
        EntityId residentId = DemoId('a', 1);
        AddResidentUnit(
            inventory,
            DemoId('1', 1),
            DemoResidentToolItemId,
            residentId,
            ResidentInventoryCompartment.Main,
            0);
        AddResidentUnit(
            inventory,
            DemoId('2', 1),
            DemoResidentHammerItemId,
            residentId,
            ResidentInventoryCompartment.Main,
            1);
        CellId basketCell = new CellId(
            residentStartCell.X + 1,
            residentStartCell.Y,
            residentStartCell.Z);
        Require(inventory.AddUnit(
            DemoId('3', 1),
            DemoBasketItemId,
            ItemLocation.InWorld(basketCell),
            tick: 0));
        CellId largeBasketCell = new CellId(
            residentStartCell.X + 2,
            residentStartCell.Y,
            residentStartCell.Z);
        Require(inventory.AddUnit(
            DemoId('4', 1),
            DemoLargeBasketItemId,
            ItemLocation.InWorld(largeBasketCell),
            tick: 0));
        CellId sheathCell = new CellId(
            residentStartCell.X + 3,
            residentStartCell.Y,
            residentStartCell.Z);
        Require(inventory.AddUnit(
            DemoId('5', 1),
            DemoScabbardItemId,
            ItemLocation.InWorld(sheathCell),
            tick: 0));
        CellId harnessCell = new CellId(
            residentStartCell.X + 4,
            residentStartCell.Y,
            residentStartCell.Z);
        Require(inventory.AddUnit(
            DemoId('6', 1),
            DemoHarnessItemId,
            ItemLocation.InWorld(harnessCell),
            tick: 0));
        CellId clubCell = new CellId(
            residentStartCell.X + 5,
            residentStartCell.Y,
            residentStartCell.Z);
        Require(inventory.AddUnit(
            DemoId('8', 1),
            DemoClubItemId,
            ItemLocation.InWorld(clubCell),
            tick: 0));
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
