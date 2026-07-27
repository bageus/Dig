using Dig.Application.Inventory;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class WorldItemGravitySettlementTests
{
    private static readonly MaterialId Rock = new MaterialId("gravity.rock");
    private static readonly MaterialId Air = new MaterialId("gravity.air");
    private static readonly ItemId Ore = new ItemId("gravity.ore");
    private static readonly EntityId StackId =
        EntityId.Parse("71000000000000000000000000000001");

    [Fact]
    public void Excavated_support_moves_free_world_item_to_first_solid_floor()
    {
        WorldState world = CreateWorld();
        Assert.True(world.ApplyTerrainChanges(
            new[]
            {
                Empty(2, 1),
                Empty(2, 2),
                Empty(2, 3),
                Empty(2, 4),
            },
            tick: 1).IsSuccess);
        InventoryState inventory = CreateInventory();
        Assert.True(inventory.AddUnit(
            StackId,
            Ore,
            ItemLocation.InWorld(new CellId(2, 1, 0)),
            tick: 1).IsSuccess);

        Result result = WorldItemGravitySettlement.Settle(
            inventory,
            world.CreateSnapshot(),
            tick: 2);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(
            ItemLocation.InWorld(new CellId(2, 4, 0)),
            inventory.GetStack(StackId)!.Location);
    }

    [Fact]
    public void Reserved_world_item_is_not_relocated_by_support_reconciliation()
    {
        WorldState world = CreateWorld();
        Assert.True(world.ApplyTerrainChanges(
            new[] { Empty(2, 1), Empty(2, 2), Empty(2, 3), Empty(2, 4) },
            tick: 1).IsSuccess);
        InventoryState inventory = CreateInventory();
        Assert.True(inventory.AddUnit(
            StackId,
            Ore,
            ItemLocation.InWorld(new CellId(2, 1, 0)),
            tick: 1).IsSuccess);
        Assert.True(inventory.ReserveQuantity(
            StackId,
            EntityId.Parse("72000000000000000000000000000001"),
            quantity: 1,
            tick: 2).IsSuccess);

        Assert.True(WorldItemGravitySettlement.Settle(
            inventory,
            world.CreateSnapshot(),
            tick: 3).IsSuccess);

        Assert.Equal(
            ItemLocation.InWorld(new CellId(2, 1, 0)),
            inventory.GetStack(StackId)!.Location);
    }

    private static WorldState CreateWorld()
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(Rock, isSolid: true, hardness: 10),
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
        });
        return WorldState.CreateFilled(
            new WorldSize(5, 6),
            chunkSize: 3,
            materials,
            Rock,
            explored: true).Value;
    }

    private static InventoryState CreateInventory()
    {
        ItemCategoryId raw = new ItemCategoryId("raw");
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(Ore, "Ore", 100, isTool: false, new[] { raw }),
        }));
    }

    private static TerrainChange Empty(int x, int y)
    {
        return new TerrainChange(
            new CellId(x, y, 0),
            new CellState(
                Air,
                CellDesignation.None,
                isExplored: true,
                damage: 0,
                temperature: 20));
    }
}

}
