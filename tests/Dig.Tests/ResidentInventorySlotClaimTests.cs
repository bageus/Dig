using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentInventorySlotClaimTests
{
    private static readonly EntityId ResidentId = Id(1);
    private static readonly EntityId FirstJobId = Id(2);
    private static readonly EntityId SecondJobId = Id(3);
    private static readonly EntityId OreStackId = Id(4);
    private static readonly EntityId BasketStackId = Id(5);
    private static readonly ItemId OreId = new ItemId("ore.iron");
    private static readonly ItemId SwordId = new ItemId("weapon.sword");
    private static readonly ItemId BasketId = new ItemId("inventory.basket");

    [Fact]
    public void Existing_cargo_item_compacts_into_main_before_new_claim()
    {
        InventoryState inventory = CreateInventory(withBasket: true);
        Assert.True(inventory.AddStack(
            OreStackId,
            OreId,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Cargo,
                0),
            tick: 0).IsSuccess);

        Result<System.Collections.Generic.IReadOnlyList<ResidentInventorySlotClaimSnapshot>>
            result = inventory.ReserveResidentSlotCapacity(
                FirstJobId,
                ResidentId,
                OreId,
                quantity: 1,
                tick: 1);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        ResidentInventorySlotClaimSnapshot claim = Assert.Single(result.Value);
        Assert.Equal(
            new ResidentInventorySlot(ResidentInventoryCompartment.Main, 2),
            claim.Slot);
        Assert.Equal(1, claim.Quantity);
    }

    [Fact]
    public void Compaction_places_physical_item_before_reflowed_incoming_claim()
    {
        InventoryState inventory = CreateInventory(withBasket: true);
        var reserved = inventory.ReserveResidentSlotCapacity(
            FirstJobId,
            ResidentId,
            OreId,
            quantity: 1,
            tick: 1);
        Assert.True(reserved.IsSuccess, reserved.Error?.ToString());
        Assert.Equal(
            new ResidentInventorySlot(ResidentInventoryCompartment.Main, 1),
            Assert.Single(reserved.Value).Slot);
        Assert.True(inventory.AddStack(
            OreStackId,
            OreId,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Cargo,
                0),
            tick: 2).IsSuccess);

        Result normalized = inventory.NormalizeResidentInventory(
            ResidentId,
            tick: 3);

        Assert.True(normalized.IsSuccess, normalized.Error?.ToString());
        Assert.Equal(
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                1),
            inventory.GetStack(OreStackId)!.Location);
        ResidentInventorySlotClaimSnapshot claim = Assert.Single(
            inventory.GetResidentSlotClaims(FirstJobId));
        Assert.Equal(FirstJobId, claim.JobId);
        Assert.Equal(ResidentId, claim.ResidentId);
        Assert.Equal(OreId, claim.ItemId);
        Assert.Equal(1, claim.Quantity);
        Assert.Equal(
            new ResidentInventorySlot(ResidentInventoryCompartment.Main, 2),
            claim.Slot);
    }

    [Fact]
    public void Filled_main_skips_occupied_same_item_cargo_slot_and_uses_next_empty_slot()
    {
        InventoryState inventory = CreateInventory(withBasket: true);
        for (int index = 1; index < ResidentInventoryLayoutSnapshot.MainSlotCount; index++)
        {
            Assert.True(inventory.AddStack(
                Id(20 + index),
                OreId,
                1,
                ItemLocation.InResidentSlot(
                    ResidentId,
                    ResidentInventoryCompartment.Main,
                    index),
                tick: 0).IsSuccess);
        }

        Assert.True(inventory.AddStack(
            OreStackId,
            OreId,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Cargo,
                0),
            tick: 0).IsSuccess);

        var result = inventory.ReserveResidentSlotCapacity(
            FirstJobId,
            ResidentId,
            OreId,
            quantity: 1,
            tick: 1);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        ResidentInventorySlotClaimSnapshot claim = Assert.Single(result.Value);
        Assert.Equal(
            new ResidentInventorySlot(ResidentInventoryCompartment.Cargo, 1),
            claim.Slot);
        Assert.Equal(1, claim.Quantity);
    }

    [Fact]
    public void Concurrent_jobs_cannot_overbook_the_same_slot_capacity()
    {
        InventoryState inventory = CreateInventory(withBasket: false);

        var first = inventory.ReserveResidentSlotCapacity(
            FirstJobId,
            ResidentId,
            OreId,
            quantity: 5,
            tick: 1);
        var second = inventory.ReserveResidentSlotCapacity(
            SecondJobId,
            ResidentId,
            OreId,
            quantity: 1,
            tick: 2);

        Assert.True(first.IsSuccess, first.Error?.ToString());
        Assert.True(second.IsSuccess, second.Error?.ToString());
        Assert.NotEqual(first.Value[0].Slot, second.Value[0].Slot);
        Assert.Equal(6, inventory.GetResidentSlotClaims().Sum(claim => claim.Quantity));
    }

    [Fact]
    public void Slot_claim_reservation_is_idempotent_for_same_job()
    {
        InventoryState inventory = CreateInventory(withBasket: false);

        var first = inventory.ReserveResidentSlotCapacity(
            FirstJobId,
            ResidentId,
            OreId,
            quantity: 3,
            tick: 1);
        long version = inventory.Version;
        var repeated = inventory.ReserveResidentSlotCapacity(
            FirstJobId,
            ResidentId,
            OreId,
            quantity: 3,
            tick: 2);

        Assert.True(first.IsSuccess, first.Error?.ToString());
        Assert.True(repeated.IsSuccess, repeated.Error?.ToString());
        Assert.Equal(version, inventory.Version);
        Assert.Equal(first.Value.ToArray(), repeated.Value.ToArray());
    }

    [Fact]
    public void Release_clears_all_claimed_capacity_for_job()
    {
        InventoryState inventory = CreateInventory(withBasket: true);
        Assert.True(inventory.ReserveResidentSlotCapacity(
            FirstJobId,
            ResidentId,
            OreId,
            quantity: 9,
            tick: 1).IsSuccess);

        int released = inventory.ReleaseResidentSlotClaims(FirstJobId, tick: 2);

        Assert.Equal(9, released);
        Assert.Empty(inventory.GetResidentSlotClaims(FirstJobId));
        Assert.True(inventory.ReserveResidentSlotCapacity(
            SecondJobId,
            ResidentId,
            OreId,
            quantity: 9,
            tick: 3).IsSuccess);
    }

    [Fact]
    public void Ordinary_resources_never_claim_weapon_slots()
    {
        InventoryState inventory = CreateInventory(withBasket: false, withWeaponSlots: true);
        var result = inventory.ReserveResidentSlotCapacity(
            FirstJobId,
            ResidentId,
            OreId,
            quantity: 5,
            tick: 1);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(5, result.Value.Count);
        Assert.All(result.Value, claim =>
            Assert.Equal(ResidentInventoryCompartment.Main, claim.Slot.Compartment));
    }

    [Fact]
    public void Weapon_items_can_claim_active_weapon_slots()
    {
        InventoryState inventory = CreateInventory(withBasket: false, withWeaponSlots: true);
        var result = inventory.ReserveResidentSlotCapacity(
            FirstJobId,
            ResidentId,
            SwordId,
            quantity: 2,
            tick: 1);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(ResidentInventoryCompartment.Weapon, result.Value[0].Slot.Compartment);
    }

    [Fact]
    public void Snapshot_restore_preserves_exact_slot_claims()
    {
        InventoryState inventory = CreateInventory(withBasket: true);
        Assert.True(inventory.ReserveResidentSlotCapacity(
            FirstJobId,
            ResidentId,
            OreId,
            quantity: 7,
            tick: 1).IsSuccess);
        InventorySnapshot snapshot = inventory.CreateSnapshot();

        Result<InventoryState> restored = InventoryState.Restore(snapshot, inventory.Catalog);

        Assert.True(restored.IsSuccess, restored.Error?.ToString());
        Assert.Equal(snapshot.Version, restored.Value.Version);
        Assert.Equal(
            snapshot.ResidentSlotClaims.ToArray(),
            restored.Value.CreateSnapshot().ResidentSlotClaims.ToArray());
    }

    private static InventoryState CreateInventory(
        bool withBasket,
        bool withWeaponSlots = false)
    {
        ItemCategoryId raw = new ItemCategoryId("raw");
        ItemCategoryId weapon = new ItemCategoryId("weapon");
        ItemId harnessId = new ItemId("inventory.harness");
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(OreId, "Iron ore", 100, false, new[] { raw }),
            new ItemDefinition(SwordId, "Sword", 1, false, new[] { weapon }),
            Expansion(
                BasketId,
                "Basket",
                InventoryExpansionGroup.Cargo,
                4,
                new[] { raw, weapon }),
            Expansion(
                harnessId,
                "Harness",
                InventoryExpansionGroup.Weapon,
                4,
                new[] { weapon }),
        }));
        if (withBasket)
        {
            Assert.True(inventory.AddStack(
                BasketStackId,
                BasketId,
                1,
                ItemLocation.InResidentSlot(
                    ResidentId,
                    ResidentInventoryCompartment.Main,
                    0),
                tick: 0).IsSuccess);
        }

        if (withWeaponSlots)
        {
            Assert.True(inventory.AddStack(
                Id(6),
                harnessId,
                1,
                ItemLocation.InResidentSlot(
                    ResidentId,
                    ResidentInventoryCompartment.Main,
                    withBasket ? 1 : 0),
                tick: 0).IsSuccess);
        }

        return inventory;
    }

    private static ItemDefinition Expansion(
        ItemId itemId,
        string name,
        InventoryExpansionGroup group,
        int slots,
        ItemCategoryId[] accepted)
    {
        return new ItemDefinition(
            itemId,
            name,
            1,
            false,
            accepted,
            new InventoryExpansionDefinition(
                group,
                tier: 1,
                addedSlots: slots,
                acceptedCategories: accepted,
                moveSpeedMultiplierWhenOccupied: 0.75d,
                visualAttachmentId: $"visual.{itemId}"));
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
