using System.Linq;
using Dig.Application.Inventory;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentEquipmentTests
{
    private static readonly ItemId BoxItemId = new ItemId("building_box.workshop");
    private static readonly ItemId ToolItemId = new ItemId("tool.pickaxe");
    private static readonly EntityId ResidentId = Id(1);
    private static readonly EntityId FirstStackId = Id(2);
    private static readonly EntityId SecondStackId = Id(3);

    [Fact]
    public void Use_rejects_second_tool_when_held_reference_is_occupied()
    {
        InventoryState inventory = CreateInventory();
        Assert.True(inventory.AddStack(
            FirstStackId,
            ToolItemId,
            quantity: 1,
            ItemLocation.InAgent(ResidentId),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddStack(
            SecondStackId,
            ToolItemId,
            quantity: 1,
            ItemLocation.InAgent(ResidentId),
            tick: 0).IsSuccess);
        InMemoryInventoryRepository repository = new InMemoryInventoryRepository(inventory);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        UseResidentInventoryItemHandler handler = new UseResidentInventoryItemHandler(
            repository,
            journal);

        Result first = handler.Handle(new UseResidentInventoryItemCommand(
            ResidentId,
            FirstStackId,
            tick: 1));
        Result second = handler.Handle(new UseResidentInventoryItemCommand(
            ResidentId,
            SecondStackId,
            tick: 2));

        Assert.True(first.IsSuccess, first.Error?.ToString());
        Assert.Equal(InventoryErrors.ToolSlotOccupied, second.Error);
        Assert.Equal(ItemLocation.InAgent(ResidentId),
            inventory.GetStack(FirstStackId)!.Location);
        Assert.Equal(ItemLocation.InAgent(ResidentId),
            inventory.GetStack(SecondStackId)!.Location);
        Assert.Equal(FirstStackId, inventory.GetHeldItem(ResidentId)!.Value.StackId);
    }

    [Fact]
    public void Held_tool_is_presented_once_and_cannot_drop_until_released()
    {
        InventoryState inventory = CreateInventory();
        Assert.True(inventory.AddStack(
            FirstStackId,
            ToolItemId,
            quantity: 1,
            ItemLocation.InAgent(ResidentId),
            tick: 0).IsSuccess);
        InMemoryInventoryRepository repository = new InMemoryInventoryRepository(inventory);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        UseResidentInventoryItemHandler use = new UseResidentInventoryItemHandler(
            repository,
            journal);
        Assert.True(use.Handle(new UseResidentInventoryItemCommand(
            ResidentId,
            FirstStackId,
            tick: 1)).IsSuccess);

        ResidentInventorySlotViewModel slot = Assert.Single(
            new ResidentInventoryPresenter(
                BoxItemId,
                inventory.Catalog).Present(
                    inventory.CreateSnapshot(),
                    ResidentId).Slots,
            value => value.StackId == FirstStackId.ToString());

        Assert.True(slot.IsEquipped);
        Assert.True(slot.IsTool);
        Assert.False(slot.CanUse);
        Assert.False(slot.CanDrop);
        Assert.Equal(1, slot.HeldQuantity);

        CellId destination = new CellId(4, 5);
        Result blocked = new DropResidentInventoryStackHandler(
            repository,
            journal).Handle(new DropResidentInventoryStackCommand(
                ResidentId,
                FirstStackId,
                destination,
                tick: 2));
        Assert.Equal(ResidentInventoryActionErrors.StackReserved, blocked.Error);
        Assert.True(inventory.ReleaseHeldItem(ResidentId, tick: 3).IsSuccess);

        Result dropped = new DropResidentInventoryStackHandler(
            repository,
            journal).Handle(new DropResidentInventoryStackCommand(
                ResidentId,
                FirstStackId,
                destination,
                tick: 4));

        Assert.True(dropped.IsSuccess, dropped.Error?.ToString());
        Assert.Equal(ItemLocation.InWorld(destination),
            inventory.GetStack(FirstStackId)!.Location);
        Assert.Equal(1, inventory.GetTotal(ToolItemId));
    }

    private static InventoryState CreateInventory()
    {
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(
                BoxItemId,
                "Workshop BuildingBox",
                maximumStackSize: 1,
                isTool: false),
            new ItemDefinition(
                ToolItemId,
                "Pickaxe",
                maximumStackSize: 1,
                isTool: true),
        }));
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}