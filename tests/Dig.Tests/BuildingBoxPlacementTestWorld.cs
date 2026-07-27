using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{
internal static class BuildingBoxPlacementTestWorld
{
    internal static WorldSnapshot Supported(
        BuildingDefinition definition,
        CellId origin,
        BuildingOrientation orientation,
        IEnumerable<CellId> reachable)
    {
        HashSet<CellId> open = new HashSet<CellId>(
            definition.ResolveFootprint(origin, orientation));
        foreach (CellId cell in reachable)
        {
            open.Add(cell);
        }

        return WithOpenCells(open);
    }

    internal static WorldState SupportedState(IEnumerable<CellId> openCells)
    {
        MaterialId rock = new MaterialId("rock");
        MaterialId air = new MaterialId("air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(rock, isSolid: true, hardness: 100),
            new MaterialDefinition(air, isSolid: false, hardness: 0),
        });
        WorldState world = Require(WorldState.CreateFilled(
            new WorldSize(8, 8),
            chunkSize: 4,
            materials,
            rock,
            explored: true));
        long tick = 1;
        foreach (CellId cell in openCells.Distinct())
        {
            Assert.True(world.Excavate(cell, air, tick++).IsSuccess);
        }

        return world;
    }

    internal static WorldSnapshot Empty()
    {
        MaterialId air = new MaterialId("air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(air, isSolid: false, hardness: 0),
        });
        return Require(WorldState.CreateFilled(
            new WorldSize(8, 8),
            chunkSize: 4,
            materials,
            air,
            explored: true)).CreateSnapshot();
    }

    private static WorldSnapshot WithOpenCells(IEnumerable<CellId> openCells)
    {
        return SupportedState(openCells).CreateSnapshot();
    }

    private static T Require<T>(Result<T> result)
    {
        Assert.True(result.IsSuccess, result.Error?.ToString());
        return result.Value;
    }
}
}
