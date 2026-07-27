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
    internal static readonly MaterialId Rock = new MaterialId("rock");
    internal static readonly MaterialId Air = new MaterialId("air");

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
        MaterialCatalog materials = Materials();
        WorldState world = Require(WorldState.CreateFilled(
            new WorldSize(8, 8),
            chunkSize: 4,
            materials,
            Rock,
            explored: true));
        long tick = 1;
        foreach (CellId cell in openCells.Distinct())
        {
            Assert.True(world.Excavate(cell, Air, tick++).IsSuccess);
        }

        return world;
    }

    internal static WorldSnapshot Empty()
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
        });
        return Require(WorldState.CreateFilled(
            new WorldSize(8, 8),
            chunkSize: 4,
            materials,
            Air,
            explored: true)).CreateSnapshot();
    }

    internal static MaterialCatalog Materials()
    {
        return new MaterialCatalog(new[]
        {
            new MaterialDefinition(Rock, isSolid: true, hardness: 100),
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
        });
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
