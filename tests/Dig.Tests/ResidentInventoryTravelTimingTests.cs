using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentInventoryTravelTimingTests
{
    private static readonly EntityId ResidentId = Id(1);
    private static readonly ItemId OreId = new ItemId("ore.iron");
    private static readonly ItemId BasketId = new ItemId("inventory.basket");
    private static readonly ItemId LargeBasketId =
        new ItemId("inventory.large_basket");

    [Fact]
    public void Empty_cargo_keeps_full_speed_for_movement_eta_and_cost()
    {
        InventoryState inventory = CreateInventory(BasketId);

        ResidentTravelTimingSnapshot timing =
            inventory.ResolveResidentTravelTiming(ResidentId, baseTicks: 12);

        Assert.Equal(1d, timing.SpeedMultiplier);
        Assert.Equal(12, timing.EffectiveTicks);
        Assert.Equal(1d, timing.CostMultiplier);
        Assert.Equal(60,
            inventory.ResolveResidentPathEtaTicks(
                ResidentId,
                pathSteps: 5,
                baseTicksPerStep: 12));
    }

    [Fact]
    public void Occupied_basket_applies_one_point_three_three_cost_multiplier()
    {
        InventoryState inventory = CreateInventory(BasketId);
        AddCargo(inventory, Id(20));

        ResidentTravelTimingSnapshot timing =
            inventory.ResolveResidentTravelTiming(ResidentId, baseTicks: 12);

        Assert.Equal(0.75d, timing.SpeedMultiplier);
        Assert.Equal(16, timing.EffectiveTicks);
        Assert.Equal(4d / 3d, timing.CostMultiplier, precision: 10);
        Assert.Equal(80,
            inventory.ResolveResidentPathEtaTicks(
                ResidentId,
                pathSteps: 5,
                baseTicksPerStep: 12));
    }

    [Fact]
    public void Occupied_large_basket_applies_single_active_tier_multiplier()
    {
        InventoryState inventory = CreateInventory(LargeBasketId);
        Assert.True(inventory.AddStack(
            Id(11),
            BasketId,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                1),
            tick: 0).IsSuccess);
        AddCargo(inventory, Id(20));

        ResidentTravelTimingSnapshot timing =
            inventory.ResolveResidentTravelTiming(ResidentId, baseTicks: 13);

        Assert.Equal(0.65d, timing.SpeedMultiplier);
        Assert.Equal(20, timing.EffectiveTicks);
        Assert.Equal(20d / 13d, timing.CostMultiplier, precision: 10);
    }

    [Fact]
    public void Removing_last_cargo_stack_immediately_restores_full_speed()
    {
        InventoryState inventory = CreateInventory(BasketId);
        EntityId cargoStackId = Id(20);
        AddCargo(inventory, cargoStackId);
        Assert.Equal(16,
            inventory.ResolveResidentTravelTiming(ResidentId, 12).EffectiveTicks);

        Assert.True(inventory.MoveAvailable(
            cargoStackId,
            quantity: 1,
            ItemLocation.InWorld(new Dig.Domain.World.CellId(4, 4)),
            splitStackId: default,
            tick: 1).IsSuccess);

        Assert.Equal(12,
            inventory.ResolveResidentTravelTiming(ResidentId, 12).EffectiveTicks);
    }

    private static InventoryState CreateInventory(ItemId activeExpansionId)
    {
        ItemCategoryId raw = new ItemCategoryId("raw");
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(OreId, "Ore", 100, false, new[] { raw }),
            Expansion(BasketId, "Basket", tier: 1, slots: 4, speed: 0.75d, raw),
            Expansion(
                LargeBasketId,
                "Large basket",
                tier: 2,
                slots: 6,
                speed: 0.65d,
                raw),
        }));
        Assert.True(inventory.AddStack(
            Id(10),
            activeExpansionId,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                0),
            tick: 0).IsSuccess);
        return inventory;
    }

    private static void AddCargo(InventoryState inventory, EntityId stackId)
    {
        Assert.True(inventory.AddStack(
            stackId,
            OreId,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Cargo,
                0),
            tick: 0).IsSuccess);
    }

    private static ItemDefinition Expansion(
        ItemId itemId,
        string name,
        int tier,
        int slots,
        double speed,
        ItemCategoryId accepted)
    {
        return new ItemDefinition(
            itemId,
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
                $"visual.{itemId}"));
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}