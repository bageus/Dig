using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentInventoryMovementCadenceTests
{
    private static readonly EntityId ResidentId = Id(1);
    private static readonly ItemId OreId = new ItemId("ore.iron");
    private static readonly ItemId BasketId = new ItemId("inventory.basket");
    private static readonly ItemId LargeBasketId = new ItemId("inventory.large_basket");

    [Theory]
    [InlineData(1d, 20)]
    [InlineData(0.75d, 15)]
    [InlineData(0.65d, 13)]
    public void Fixed_point_cadence_matches_speed_over_twenty_ticks(
        double speed,
        int expectedMoves)
    {
        int moves = Enumerable.Range(1, 20)
            .Count(tick => ResidentInventoryMovementCadence.IsDue(tick, speed));

        Assert.Equal(expectedMoves, moves);
    }

    [Fact]
    public void Active_occupied_cargo_controls_real_movement_cadence()
    {
        InventoryState inventory = CreateInventory(BasketId);
        Assert.True(inventory.AddStack(
            Id(20),
            OreId,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Cargo,
                0),
            tick: 0).IsSuccess);

        bool[] due = Enumerable.Range(1, 4)
            .Select(tick => inventory.IsResidentMovementDue(ResidentId, tick))
            .ToArray();

        Assert.Equal(new[] { false, true, true, true }, due);
    }

    [Fact]
    public void Emptying_cargo_restores_next_tick_to_full_speed()
    {
        InventoryState inventory = CreateInventory(LargeBasketId);
        EntityId cargoId = Id(20);
        Assert.True(inventory.AddStack(
            cargoId,
            OreId,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Cargo,
                0),
            tick: 0).IsSuccess);
        Assert.False(inventory.IsResidentMovementDue(ResidentId, tick: 1));
        Assert.True(inventory.MoveAvailable(
            cargoId,
            quantity: 1,
            ItemLocation.InWorld(new Dig.Domain.World.CellId(3, 3)),
            splitStackId: default,
            tick: 1).IsSuccess);

        Assert.True(inventory.IsResidentMovementDue(ResidentId, tick: 2));
    }

    private static InventoryState CreateInventory(ItemId expansionId)
    {
        ItemCategoryId raw = new ItemCategoryId("raw");
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(OreId, "Ore", 100, false, new[] { raw }),
            Expansion(BasketId, tier: 1, slots: 4, speed: 0.75d, raw),
            Expansion(LargeBasketId, tier: 2, slots: 6, speed: 0.65d, raw),
        }));
        Assert.True(inventory.AddStack(
            Id(10),
            expansionId,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                0),
            tick: 0).IsSuccess);
        return inventory;
    }

    private static ItemDefinition Expansion(
        ItemId itemId,
        int tier,
        int slots,
        double speed,
        ItemCategoryId raw)
    {
        return new ItemDefinition(
            itemId,
            itemId.ToString(),
            1,
            false,
            new[] { raw },
            new InventoryExpansionDefinition(
                InventoryExpansionGroup.Cargo,
                tier,
                slots,
                new[] { raw },
                speed,
                $"visual.{itemId}"));
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}