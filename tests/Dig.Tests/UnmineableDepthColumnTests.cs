using System.Linq;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class UnmineableDepthColumnTests
{
    [Fact]
    public void Making_z0_unmineable_propagates_material_through_every_depth_layer()
    {
        MaterialId rock = new MaterialId("terrain.rock");
        MaterialId bedrock = new MaterialId("terrain.unmineable");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(rock, "Rock", true, 10, true, null),
            new MaterialDefinition(bedrock, "Bedrock", true, int.MaxValue, false, null),
        });
        WorldState world = WorldState.CreateFilled(
            new WorldSize(4, 4, 4),
            chunkSize: 2,
            materials,
            rock,
            explored: true).Value;
        CellId front = new CellId(1, 2, 0);

        Assert.True(world.ApplyTerrainChanges(new[]
        {
            new TerrainChange(front, world.GetCell(front).Value.State.WithTerrain(bedrock)),
        }, tick: 1).IsSuccess);

        CellSnapshot[] column = world.CreateSnapshot().Chunks
            .SelectMany(chunk => chunk.Cells)
            .Where(cell => cell.Id.X == front.X && cell.Id.Y == front.Y)
            .OrderBy(cell => cell.Id.Z)
            .ToArray();
        Assert.Equal(4, column.Length);
        Assert.All(column, cell =>
        {
            Assert.Equal(bedrock, cell.State.MaterialId);
            Assert.False(materials.Get(cell.State.MaterialId)!.IsMineable);
        });
    }
}

}
