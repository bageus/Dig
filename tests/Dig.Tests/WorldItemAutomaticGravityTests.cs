using Dig.Application.Inventory;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{
public sealed class WorldItemAutomaticGravityTests
{
    private static readonly MaterialId Rock = new MaterialId("automatic.gravity.rock");
    private static readonly MaterialId Air = new MaterialId("automatic.gravity.air");
    private static readonly ItemId FutureMaterial =
        new ItemId("future.material.without.gravity.registration");
    private static readonly ItemId FutureTool =
        new ItemId("future.tool.without.gravity.registration");

    [Fact]
    public void New_material_and_tool_settle_without_gravity_registration()
    {
        WorldState world = CreateWorld();
        Assert.True(world.ApplyTerrainChanges(
            new[]
            {
                Empty(1, 1), Empty(1, 2), Empty(1, 3),
                Empty(3, 1), Empty(3, 2), Empty(3, 3),
            },
            tick: 1).IsSuccess);
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(
                FutureMaterial,
                "Future material",
                maximumStackSize: 50,
                isTool: false),
            new ItemDefinition(
                FutureTool,
                "Future tool",
                maximumStackSize: 1,
                isTool: true),
        }));
        EntityId materialStack =
            EntityId.Parse("71000000000000000000000000000002");
        EntityId toolStack =
            EntityId.Parse("71000000000000000000000000000003");
        Assert.True(inventory.AddUnit(
            materialStack,
            FutureMaterial,
            ItemLocation.InWorld(new CellId(1, 1, 0)),
            tick: 1).IsSuccess);
        Assert.True(inventory.AddUnit(
            toolStack,
            FutureTool,
            ItemLocation.InWorld(new CellId(3, 1, 0)),
            tick: 1).IsSuccess);

        Result result = WorldItemGravitySettlement.Settle(
            inventory,
            world.CreateSnapshot(),
            tick: 2);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(
            ItemLocation.InWorld(new CellId(1, 3, 0)),
            inventory.GetStack(materialStack)!.Location);
        Assert.Equal(
            ItemLocation.InWorld(new CellId(3, 3, 0)),
            inventory.GetStack(toolStack)!.Location);
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
