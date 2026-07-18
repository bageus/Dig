using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class HeldItemReferenceTests
{
    private static readonly EntityId ResidentId =
        EntityId.Parse("81000000000000000000000000000001");
    private static readonly EntityId FirstStackId =
        EntityId.Parse("82000000000000000000000000000001");
    private static readonly EntityId SecondStackId =
        EntityId.Parse("82000000000000000000000000000002");
    private static readonly ItemId PickaxeId = new ItemId("tool.pickaxe");
    private static readonly ItemId HammerId = new ItemId("tool.hammer");

    [Fact]
    public void Tool_stays_in_original_slot_while_held()
    {
        InventoryState inventory = CreateInventory();
        ItemLocation slot = ItemLocation.InResidentSlot(
            ResidentId,
            ResidentInventoryCompartment.Main,
            0);
        AddTool(inventory, FirstStackId, PickaxeId, slot);

        Result result = inventory.EquipTool(FirstStackId, ResidentId, tick: 1);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        ItemStackSnapshot stack = inventory.GetStack(FirstStackId)!;
        Assert.Equal(slot, stack.Location);
        Assert.Equal(1, stack.HeldQuantity);
        Assert.Equal(0, stack.AvailableQuantity);
        HeldItemReferenceSnapshot held = inventory.GetHeldItem(ResidentId)!.Value;
        Assert.Equal(FirstStackId, held.StackId);
        Assert.Equal(HeldItemPurpose.ToolUse, held.Purpose);
        Assert.Equal(1, inventory.GetTotal(PickaxeId));
    }

    [Fact]
    public void Held_stack_cannot_be_moved_or_dropped()
    {
        InventoryState inventory = CreateInventory();
        ItemLocation slot = ItemLocation.InResidentSlot(
            ResidentId,
            ResidentInventoryCompartment.Main,
            0);
        AddTool(inventory, FirstStackId, PickaxeId, slot);
        Assert.True(inventory.EquipTool(FirstStackId, ResidentId, tick: 1).IsSuccess);

        Result moved = inventory.MoveAvailable(
            FirstStackId,
            1,
            ItemLocation.InWorld(new CellId(3, 3)),
            default,
            tick: 2);

        Assert.Equal(InventoryErrors.InsufficientAvailableQuantity, moved.Error);
        Assert.Equal(slot, inventory.GetStack(FirstStackId)!.Location);
        Assert.Equal(1, inventory.GetTotal(PickaxeId));
    }

    [Fact]
    public void Releasing_held_item_restores_available_quantity()
    {
        InventoryState inventory = CreateInventory();
        AddTool(
            inventory,
            FirstStackId,
            PickaxeId,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                0));
        Assert.True(inventory.EquipTool(FirstStackId, ResidentId, tick: 1).IsSuccess);

        Result released = inventory.ReleaseHeldItem(ResidentId, tick: 2);

        Assert.True(released.IsSuccess, released.Error?.ToString());
        Assert.Null(inventory.GetHeldItem(ResidentId));
        Assert.Equal(0, inventory.GetStack(FirstStackId)!.HeldQuantity);
        Assert.Equal(1, inventory.GetStack(FirstStackId)!.AvailableQuantity);
    }

    [Fact]
    public void Switching_tools_changes_reference_without_moving_either_stack()
    {
        InventoryState inventory = CreateInventory();
        ItemLocation firstSlot = ItemLocation.InResidentSlot(
            ResidentId,
            ResidentInventoryCompartment.Main,
            0);
        ItemLocation secondSlot = ItemLocation.InResidentSlot(
            ResidentId,
            ResidentInventoryCompartment.Main,
            1);
        AddTool(inventory, FirstStackId, PickaxeId, firstSlot);
        AddTool(inventory, SecondStackId, HammerId, secondSlot);
        Assert.True(inventory.EquipTool(FirstStackId, ResidentId, tick: 1).IsSuccess);

        Result switched = inventory.SwitchTool(SecondStackId, ResidentId, tick: 2);

        Assert.True(switched.IsSuccess, switched.Error?.ToString());
        Assert.Equal(firstSlot, inventory.GetStack(FirstStackId)!.Location);
        Assert.Equal(secondSlot, inventory.GetStack(SecondStackId)!.Location);
        Assert.Equal(0, inventory.GetStack(FirstStackId)!.HeldQuantity);
        Assert.Equal(1, inventory.GetStack(SecondStackId)!.HeldQuantity);
        Assert.Equal(SecondStackId, inventory.GetHeldItem(ResidentId)!.Value.StackId);
    }

    [Fact]
    public void Restore_recreates_held_reference_and_slot()
    {
        InventoryState source = CreateInventory();
        ItemLocation slot = ItemLocation.InResidentSlot(
            ResidentId,
            ResidentInventoryCompartment.Main,
            0);
        AddTool(source, FirstStackId, PickaxeId, slot);
        Assert.True(source.EquipTool(FirstStackId, ResidentId, tick: 1).IsSuccess);
        InventorySnapshot snapshot = source.CreateSnapshot();

        Result<InventoryState> restored = InventoryState.Restore(
            snapshot,
            source.Catalog);

        Assert.True(restored.IsSuccess, restored.Error?.ToString());
        Assert.Equal(slot, restored.Value.GetStack(FirstStackId)!.Location);
        Assert.Equal(1, restored.Value.GetStack(FirstStackId)!.HeldQuantity);
        Assert.Equal(FirstStackId, restored.Value.GetHeldItem(ResidentId)!.Value.StackId);
        Assert.Equal(snapshot.Version, restored.Value.Version);
    }

    [Fact]
    public void Layout_marks_held_stack_without_duplicating_icon()
    {
        InventoryState inventory = CreateInventory();
        AddTool(
            inventory,
            FirstStackId,
            PickaxeId,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                0));
        Assert.True(inventory.EquipTool(FirstStackId, ResidentId, tick: 1).IsSuccess);

        ResidentInventoryLayoutViewModel layout =
            new ResidentInventoryLayoutPresenter(new ItemId("building.box"))
                .Present(inventory, ResidentId);

        ResidentInventoryLayoutSlotViewModel slot = layout.Slots[0];
        Assert.True(slot.IsHeld);
        Assert.Equal(1, slot.HeldQuantity);
        Assert.Equal(0, slot.AvailableQuantity);
        Assert.False(slot.CanDrop);
        Assert.Single(layout.Slots, value => value.StackId == FirstStackId.ToString());
    }

    private static InventoryState CreateInventory()
    {
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(PickaxeId, "Pickaxe", 1, isTool: true),
            new ItemDefinition(HammerId, "Hammer", 1, isTool: true),
        }));
    }

    private static void AddTool(
        InventoryState inventory,
        EntityId stackId,
        ItemId itemId,
        ItemLocation location)
    {
        Assert.True(inventory.AddStack(
            stackId,
            itemId,
            1,
            location,
            tick: 0).IsSuccess);
    }
}

}
