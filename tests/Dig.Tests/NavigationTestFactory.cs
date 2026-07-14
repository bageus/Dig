using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

internal static class NavigationTestFactory
{
    public static readonly MaterialId Air = new MaterialId("air");
    public static readonly MaterialId Stone = new MaterialId("stone");

    public static MaterialCatalog CreateMaterials()
    {
        return new MaterialCatalog(new[]
        {
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
            new MaterialDefinition(Stone, isSolid: true, hardness: 10),
        });
    }

    public static WorldState CreateStoneWorld(
        int width,
        int height,
        int chunkSize)
    {
        Result<WorldState> result = WorldState.CreateFilled(
            new WorldSize(width, height),
            chunkSize,
            CreateMaterials(),
            Stone,
            explored: true);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    public static WorldState CreateGroundedCorridor(
        int width,
        int height,
        int chunkSize,
        int corridorY,
        params int[] blockedX)
    {
        WorldState world = CreateStoneWorld(width, height, chunkSize);
        HashSet<int> blocked = new HashSet<int>(blockedX);
        List<TerrainChange> changes = new List<TerrainChange>();
        for (int x = 0; x < width; x++)
        {
            if (blocked.Contains(x))
            {
                continue;
            }

            changes.Add(new TerrainChange(
                new CellId(x, corridorY),
                CreateState(Air)));
        }

        Result<WorldMutationResult> result = world.ApplyTerrainChanges(
            changes,
            tick: 1);
        Assert.True(result.IsSuccess);
        world.DrainDirtyChunks();
        world.DequeueUncommittedEvents();
        return world;
    }

    public static CellState CreateState(MaterialId materialId)
    {
        return new CellState(
            materialId,
            CellDesignation.None,
            isExplored: true,
            damage: 0,
            temperature: 20);
    }

    public static NavigationMap BuildMap(
        WorldState world,
        TraversalProfile profile,
        params TraversalLink[] links)
    {
        NavigationMap map = new NavigationMap(profile);
        Result<NavigationUpdateDiagnostics> result = map.Rebuild(
            world.CreateSnapshot(),
            links);
        Assert.True(result.IsSuccess);
        return map;
    }

    public static NavigationSnapshot GetSnapshot(NavigationMap map)
    {
        Result<NavigationSnapshot> result = map.GetSnapshot();
        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
}
