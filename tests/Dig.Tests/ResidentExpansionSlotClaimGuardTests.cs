using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentExpansionSlotClaimGuardTests
{
    private static readonly EntityId ResidentId = Id(1);
    private static readonly EntityId BasketStackId = Id(2);
    private static readonly EntityId JobId = Id(3);
    private static readonly ItemId OreId = new ItemId("ore.iron");
    private static readonly ItemId BasketId = new ItemId("inventory.basket");

    [Fact]
    public void Active_expansion_cannot_move_while_its_compartment_is_claimed()
    {
        InventoryState inventory = CreateInventory();
        Assert.True(inventory.ReserveResidentSlotCapacity(
            JobId,
            ResidentId,
            OreId,
            quantity: 8,
            tick: 1).IsSuccess);

        Result moved = inventory.MoveAvailable(
            BasketStackId,
            quantity: 1,
            ItemLocation.InWorld(new CellId(4, 4)),
            splitStackId: default,
            tick: 2);

        Assert.Equal(InventoryErrors.ResidentSlotClaimConflict, moved.Error);
        Assert.Equal(
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                0),
            inventory.GetStack(BasketStackId)!.Location);
    }

    [Fact]
    public void Transactional_spill_is_blocked_until_claim_is_released()
    {
        InventoryState inventory = CreateInventory();
        Assert.True(inventory.ReserveResidentSlotCapacity(
            JobId,
            ResidentId,
            OreId,
            quantity: 8,
            tick: 1).IsSuccess);

        Result blocked = inventory.DropResidentStackWithSpill(
            BasketStackId,
            ItemLocation.InWorld(new CellId(4, 4)),
            tick: 2);
        Assert.Equal(InventoryErrors.ResidentSlotClaimConflict, blocked.Error);

        Assert.Equal(8, inventory.ReleaseResidentSlotClaims(JobId, tick: 3));
        Assert.True(inventory.DropResidentStackWithSpill(
            BasketStackId,
            ItemLocation.InWorld(new CellId(4, 4)),
            tick: 4).IsSuccess);
    }

    private static InventoryState CreateInventory()
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
                    occupiedSpeedMultiplier: 0.75d,
                    visualToken: "visual.basket")),
        }));
        Assert.True(inventory.AddStack(
            BasketStackId,
            BasketId,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                0),
            tick: 0).IsSuccess);
        return inventory;
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}