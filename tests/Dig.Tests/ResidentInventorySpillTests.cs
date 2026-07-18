using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentInventorySpillTests
{
    private static readonly EntityId ResidentId =
        EntityId.Parse("91000000000000000000000000000001");
    private static readonly EntityId LargeBasketStackId =
        EntityId.Parse("92000000000000000000000000000001");
    private static readonly EntityId BasketStackId =
        EntityId.Parse("92000000000000000000000000000002");
    private static readonly EntityId CargoStackId =
        EntityId.Parse("92000000000000000000000000000003");
    private static readonly EntityId WorldStackId =
        EntityId.Parse("92000000000000000000000000000004");
    private static readonly EntityId ReservationJobId =
        EntityId.Parse("93000000000000000000000000000001");
    private static readonly ItemId OreId = new ItemId("ore.iron");
    private static readonly ItemId BasketId = new ItemId("inventory.basket");
    private static readonly ItemId LargeBasketId =
        new ItemId("inventory.large_basket");

    [Fact]
    public void Dropping_active_expansion_spills_compartment_and_merges_world_stacks()
    {
        InventoryState inventory = CreateLoadedInventory();
        CellId destinationCell = new CellId(4, 5);
        ItemLocation destination = ItemLocation.InWorld(destinationCell);
        Assert.True(inventory.AddStack(
            WorldStackId,
            OreId,
            95,
            destination,
            tick: 0).IsSuccess);
        int oreBefore = inventory.GetTotal(OreId);

        Result result = inventory.DropResidentStackWithSpill(
            LargeBasketStackId,
            destination,
            tick: 1);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Null(inventory.GetStack(CargoStackId));
        Assert.Equal(100, inventory.GetStack(WorldStackId)!.Quantity);
        Assert.Equal(destination, inventory.GetStack(LargeBasketStackId)!.Location);
        Assert.Equal(oreBefore, inventory.GetTotal(OreId));
        ResidentInventoryLayoutSnapshot layout =
            inventory.GetResidentInventoryLayout(ResidentId);
        Assert.Equal(BasketId, layout.ActiveCargoExpansion!.Value.ItemId);
        Assert.Equal(4, layout.CargoCapacity);
        Assert.DoesNotContain(
            layout.Slots,
            slot => slot.Slot.Compartment == ResidentInventoryCompartment.Cargo
                && !slot.IsEmpty);
    }

    [Fact]
    public void Reserved_compartment_content_rolls_back_entire_spill()
    {
        InventoryState inventory = CreateLoadedInventory();
        Assert.True(inventory.ReserveQuantity(
            CargoStackId,
            ReservationJobId,
            2,
            tick: 1).IsSuccess);
        InventorySnapshot before = inventory.CreateSnapshot();
        ItemLocation destination = ItemLocation.InWorld(new CellId(8, 8));

        Result result = inventory.DropResidentStackWithSpill(
            LargeBasketStackId,
            destination,
            tick: 2);

        Assert.Equal(InventoryErrors.InsufficientAvailableQuantity, result.Error);
        InventorySnapshot after = inventory.CreateSnapshot();
        Assert.Equal(before.Version, after.Version);
        Assert.Equal(
            before.GetTotal(OreId),
            after.GetTotal(OreId));
        Assert.Equal(
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                0),
            inventory.GetStack(LargeBasketStackId)!.Location);
        Assert.Equal(
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Cargo,
                5),
            inventory.GetStack(CargoStackId)!.Location);
    }

    [Fact]
    public void Dropping_inactive_expansion_does_not_spill_active_compartment()
    {
        InventoryState inventory = CreateLoadedInventory();
        ItemLocation destination = ItemLocation.InWorld(new CellId(2, 7));

        Result result = inventory.DropResidentStackWithSpill(
            BasketStackId,
            destination,
            tick: 1);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(destination, inventory.GetStack(BasketStackId)!.Location);
        Assert.Equal(
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Cargo,
                5),
            inventory.GetStack(CargoStackId)!.Location);
        Assert.Equal(6, inventory.GetResidentInventoryLayout(ResidentId).CargoCapacity);
    }

    private static InventoryState CreateLoadedInventory()
    {
        ItemCategoryId raw = new ItemCategoryId("raw");
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(OreId, "Iron ore", 100, false, new[] { raw }),
            Expansion(BasketId, "Basket", tier: 1, slots: 4, speed: 0.75d, raw),
            Expansion(LargeBasketId, "Large basket", tier: 2, slots: 6, speed: 0.65d, raw),
        }));
        Assert.True(inventory.AddStack(
            LargeBasketStackId,
            LargeBasketId,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                0),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddStack(
            BasketStackId,
            BasketId,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                1),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddStack(
            CargoStackId,
            OreId,
            5,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Cargo,
                5),
            tick: 0).IsSuccess);
        return inventory;
    }

    private static ItemDefinition Expansion(
        ItemId id,
        string name,
        int tier,
        int slots,
        double speed,
        ItemCategoryId accepted)
    {
        return new ItemDefinition(
            id,
            name,
            1,
            false,
            new[] { accepted },
            new InventoryExpansionDefinition(
                InventoryExpansionGroup.Cargo,
                tier,
                slots,
                new[] { accepted },
                speed,
                $"visual.{id}"));
    }
}

}
