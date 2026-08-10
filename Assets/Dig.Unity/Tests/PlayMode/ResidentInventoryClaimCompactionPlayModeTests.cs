using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Presentation.Inventory;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class ResidentInventoryClaimCompactionPlayModeTests
{
    private static readonly EntityId ResidentId = Id(1);
    private static readonly ItemId Material =
        new ItemId("material.inventory_claim_playmode");
    private static readonly ItemId LargeBasket =
        new ItemId("inventory.large_basket.claim_playmode");
    private static readonly ItemCategoryId General =
        new ItemCategoryId("general.inventory_claim_playmode");

    [Test]
    public void Completed_large_basket_is_projected_in_fourth_main_cell()
    {
        InventoryState inventory = CreateInventory();
        Add(inventory, 2, Material, slot: 0);
        Add(inventory, 3, Material, slot: 1);
        Add(inventory, 4, Material, slot: 2);
        EntityId firstJob = Id(10);
        EntityId secondJob = Id(11);
        EntityId basketJob = Id(12);

        Assert.That(inventory.ReserveResidentSlotCapacity(
            firstJob,
            ResidentId,
            Material,
            quantity: 1,
            tick: 1).IsSuccess, Is.True);
        Assert.That(inventory.ReserveResidentSlotCapacity(
            secondJob,
            ResidentId,
            Material,
            quantity: 1,
            tick: 2).IsSuccess, Is.True);
        Assert.That(inventory.ReserveResidentSlotCapacity(
            basketJob,
            ResidentId,
            LargeBasket,
            quantity: 1,
            tick: 3).IsSuccess, Is.True);
        Assert.That(inventory.ReleaseResidentSlotClaims(basketJob, tick: 4), Is.EqualTo(1));
        Add(inventory, 20, LargeBasket, slot: 5);

        Assert.That(inventory.NormalizeResidentInventory(
            ResidentId,
            tick: 6).IsSuccess, Is.True);
        ResidentInventoryLayoutViewModel model =
            new ResidentInventoryLayoutPresenter().Present(inventory, ResidentId);
        ResidentInventoryLayoutSlotViewModel[] main = model
            .GetCompartment(ResidentInventoryCompartment.Main)
            .OrderBy(value => value.SlotIndex)
            .ToArray();

        Assert.That(main[3].ItemId, Is.EqualTo(LargeBasket.ToString()));
        Assert.That(main[3].DisplayName, Is.EqualTo("Large basket"));
        Assert.That(main[4].IsEmpty, Is.True);
        Assert.That(main[5].IsEmpty, Is.True);
        Assert.That(
            inventory.GetResidentSlotClaims()
                .OrderBy(value => value.Slot.Index)
                .Select(value => value.Slot.Index)
                .ToArray(),
            Is.EqualTo(new[] { 4, 5 }));
    }

    private static InventoryState CreateInventory()
    {
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(
                Material,
                "Mushroom material",
                maximumStackSize: 1,
                isTool: false,
                new[] { General }),
            new ItemDefinition(
                LargeBasket,
                "Large basket",
                maximumStackSize: 1,
                isTool: false,
                new[] { General },
                new InventoryExpansionDefinition(
                    InventoryExpansionGroup.Cargo,
                    tier: 2,
                    addedSlots: 6,
                    acceptedCategories: new[] { General },
                    moveSpeedMultiplierWhenOccupied: 0.65d,
                    visualAttachmentId: "visual.resident.large_basket")),
        }));
    }

    private static void Add(
        InventoryState inventory,
        int id,
        ItemId itemId,
        int slot)
    {
        Assert.That(inventory.AddUnit(
            Id(id),
            itemId,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                slot),
            tick: 0).IsSuccess, Is.True);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
