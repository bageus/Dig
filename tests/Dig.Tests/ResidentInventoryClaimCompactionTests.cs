using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentInventoryClaimCompactionTests
{
    private static readonly EntityId ResidentId = Id(1);
    private static readonly EntityId FirstClaimJobId = Id(10);
    private static readonly EntityId SecondClaimJobId = Id(11);
    private static readonly EntityId BasketClaimJobId = Id(12);
    private static readonly EntityId LargeBasketStackId = Id(20);
    private static readonly ItemId MushroomLeg =
        new ItemId("material.mushroom_leg.claim_compaction");
    private static readonly ItemId MushroomCap =
        new ItemId("material.mushroom_cap.claim_compaction");
    private static readonly ItemId LargeBasket =
        new ItemId("inventory.large_basket.claim_compaction");
    private static readonly ItemCategoryId General =
        new ItemCategoryId("general.claim_compaction");

    [Fact]
    public void Completed_large_basket_compacts_before_outstanding_pickup_claims()
    {
        InventoryState inventory = CreateInventory();
        AddPhysical(inventory, 2, MushroomLeg, slot: 0);
        AddPhysical(inventory, 3, MushroomCap, slot: 1);
        AddPhysical(inventory, 4, MushroomCap, slot: 2);

        Assert.True(inventory.ReserveResidentSlotCapacity(
            FirstClaimJobId,
            ResidentId,
            MushroomCap,
            quantity: 1,
            tick: 1).IsSuccess);
        Assert.True(inventory.ReserveResidentSlotCapacity(
            SecondClaimJobId,
            ResidentId,
            MushroomCap,
            quantity: 1,
            tick: 2).IsSuccess);
        Assert.True(inventory.ReserveResidentSlotCapacity(
            BasketClaimJobId,
            ResidentId,
            LargeBasket,
            quantity: 1,
            tick: 3).IsSuccess);
        Assert.Equal(
            new[] { 3, 4, 5 },
            inventory.GetResidentSlotClaims()
                .OrderBy(value => value.Slot.Index)
                .Select(value => value.Slot.Index)
                .ToArray());

        Assert.Equal(1, inventory.ReleaseResidentSlotClaims(
            BasketClaimJobId,
            tick: 4));
        Assert.True(inventory.AddUnit(
            LargeBasketStackId,
            LargeBasket,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                slotIndex: 5),
            tick: 5).IsSuccess);
        inventory.DequeueUncommittedEvents();

        Result normalized = inventory.NormalizeResidentInventory(
            ResidentId,
            tick: 6);

        Assert.True(normalized.IsSuccess, normalized.Error?.ToString());
        Assert.Equal(
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                slotIndex: 3),
            inventory.GetStack(LargeBasketStackId)!.Location);
        Assert.Equal(
            new[] { 0, 1, 2, 3 },
            inventory.GetResidentInventoryLayout(ResidentId).Slots
                .Where(value => value.Slot.Compartment
                    == ResidentInventoryCompartment.Main && !value.IsEmpty)
                .Select(value => value.Slot.Index)
                .ToArray());

        ResidentInventorySlotClaimSnapshot[] claims = inventory
            .GetResidentSlotClaims()
            .OrderBy(value => value.JobId.ToString())
            .ToArray();
        Assert.Equal(2, claims.Length);
        Assert.Equal(FirstClaimJobId, claims[0].JobId);
        Assert.Equal(4, claims[0].Slot.Index);
        Assert.Equal(SecondClaimJobId, claims[1].JobId);
        Assert.Equal(5, claims[1].Slot.Index);
        Assert.All(claims, value =>
        {
            Assert.Equal(ResidentId, value.ResidentId);
            Assert.Equal(MushroomCap, value.ItemId);
            Assert.Equal(1, value.Quantity);
            Assert.Equal(ResidentInventoryCompartment.Main, value.Slot.Compartment);
        });

        ResidentInventorySlotClaimChanged[] claimEvents = inventory
            .DequeueUncommittedEvents()
            .OfType<ResidentInventorySlotClaimChanged>()
            .ToArray();
        Assert.Equal(4, claimEvents.Length);
        Assert.Equal(2, claimEvents.Count(value => value.Quantity == 0));
        Assert.Equal(2, claimEvents.Count(value => value.Quantity == 1));

        long stableVersion = inventory.Version;
        Assert.True(inventory.NormalizeResidentInventory(
            ResidentId,
            tick: 7).IsSuccess);
        Assert.Equal(stableVersion, inventory.Version);
        Assert.Empty(inventory.DequeueUncommittedEvents());
    }

    private static InventoryState CreateInventory()
    {
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(
                MushroomLeg,
                "Mushroom leg",
                maximumStackSize: 1,
                isTool: false,
                new[] { General }),
            new ItemDefinition(
                MushroomCap,
                "Mushroom cap",
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

    private static void AddPhysical(
        InventoryState inventory,
        int id,
        ItemId itemId,
        int slot)
    {
        Assert.True(inventory.AddUnit(
            Id(id),
            itemId,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                slot),
            tick: 0).IsSuccess);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
