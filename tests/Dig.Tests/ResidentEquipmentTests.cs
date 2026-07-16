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
    public void Use_rejects_second_tool_when_equipped_slot_is_occupied()
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
        Assert.Equal(ItemLocation.EquippedBy(ResidentId),
            inventory.GetStack(FirstStackId)!.Location);
        Assert.Equal(ItemLocation.InAgent(ResidentId),
            inventory.GetStack(SecondStackId)!.Location);
    }

    [Fact]
    public void Equipped_tool_is_presented_and_can_be_dropped_without_duplication()
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

        ResidentInventorySlotViewModel slot = new ResidentInventoryPresenter(
            BoxItemId,
            inventory.Catalog).Present(
                inventory.CreateSnapshot(),
                ResidentId).Slots[0];

        Assert.True(slot.IsEquipped);
        Assert.True(slot.IsTool);
        Assert.False(slot.CanUse);
        Assert.True(slot.CanDrop);

        CellId destination = new CellId(4, 5);
        Result dropped = new DropResidentInventoryStackHandler(
            repository,
            journal).Handle(new DropResidentInventoryStackCommand(
                ResidentId,
                FirstStackId,
                destination,
                tick: 2));

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