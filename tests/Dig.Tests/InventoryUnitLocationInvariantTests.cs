using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class InventoryUnitLocationInvariantTests
{
    private static readonly ItemId Stone = new ItemId("material.stone");
    private static readonly EntityId StackId =
        EntityId.Parse("79000000000000000000000000000001");

    [Theory]
    [InlineData(ItemLocationKind.World)]
    [InlineData(ItemLocationKind.AgentInventory)]
    public void Physical_locations_reject_aggregate_quantities(ItemLocationKind kind)
    {
        InventoryState inventory = CreateInventory();
        ItemLocation location = kind == ItemLocationKind.World
            ? ItemLocation.InWorld(new CellId(2, 3, 0))
            : ItemLocation.InAgent(
                EntityId.Parse("79000000000000000000000000000002"));

        Result result = inventory.AddStack(
            StackId,
            Stone,
            quantity: 3,
            location,
            tick: 0);

        Assert.Equal(InventoryErrors.UnitLocationRequiresSingleItem, result.Error);
        Assert.Null(inventory.GetStack(StackId));
    }

    [Fact]
    public void Storage_may_still_hold_aggregate_quantity_during_migration()
    {
        InventoryState inventory = CreateInventory();

        Result result = inventory.AddStack(
            StackId,
            Stone,
            quantity: 3,
            ItemLocation.InStorage(
                EntityId.Parse("79000000000000000000000000000003")),
            tick: 0);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(3, inventory.GetStack(StackId)!.Quantity);
    }

    private static InventoryState CreateInventory()
    {
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(Stone, "Stone", maximumStackSize: 20, isTool: false),
        }));
    }
}

}