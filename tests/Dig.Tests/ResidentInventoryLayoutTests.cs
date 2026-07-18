using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentInventoryLayoutTests
{
    private static readonly EntityId ResidentId =
        EntityId.Parse("a1000000000000000000000000000001");
    private static readonly ItemId Ore = new ItemId("ore.iron");
    private static readonly ItemId Sword = new ItemId("weapon.sword");
    private static readonly ItemId Basket = new ItemId("inventory.basket");
    private static readonly ItemId LargeBasket = new ItemId("inventory.large_basket");
    private static readonly ItemId Scabbard = new ItemId("inventory.scabbard");
    private static readonly ItemId Harness = new ItemId("inventory.harness");

    [Fact]
    public void Adult_resident_always_has_six_main_slots()
    {
        ResidentInventoryLayoutSnapshot layout = CreateInventory()
            .GetResidentInventoryLayout(ResidentId);

        Assert.Equal(6, layout.MainCapacity);
        Assert.Equal(6, layout.Slots.Count);
        Assert.All(layout.Slots, slot =>
        {
            Assert.Equal(ResidentInventoryCompartment.Main, slot.Slot.Compartment);
            Assert.True(slot.IsEmpty);
        });
    }

    [Fact]
    public void Highest_tier_per_group_is_active_without_capacity_summing()
    {
        InventoryState inventory = CreateInventory();
        AddLegacy(inventory, Stack('1'), Basket, tick: 0);
        AddLegacy(inventory, Stack('2'), LargeBasket, tick: 0);
        AddLegacy(inventory, Stack('3'), Scabbard, tick: 0);
        AddLegacy(inventory, Stack('4'), Harness, tick: 0);

        Assert.True(inventory.NormalizeResidentInventory(ResidentId, tick: 1).IsSuccess);
        ResidentInventoryLayoutSnapshot layout =
            inventory.GetResidentInventoryLayout(ResidentId);

        Assert.Equal(6, layout.CargoCapacity);
        Assert.Equal(4, layout.WeaponCapacity);
        Assert.Equal(LargeBasket, layout.ActiveCargoExpansion!.Value.ItemId);
        Assert.Equal(Harness, layout.ActiveWeaponExpansion!.Value.ItemId);
        Assert.Equal(16, layout.Slots.Count);
        Assert.Equal(2, layout.Slots.Count(slot => slot.IsActiveExpansion));
    }

    [Fact]
    public void Cargo_and_weapon_filters_are_enforced_by_domain_moves()
    {
        InventoryState inventory = CreateInventory();
        Assert.True(inventory.AddStack(
            Stack('1'),
            LargeBasket,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                0),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddStack(
            Stack('2'),
            Harness,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                1),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddStack(
            Stack('3'),
            Ore,
            5,
            ItemLocation.InWorld(new CellId(1, 1)),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddStack(
            Stack('4'),
            Sword,
            1,
            ItemLocation.InWorld(new CellId(2, 1)),
            tick: 0).IsSuccess);

        Assert.True(inventory.MoveAvailableToResidentSlot(
            Stack('3'),
            5,
            ResidentId,
            new ResidentInventorySlot(ResidentInventoryCompartment.Cargo, 0),
            default,
            tick: 1).IsSuccess);
        Assert.True(inventory.MoveAvailableToResidentSlot(
            Stack('4'),
            1,
            ResidentId,
            new ResidentInventorySlot(ResidentInventoryCompartment.Weapon, 0),
            default,
            tick: 1).IsSuccess);

        Result rejected = inventory.MoveAvailable(
            Stack('3'),
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Weapon,
                1),
            Stack('5'),
            tick: 2);
        Assert.Equal(InventoryErrors.ResidentSlotCategoryRejected, rejected.Error);
    }

    [Fact]
    public void Compatible_stack_move_merges_into_existing_slot()
    {
        InventoryState inventory = CreateInventory();
        Assert.True(inventory.AddStack(
            Stack('1'),
            Ore,
            5,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                0),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddStack(
            Stack('2'),
            Ore,
            4,
            ItemLocation.InWorld(new CellId(1, 1)),
            tick: 0).IsSuccess);

        Assert.True(inventory.MoveAvailableToResidentSlot(
            Stack('2'),
            4,
            ResidentId,
            new ResidentInventorySlot(ResidentInventoryCompartment.Main, 0),
            default,
            tick: 1).IsSuccess);

        Assert.Equal(9, inventory.GetStack(Stack('1'))!.Quantity);
        Assert.Null(inventory.GetStack(Stack('2')));
        Assert.Equal(9, inventory.GetTotal(Ore));
    }

    [Fact]
    public void Basket_speed_penalty_depends_on_occupied_active_cargo()
    {
        InventoryState inventory = CreateInventory();
        Assert.True(inventory.AddStack(
            Stack('1'),
            Basket,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                0),
            tick: 0).IsSuccess);
        Assert.Equal(1d, inventory.GetResidentMoveSpeedMultiplier(ResidentId));

        Assert.True(inventory.AddStack(
            Stack('2'),
            Ore,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Cargo,
                0),
            tick: 1).IsSuccess);

        Assert.Equal(0.75d, inventory.GetResidentMoveSpeedMultiplier(ResidentId));
    }

    [Fact]
    public void Removing_loaded_active_expansion_requires_transactional_spill()
    {
        InventoryState inventory = CreateInventory();
        Assert.True(inventory.AddStack(
            Stack('1'),
            Basket,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                0),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddStack(
            Stack('2'),
            Ore,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Cargo,
                0),
            tick: 0).IsSuccess);

        Result result = inventory.MoveAvailable(
            Stack('1'),
            1,
            ItemLocation.InWorld(new CellId(3, 3)),
            default,
            tick: 1);

        Assert.Equal(InventoryErrors.ResidentInventorySpillRequired, result.Error);
        Assert.Equal(
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                0),
            inventory.GetStack(Stack('1'))!.Location);
    }

    private static InventoryState CreateInventory()
    {
        ItemCategoryId raw = new ItemCategoryId("raw");
        ItemCategoryId weapon = new ItemCategoryId("weapon");
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(Ore, "Iron ore", 100, false, new[] { raw }),
            new ItemDefinition(Sword, "Sword", 1, false, new[] { weapon }),
            Expansion(Basket, "Basket", InventoryExpansionGroup.Cargo, 1, 4, 0.75d, raw),
            Expansion(LargeBasket, "Large basket", InventoryExpansionGroup.Cargo, 2, 6, 0.65d, raw),
            Expansion(Scabbard, "Scabbard", InventoryExpansionGroup.Weapon, 1, 2, 1d, weapon),
            Expansion(Harness, "Harness", InventoryExpansionGroup.Weapon, 2, 4, 1d, weapon),
        }));
    }

    private static ItemDefinition Expansion(
        ItemId id,
        string name,
        InventoryExpansionGroup group,
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
                group,
                tier,
                slots,
                new[] { accepted },
                speed,
                $"visual.{id}"));
    }

    private static void AddLegacy(
        InventoryState inventory,
        EntityId stackId,
        ItemId itemId,
        long tick)
    {
        Assert.True(inventory.AddStack(
            stackId,
            itemId,
            1,
            ItemLocation.InAgent(ResidentId),
            tick).IsSuccess);
    }

    private static EntityId Stack(char suffix)
    {
        return EntityId.Parse($"b100000000000000000000000000000{suffix}");
    }
}

}
