using System.Collections.Generic;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class CaveRoomProtectionTests
{
    [Fact]
    public void Completed_room_protects_only_roof_sides_and_floor_support()
    {
        CellId entrance = new CellId(10, 9);
        CaveRoomPlanResult planned = new CaveRoomPlanner().Plan(
            CreateWorld(horizontalTunnelY: entrance.Y),
            new ExcavationBoundaryPolicy(20, 14, topRockY: 2),
            CaveRoomPresetKind.Small,
            entrance);
        Assert.True(planned.Succeeded, planned.Detail);

        CellId[] shell = new CaveRoomShellProtectionPolicy()
            .Resolve(planned.Plan!, new WorldSize(20, 14))
            .ToArray();

        Assert.All(planned.Plan!.RoofCells, cell => Assert.Contains(cell, shell));
        Assert.Contains(new CellId(7, 9), shell);
        Assert.Contains(new CellId(13, 9), shell);
        Assert.Contains(new CellId(8, 10), shell);
        Assert.Contains(new CellId(12, 10), shell);
        Assert.DoesNotContain(entrance, shell);
        Assert.All(
            planned.Plan.FrontExcavationCells,
            roomCell => Assert.DoesNotContain(roomCell, shell));
    }

    private static WorldSnapshot CreateWorld(int horizontalTunnelY)
    {
        MaterialId rock = new MaterialId("test.rock");
        MaterialId air = new MaterialId("test.air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(rock, isSolid: true, hardness: 100),
            new MaterialDefinition(air, isSolid: false, hardness: 0),
        });
        WorldState world = WorldState.CreateFilled(
            new WorldSize(20, 14),
            chunkSize: 5,
            materials,
            rock,
            explored: true).Value;
        CellState empty = new CellState(
            air,
            CellDesignation.None,
            isExplored: true,
            damage: 0,
            temperature: 20);
        List<TerrainChange> changes = new List<TerrainChange>();
        for (int x = 1; x < 19; x++)
        {
            changes.Add(new TerrainChange(new CellId(x, horizontalTunnelY), empty));
        }

        world.ApplyTerrainChanges(changes, tick: 1);
        return world.CreateSnapshot();
    }
}

}