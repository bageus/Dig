using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentInventoryIngressCompactionTests
{
    [Fact]
    public void Expansion_pickup_releases_claim_and_compacts_into_first_main_slot()
    {
        EntityId residentId = Id(1);
        EntityId jobId = Id(2);
        EntityId harnessStackId = Id(3);
        ItemCategoryId weapon = new ItemCategoryId("weapon");
        ItemId pickaxeId = new ItemId("tool.pickaxe");
        ItemId hammerId = new ItemId("tool.hammer");
        ItemId harnessId = new ItemId("inventory.weapon_harness");
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(pickaxeId, "Pickaxe", 1, true, new[] { weapon }),
            new ItemDefinition(hammerId, "Hammer", 1, true, new[] { weapon }),
            new ItemDefinition(
                harnessId,
                "Weapon harness",
                1,
                false,
                new[] { weapon },
                new InventoryExpansionDefinition(
                    InventoryExpansionGroup.Weapon,
                    tier: 1,
                    addedSlots: 4,
                    acceptedCategories: new[] { weapon },
                    moveSpeedMultiplierWhenOccupied: 1d,
                    visualAttachmentId: "visual.weapon_harness")),
        }));
        Assert.True(inventory.AddStack(
            Id(10),
            pickaxeId,
            1,
            ItemLocation.InResidentSlot(
                residentId,
                ResidentInventoryCompartment.Main,
                slotIndex: 0),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddStack(
            Id(11),
            hammerId,
            1,
            ItemLocation.InResidentSlot(
                residentId,
                ResidentInventoryCompartment.Main,
                slotIndex: 1),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddStack(
            harnessStackId,
            harnessId,
            1,
            ItemLocation.InWorld(new Dig.Domain.World.CellId(3, 3, 0)),
            tick: 0).IsSuccess);
        Assert.True(inventory.ReserveQuantity(
            harnessStackId,
            jobId,
            quantity: 1,
            tick: 1).IsSuccess);
        var claimed = inventory.ReserveResidentSlotCapacity(
            jobId,
            residentId,
            harnessId,
            quantity: 1,
            tick: 1);
        Assert.True(claimed.IsSuccess, claimed.Error?.ToString());
        Assert.Equal(
            new ResidentInventorySlot(ResidentInventoryCompartment.Main, 2),
            Assert.Single(claimed.Value).Slot);

        Result acquired = inventory.AcquireReservedIntoResidentSlots(
            harnessStackId,
            jobId,
            residentId,
            destinationStackId: default,
            tick: 2);

        Assert.True(acquired.IsSuccess, acquired.Error?.ToString());
        Assert.Empty(inventory.GetResidentSlotClaims(jobId));
        Assert.Equal(
            ItemLocation.InResidentSlot(
                residentId,
                ResidentInventoryCompartment.Main,
                slotIndex: 0),
            inventory.GetStack(harnessStackId)!.Location);
        Assert.Equal(
            ItemLocation.InResidentSlot(
                residentId,
                ResidentInventoryCompartment.Weapon,
                slotIndex: 0),
            inventory.GetStack(Id(10))!.Location);
        Assert.Equal(
            ItemLocation.InResidentSlot(
                residentId,
                ResidentInventoryCompartment.Weapon,
                slotIndex: 1),
            inventory.GetStack(Id(11))!.Location);
    }

    private static EntityId Id(int value) =>
        EntityId.Parse(value.ToString("x32"));
}

}
