using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentInventoryCompactionTests
{
    private static readonly EntityId ResidentId = Id(1);
    private static readonly ItemId Ore = new ItemId("ore.iron");
    private static readonly ItemId Sword = new ItemId("weapon.sword");
    private static readonly ItemId Tool = new ItemId("tool.pickaxe");
    private static readonly ItemId LargeBasket =
        new ItemId("inventory.large_basket");
    private static readonly ItemId Scabbard =
        new ItemId("inventory.scabbard");

    [Fact]
    public void Normalize_compacts_main_and_promotes_cargo_after_weapon_routing()
    {
        InventoryState inventory = CreateInventory();
        Add(inventory, 10, Ore, ResidentInventoryCompartment.Main, 0);
        Add(inventory, 11, LargeBasket, ResidentInventoryCompartment.Main, 2);
        Add(inventory, 12, Scabbard, ResidentInventoryCompartment.Main, 3);
        Add(inventory, 13, Ore, ResidentInventoryCompartment.Main, 4);
        Add(inventory, 14, Sword, ResidentInventoryCompartment.Weapon, 0);
        Add(inventory, 15, Ore, ResidentInventoryCompartment.Cargo, 0);

        Result normalized = inventory.NormalizeResidentInventory(
            ResidentId,
            tick: 1);

        Assert.True(normalized.IsSuccess, normalized.Error?.ToString());
        ResidentInventoryLayoutSnapshot layout =
            inventory.GetResidentInventoryLayout(ResidentId);
        Assert.Equal(
            new[] { 0, 1, 2, 3, 4 },
            layout.Slots
                .Where(slot => slot.Slot.Compartment
                    == ResidentInventoryCompartment.Main && !slot.IsEmpty)
                .Select(slot => slot.Slot.Index)
                .ToArray());
        Assert.Equal(
            new[] { Ore, LargeBasket, Scabbard, Ore, Ore },
            layout.Slots
                .Where(slot => slot.Slot.Compartment
                    == ResidentInventoryCompartment.Main && !slot.IsEmpty)
                .OrderBy(slot => slot.Slot.Index)
                .Select(slot => slot.ItemId!.Value)
                .ToArray());
        Assert.All(
            layout.Slots.Where(slot => slot.Slot.Compartment
                == ResidentInventoryCompartment.Cargo),
            slot => Assert.True(slot.IsEmpty));
        Assert.Equal(
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Weapon,
                0),
            inventory.GetStack(Id(14))!.Location);
        Assert.Equal(
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                4),
            inventory.GetStack(Id(15))!.Location);
        Assert.Equal(6, inventory.CreateSnapshot().Stacks.Count);
        Assert.Equal(3, inventory.GetTotal(Ore));
    }

    [Fact]
    public void Weapon_overflow_compacts_to_main_and_then_cargo()
    {
        InventoryState inventory = CreateInventory();
        Add(inventory, 20, LargeBasket, ResidentInventoryCompartment.Main, 0);
        Add(inventory, 21, Scabbard, ResidentInventoryCompartment.Main, 1);
        Add(inventory, 22, Ore, ResidentInventoryCompartment.Main, 2);
        Add(inventory, 23, Sword, ResidentInventoryCompartment.Weapon, 0);
        Add(inventory, 24, Sword, ResidentInventoryCompartment.Main, 4);
        Add(inventory, 25, Sword, ResidentInventoryCompartment.Cargo, 0);
        Add(inventory, 26, Sword, ResidentInventoryCompartment.Main, 5);
        Add(inventory, 27, Sword, ResidentInventoryCompartment.Cargo, 1);
        Add(inventory, 28, Sword, ResidentInventoryCompartment.Cargo, 2);

        Result normalized = inventory.NormalizeResidentInventory(
            ResidentId,
            tick: 1);

        Assert.True(normalized.IsSuccess, normalized.Error?.ToString());
        Assert.Equal(
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Weapon,
                0),
            inventory.GetStack(Id(23))!.Location);
        Assert.Equal(
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Weapon,
                1),
            inventory.GetStack(Id(24))!.Location);
        Assert.Equal(
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                4),
            inventory.GetStack(Id(25))!.Location);
        Assert.Equal(
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Cargo,
                0),
            inventory.GetStack(Id(28))!.Location);
    }

    [Fact]
    public void Weapon_overflow_claims_weapon_then_main_then_cargo()
    {
        InventoryState inventory = CreateInventory();
        Add(inventory, 40, LargeBasket, ResidentInventoryCompartment.Main, 0);
        Add(inventory, 41, Scabbard, ResidentInventoryCompartment.Main, 1);

        var result = inventory.ReserveResidentSlotCapacity(
            Id(42),
            ResidentId,
            Sword,
            quantity: 9,
            tick: 1);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(2, result.Value.Count(claim => claim.Slot.Compartment
            == ResidentInventoryCompartment.Weapon));
        Assert.Equal(4, result.Value.Count(claim => claim.Slot.Compartment
            == ResidentInventoryCompartment.Main));
        Assert.Equal(3, result.Value.Count(claim => claim.Slot.Compartment
            == ResidentInventoryCompartment.Cargo));
    }

    [Fact]
    public void Held_stack_stays_pinned_while_other_items_compact_around_it()
    {
        InventoryState inventory = CreateInventory();
        Add(inventory, 30, LargeBasket, ResidentInventoryCompartment.Main, 0);
        Add(inventory, 31, Ore, ResidentInventoryCompartment.Main, 2);
        Add(inventory, 32, Tool, ResidentInventoryCompartment.Main, 4);
        Add(inventory, 33, Ore, ResidentInventoryCompartment.Cargo, 0);
        Assert.True(inventory.HoldItem(
            ResidentId,
            Id(32),
            quantity: 1,
            HeldItemPurpose.ToolUse,
            tick: 1).IsSuccess);

        Result normalized = inventory.NormalizeResidentInventory(
            ResidentId,
            tick: 2);

        Assert.True(normalized.IsSuccess, normalized.Error?.ToString());
        Assert.Equal(
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                4),
            inventory.GetStack(Id(32))!.Location);
        Assert.Equal(1, inventory.GetStack(Id(32))!.HeldQuantity);
        Assert.Equal(
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                2),
            inventory.GetStack(Id(33))!.Location);
    }

    private static InventoryState CreateInventory()
    {
        ItemCategoryId raw = new ItemCategoryId("raw");
        ItemCategoryId weapon = new ItemCategoryId("weapon");
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(Ore, "Iron ore", 100, false, new[] { raw }),
            new ItemDefinition(Sword, "Sword", 1, false, new[] { weapon }),
            new ItemDefinition(Tool, "Pickaxe", 1, true, new[] { raw }),
            Expansion(
                LargeBasket,
                InventoryExpansionGroup.Cargo,
                slots: 6,
                speed: 0.65d,
                raw,
                weapon),
            Expansion(
                Scabbard,
                InventoryExpansionGroup.Weapon,
                slots: 2,
                speed: 1d,
                weapon),
        }));
    }

    private static ItemDefinition Expansion(
        ItemId id,
        InventoryExpansionGroup group,
        int slots,
        double speed,
        params ItemCategoryId[] accepted)
    {
        return new ItemDefinition(
            id,
            id.ToString(),
            1,
            false,
            accepted,
            new InventoryExpansionDefinition(
                group,
                tier: 1,
                addedSlots: slots,
                acceptedCategories: accepted,
                moveSpeedMultiplierWhenOccupied: speed,
                visualAttachmentId: $"visual.{id}"));
    }

    private static void Add(
        InventoryState inventory,
        int id,
        ItemId itemId,
        ResidentInventoryCompartment compartment,
        int slot)
    {
        Assert.True(inventory.AddStack(
            Id(id),
            itemId,
            1,
            ItemLocation.InResidentSlot(ResidentId, compartment, slot),
            tick: 0).IsSuccess);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
