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
    public void Normalize_splits_same_item_quantity_into_one_unit_per_slot()
    {
        InventoryState inventory = CreateInventory();
        EntityId original = Id('a');
        Add(inventory, original, Rock, quantity: 3, slotIndex: 0);

        Result normalized = inventory.NormalizeResidentInventory(ResidentId, tick: 1);

        Assert.True(normalized.IsSuccess, normalized.Error?.ToString());
        ResidentInventorySlotSnapshot[] occupied = inventory
            .GetResidentInventoryLayout(ResidentId)
            .Slots
            .Where(value => !value.IsEmpty)
            .OrderBy(value => value.Slot.Index)
            .ToArray();
        Assert.Equal(3, occupied.Length);
        Assert.All(occupied, value => Assert.Equal(1, value.Quantity));
        Assert.Contains(occupied, value => value.StackId == original);
        Assert.Equal(3, inventory.GetTotal(Rock));
        Assert.Equal(3, inventory.CreateSnapshot().Stacks.Count);
    }

    [Fact]
    public void Normalize_keeps_different_item_units_in_separate_slots()
    {
        InventoryState inventory = CreateInventory();
        Add(inventory, Id('a'), Rock, quantity: 1, slotIndex: 0);
        Add(inventory, Id('b'), Tool, quantity: 1, slotIndex: 1);

        Assert.True(inventory.NormalizeResidentInventory(ResidentId, tick: 1).IsSuccess);

        Assert.Equal(2, inventory.GetResidentInventoryLayout(ResidentId)
            .Slots.Count(value => !value.IsEmpty));
        Assert.All(
            inventory.GetResidentInventoryLayout(ResidentId).Slots.Where(value => !value.IsEmpty),
            value => Assert.Equal(1, value.Quantity));
    }

    [Fact]
    public void Normalize_rejects_legacy_multi_unit_stack_while_action_owned()
    {
        InventoryState inventory = CreateInventory();
        EntityId stackId = Id('a');
        EntityId jobId = Id('c');
        Add(inventory, stackId, Rock, quantity: 2, slotIndex: 0);
        Assert.True(inventory.ReserveQuantity(stackId, jobId, 1, tick: 1).IsSuccess);

        Result normalized = inventory.NormalizeResidentInventory(ResidentId, tick: 2);

        Assert.Equal(InventoryErrors.ResidentInventoryLayoutInvalid, normalized.Error);
        Assert.Equal(2, inventory.GetStack(stackId)!.Quantity);
        Assert.Single(inventory.GetResidentInventoryLayout(ResidentId).Slots,
            value => !value.IsEmpty);
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
