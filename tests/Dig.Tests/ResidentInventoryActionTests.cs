using Dig.Application.Inventory;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentInventoryActionTests
{
    private static readonly ItemId BoxItemId = new ItemId("building_box.workshop");
    private static readonly ItemId ToolItemId = new ItemId("tool.pickaxe");
    private static readonly ItemId RockItemId = new ItemId("rock.chunk");

    [Fact]
    public void Drop_moves_full_carried_stack_to_actor_cell_without_duplication()
    {
        Harness harness = new Harness(BoxItemId, quantity: 1);
        CellId destination = new CellId(4, 6);

        Result result = harness.Drop(destination);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        ItemStackSnapshot stack = harness.Inventory.GetStack(harness.StackId)!;
        Assert.Equal(ItemLocation.InWorld(destination), stack.Location);
        Assert.Equal(1, stack.Quantity);
        Assert.Equal(1, harness.Inventory.GetTotal(BoxItemId));
    }

    [Fact]
    public void Drop_rejects_stack_carried_by_another_resident()
    {
        Harness harness = new Harness(BoxItemId, quantity: 1);

        Result result = new DropResidentInventoryStackHandler(
            harness.Repository,
            harness.Journal).Handle(new DropResidentInventoryStackCommand(
                Id(99),
                harness.StackId,
                new CellId(2, 2),
                tick: 10));

        Assert.True(result.IsFailure);
        Assert.Equal(ResidentInventoryActionErrors.StackNotCarriedByActor, result.Error);
        Assert.Equal(ItemLocation.InAgent(harness.ActorId),
            harness.Inventory.GetStack(harness.StackId)!.Location);
    }

    [Fact]
    public void Reserved_stack_cannot_be_dropped()
    {
        Harness harness = new Harness(BoxItemId, quantity: 1);
        Assert.True(harness.Inventory.ReserveQuantity(
            harness.StackId,
            Id(50),
            quantity: 1,
            tick: 2).IsSuccess);

        Result result = harness.Drop(new CellId(3, 3));

        Assert.True(result.IsFailure);
        Assert.Equal(ResidentInventoryActionErrors.StackReserved, result.Error);
        Assert.Equal(ItemLocation.InAgent(harness.ActorId),
            harness.Inventory.GetStack(harness.StackId)!.Location);
    }

    [Fact]
    public void Use_holds_owned_unreserved_tool_in_original_slot()
    {
        Harness harness = new Harness(ToolItemId, quantity: 1);
        ItemLocation original = ItemLocation.InAgent(harness.ActorId);

        Result result = harness.Use();

        Assert.True(result.IsSuccess, result.Error?.ToString());
        ItemStackSnapshot stack = harness.Inventory.GetStack(harness.StackId)!;
        Assert.Equal(original, stack.Location);
        Assert.Equal(1, stack.HeldQuantity);
        Assert.Equal(harness.StackId, harness.Inventory.GetHeldItem(harness.ActorId)!.Value.StackId);
        Assert.Equal(1, harness.Inventory.GetTotal(ToolItemId));
    }

    [Fact]
    public void Use_rejects_non_tool_without_moving_it()
    {
        Harness harness = new Harness(RockItemId, quantity: 4);

        Result result = harness.Use();

        Assert.True(result.IsFailure);
        Assert.Equal(ResidentInventoryActionErrors.ItemNotUsable, result.Error);
        ItemStackSnapshot stack = harness.Inventory.GetStack(harness.StackId)!;
        Assert.Equal(ItemLocation.InAgent(harness.ActorId), stack.Location);
        Assert.Equal(4, stack.Quantity);
    }

    [Fact]
    public void Use_rejects_reserved_tool()
    {
        Harness harness = new Harness(ToolItemId, quantity: 1);
        Assert.True(harness.Inventory.ReserveQuantity(
            harness.StackId,
            Id(50),
            quantity: 1,
            tick: 2).IsSuccess);

        Result result = harness.Use();

        Assert.True(result.IsFailure);
        Assert.Equal(ResidentInventoryActionErrors.StackReserved, result.Error);
        Assert.Equal(ItemLocation.InAgent(harness.ActorId),
            harness.Inventory.GetStack(harness.StackId)!.Location);
    }

    private sealed class Harness
    {
        public Harness(ItemId itemId, int quantity)
        {
            ActorId = Id(1);
            StackId = Id(2);
            Inventory = new InventoryState(new ItemCatalog(new[]
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
                new ItemDefinition(
                    RockItemId,
                    "Rock chunk",
                    maximumStackSize: 100,
                    isTool: false),
            }));
            Assert.True(Inventory.AddStack(
                StackId,
                itemId,
                quantity,
                ItemLocation.InAgent(ActorId),
                tick: 1).IsSuccess);
            Repository = new InMemoryInventoryRepository(Inventory);
            Journal = new InMemoryExecutionJournal();
        }

        public EntityId ActorId { get; }
        public EntityId StackId { get; }
        public InventoryState Inventory { get; }
        public InMemoryInventoryRepository Repository { get; }
        public InMemoryExecutionJournal Journal { get; }

        public Result Drop(CellId destination)
        {
            return new DropResidentInventoryStackHandler(Repository, Journal).Handle(
                new DropResidentInventoryStackCommand(
                    ActorId,
                    StackId,
                    destination,
                    tick: 10));
        }

        public Result Use()
        {
            return new UseResidentInventoryItemHandler(Repository, Journal).Handle(
                new UseResidentInventoryItemCommand(
                    ActorId,
                    StackId,
                    tick: 10));
        }
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}