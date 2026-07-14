using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class InventoryStateTests
{
    private static readonly ItemId Ore = new ItemId("ore.iron");
    private static readonly ItemId Pickaxe = new ItemId("tool.pickaxe");
    private static readonly EntityId SourceStackId =
        EntityId.Parse("51000000000000000000000000000001");
    private static readonly EntityId FirstJobId =
        EntityId.Parse("52000000000000000000000000000001");
    private static readonly EntityId SecondJobId =
        EntityId.Parse("52000000000000000000000000000002");

    [Fact]
    public void Partial_quantity_reservations_cannot_claim_the_same_unit_twice()
    {
        InventoryState inventory = CreateInventory();
        Assert.True(inventory.AddStack(
            SourceStackId,
            Ore,
            quantity: 10,
            ItemLocation.InWorld(new CellId(2, 3)),
            tick: 0).IsSuccess);

        Assert.True(inventory.ReserveQuantity(
            SourceStackId,
            FirstJobId,
            quantity: 6,
            tick: 1).IsSuccess);
        Assert.True(inventory.ReserveQuantity(
            SourceStackId,
            SecondJobId,
            quantity: 4,
            tick: 1).IsSuccess);
        Result overbooked = inventory.ReserveQuantity(
            SourceStackId,
            EntityId.Parse("52000000000000000000000000000003"),
            quantity: 1,
            tick: 1);

        Assert.Equal(InventoryErrors.InsufficientAvailableQuantity, overbooked.Error);
        ItemStackSnapshot stack = inventory.GetStack(SourceStackId)!;
        Assert.Equal(10, stack.ReservedQuantity);
        Assert.Equal(0, stack.AvailableQuantity);

        Assert.Equal(6, inventory.ReleaseReservations(FirstJobId, tick: 2));
        Assert.Equal(6, inventory.GetStack(SourceStackId)!.AvailableQuantity);
    }

    [Fact]
    public void Repeated_partial_moves_preserve_total_quantity()
    {
        InventoryState inventory = CreateInventory();
        Assert.True(inventory.AddStack(
            SourceStackId,
            Ore,
            quantity: 100,
            ItemLocation.InWorld(new CellId(0, 0)),
            tick: 0).IsSuccess);

        for (int index = 1; index <= 20; index++)
        {
            EntityId splitId = EntityId.Parse(
                $"5300000000000000000000000000{index:D4}");
            Result moved = inventory.MoveAvailable(
                SourceStackId,
                quantity: 1,
                ItemLocation.InBuilding(EntityId.Parse(
                    $"5400000000000000000000000000{index:D4}")),
                splitId,
                tick: index);

            Assert.True(moved.IsSuccess);
            Assert.Equal(100, inventory.GetTotal(Ore));
        }

        InventorySnapshot snapshot = inventory.CreateSnapshot();
        Assert.Equal(100, snapshot.GetTotal(Ore));
        Assert.Equal(80, inventory.GetStack(SourceStackId)!.Quantity);
        Assert.Equal(
            snapshot.Stacks.Count,
            snapshot.Stacks.Select(stack => stack.StackId).Distinct().Count());
    }

    [Fact]
    public void Reserved_partial_move_creates_one_destination_stack_and_conserves_quantity()
    {
        InventoryState inventory = CreateInventory();
        EntityId destinationStackId =
            EntityId.Parse("55000000000000000000000000000001");
        EntityId storageId =
            EntityId.Parse("56000000000000000000000000000001");
        Assert.True(inventory.AddStack(
            SourceStackId,
            Ore,
            quantity: 10,
            ItemLocation.InWorld(new CellId(4, 4)),
            tick: 0).IsSuccess);
        Assert.True(inventory.ReserveQuantity(
            SourceStackId,
            FirstJobId,
            quantity: 4,
            tick: 1).IsSuccess);

        Assert.True(inventory.MoveReserved(
            SourceStackId,
            FirstJobId,
            quantity: 4,
            ItemLocation.InStorage(storageId),
            destinationStackId,
            tick: 2).IsSuccess);

        Assert.Equal(6, inventory.GetStack(SourceStackId)!.Quantity);
        Assert.Equal(4, inventory.GetStack(destinationStackId)!.Quantity);
        Assert.Equal(ItemLocation.InStorage(storageId), inventory.GetStack(destinationStackId)!.Location);
        Assert.Equal(10, inventory.GetTotal(Ore));
        Assert.Equal(0, inventory.GetStack(SourceStackId)!.ReservedQuantity);
    }

    [Fact]
    public void Only_single_unreserved_tool_can_be_equipped()
    {
        InventoryState inventory = CreateInventory();
        EntityId toolStackId = EntityId.Parse("57000000000000000000000000000001");
        EntityId agentId = EntityId.Parse("58000000000000000000000000000001");
        Assert.True(inventory.AddStack(
            toolStackId,
            Pickaxe,
            quantity: 1,
            ItemLocation.InAgent(agentId),
            tick: 0).IsSuccess);

        Assert.True(inventory.EquipTool(toolStackId, agentId, tick: 1).IsSuccess);

        Assert.Equal(ItemLocation.EquippedBy(agentId), inventory.GetStack(toolStackId)!.Location);
    }

    private static InventoryState CreateInventory()
    {
        ItemCategoryId raw = new ItemCategoryId("raw");
        ItemCatalog catalog = new ItemCatalog(new[]
        {
            new ItemDefinition(Ore, "Iron ore", 100, isTool: false, new[] { raw }),
            new ItemDefinition(Pickaxe, "Pickaxe", 1, isTool: true),
        });
        return new InventoryState(catalog);
    }
}
}
