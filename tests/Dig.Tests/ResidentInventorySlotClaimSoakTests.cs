using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentInventorySlotClaimSoakTests
{
    private static readonly ItemId OreId = new ItemId("ore.iron");
    private static readonly ItemId BasketId = new ItemId("inventory.basket");

    [Fact]
    public void Repeated_multi_resident_claims_leave_no_orphans_or_capacity_growth()
    {
        InventoryState inventory = CreateInventory(residentCount: 8);
        const int iterations = 1000;
        for (int tick = 1; tick <= iterations; tick++)
        {
            EntityId residentId = Id(100 + (tick % 8));
            EntityId jobId = Id(1000 + tick);
            int quantity = 1 + (tick % 73);
            var reserved = inventory.ReserveResidentSlotCapacity(
                jobId,
                residentId,
                OreId,
                quantity,
                tick);
            Assert.True(reserved.IsSuccess, reserved.Error?.ToString());
            Assert.Equal(quantity, reserved.Value.Sum(claim => claim.Quantity));
            Assert.Equal(quantity, inventory.ReleaseResidentSlotClaims(jobId, tick));
            Assert.Empty(inventory.GetResidentSlotClaims(jobId));
        }

        Assert.Empty(inventory.GetResidentSlotClaims());
        Assert.Equal(8, inventory.GetTotal(BasketId));
        Assert.Equal(0, inventory.GetTotal(OreId));
        Assert.True(inventory.ValidateResidentSlotClaims().IsSuccess);
    }

    [Fact]
    public void Concurrent_claim_order_is_deterministic()
    {
        InventoryState first = CreateInventory(residentCount: 1);
        InventoryState second = CreateInventory(residentCount: 1);
        EntityId residentId = Id(100);
        for (int index = 0; index < 12; index++)
        {
            EntityId jobId = Id(2000 + index);
            int quantity = 17 + index;
            Assert.True(first.ReserveResidentSlotCapacity(
                jobId,
                residentId,
                OreId,
                quantity,
                tick: index + 1).IsSuccess);
            Assert.True(second.ReserveResidentSlotCapacity(
                jobId,
                residentId,
                OreId,
                quantity,
                tick: index + 1).IsSuccess);
        }

        string[] left = first.GetResidentSlotClaims()
            .Select(Format)
            .ToArray();
        string[] right = second.GetResidentSlotClaims()
            .Select(Format)
            .ToArray();
        Assert.Equal(left, right);
    }

    private static InventoryState CreateInventory(int residentCount)
    {
        ItemCategoryId raw = new ItemCategoryId("raw");
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(OreId, "Ore", 100, false, new[] { raw }),
            new ItemDefinition(
                BasketId,
                "Basket",
                1,
                false,
                new[] { raw },
                new InventoryExpansionDefinition(
                    InventoryExpansionGroup.Cargo,
                    tier: 1,
                    addedSlots: 4,
                    acceptedCategories: new[] { raw },
                    moveSpeedMultiplierWhenOccupied: 0.75d,
                    visualAttachmentId: "visual.basket")),
        }));
        for (int index = 0; index < residentCount; index++)
        {
            Assert.True(inventory.AddStack(
                Id(5000 + index),
                BasketId,
                1,
                ItemLocation.InResidentSlot(
                    Id(100 + index),
                    ResidentInventoryCompartment.Main,
                    0),
                tick: 0).IsSuccess);
        }

        return inventory;
    }

    private static string Format(ResidentInventorySlotClaimSnapshot claim)
    {
        return $"{claim.JobId}:{claim.ResidentId}:{claim.ItemId}:"
            + $"{claim.Slot.Compartment}:{claim.Slot.Index}:{claim.Quantity}";
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}