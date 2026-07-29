using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentInventoryStackingTests
{
    private static readonly EntityId ResidentId = Id('1');
    private static readonly ItemId Rock = new ItemId("item.rock");
    private static readonly ItemId Tool = new ItemId("item.tool");

    [Fact]
    public void Normalize_consolidates_same_item_stacks_before_assigning_resident_slots()
    {
        InventoryState inventory = CreateInventory();
        EntityId first = Id('a');
        EntityId second = Id('b');
        Add(inventory, first, Rock, quantity: 1, slotIndex: 0);
        Add(inventory, second, Rock, quantity: 2, slotIndex: 1);

        Result normalized = inventory.NormalizeResidentInventory(ResidentId, tick: 1);

        Assert.True(normalized.IsSuccess, normalized.Error?.ToString());
        ResidentInventorySlotSnapshot occupied = Assert.Single(
            inventory.GetResidentInventoryLayout(ResidentId)
                .Slots.Where(value => !value.IsEmpty));
        Assert.Equal(3, occupied.Quantity);
        Assert.Equal(first, occupied.StackId);
        Assert.Null(inventory.GetStack(second));
    }

    [Fact]
    public void Normalize_never_mixes_different_item_ids()
    {
        InventoryState inventory = CreateInventory();
        Add(inventory, Id('a'), Rock, quantity: 1, slotIndex: 0);
        Add(inventory, Id('b'), Tool, quantity: 1, slotIndex: 1);

        Assert.True(inventory.NormalizeResidentInventory(ResidentId, tick: 1).IsSuccess);

        Assert.Equal(2, inventory.GetResidentInventoryLayout(ResidentId)
            .Slots.Count(value => !value.IsEmpty));
    }

    [Fact]
    public void Normalize_does_not_merge_reserved_resident_stack()
    {
        InventoryState inventory = CreateInventory();
        EntityId first = Id('a');
        EntityId second = Id('b');
        EntityId job = Id('c');
        Add(inventory, first, Rock, quantity: 1, slotIndex: 0);
        Add(inventory, second, Rock, quantity: 1, slotIndex: 1);
        Assert.True(inventory.ReserveQuantity(second, job, 1, tick: 1).IsSuccess);

        Assert.True(inventory.NormalizeResidentInventory(ResidentId, tick: 2).IsSuccess);

        Assert.Equal(2, inventory.GetResidentInventoryLayout(ResidentId)
            .Slots.Count(value => !value.IsEmpty));
        Assert.NotNull(inventory.GetStack(second));
    }

    private static InventoryState CreateInventory()
    {
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(Rock, "Rock", 100, isTool: false),
            new ItemDefinition(Tool, "Tool", 1, isTool: true),
        }));
    }

    private static void Add(
        InventoryState inventory,
        EntityId stackId,
        ItemId itemId,
        int quantity,
        int slotIndex)
    {
        Assert.True(inventory.AddStack(
            stackId,
            itemId,
            quantity,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                slotIndex),
            tick: 0).IsSuccess);
    }

    private static EntityId Id(char suffix)
    {
        return EntityId.Parse(suffix + new string('0', 30) + "1");
    }
}

}
