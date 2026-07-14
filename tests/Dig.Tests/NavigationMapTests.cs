using System;
using System.IO;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class NavigationMapTests
{
    [Fact]
    public void Opening_passage_rebuilds_only_changed_chunk()
    {
        WorldState world = NavigationTestFactory.CreateGroundedCorridor(
            width: 12,
            height: 4,
            chunkSize: 4,
            corridorY: 1,
            blockedX: 5);
        NavigationMap map = NavigationTestFactory.BuildMap(
            world,
            TraversalProfile.CreateGroundedDwarf());
        NavigationSnapshot before = NavigationTestFactory.GetSnapshot(map);
        Assert.NotEqual(
            GetRegion(before, new CellId(0, 1)),
            GetRegion(before, new CellId(11, 1)));

        Result<WorldMutationResult> mutation = world.Excavate(
            new CellId(5, 1),
            NavigationTestFactory.Air,
            tick: 2);
        Assert.True(mutation.IsSuccess);

        Result<NavigationUpdateDiagnostics> refresh = map.Refresh(
            world.CreateSnapshot(),
            mutation.Value.InvalidatedChunks,
            Array.Empty<TraversalLink>());

        Assert.True(refresh.IsSuccess);
        Assert.False(refresh.Value.FullRebuild);
        Assert.Equal(new[] { new ChunkId(1, 0) }, refresh.Value.RebuiltChunks);
        Assert.Equal(16, refresh.Value.ExtractedCellCount);
        NavigationSnapshot after = NavigationTestFactory.GetSnapshot(map);
        Assert.Equal(
            GetRegion(after, new CellId(0, 1)),
            GetRegion(after, new CellId(11, 1)));
    }

    [Fact]
    public void Boundary_change_rebuilds_owner_and_neighbor_chunks()
    {
        WorldState world = NavigationTestFactory.CreateGroundedCorridor(
            width: 12,
            height: 4,
            chunkSize: 4,
            corridorY: 1,
            blockedX: 4);
        NavigationMap map = NavigationTestFactory.BuildMap(
            world,
            TraversalProfile.CreateGroundedDwarf());

        Result<WorldMutationResult> mutation = world.Excavate(
            new CellId(4, 1),
            NavigationTestFactory.Air,
            tick: 2);
        Assert.True(mutation.IsSuccess);
        Result<NavigationUpdateDiagnostics> refresh = map.Refresh(
            world.CreateSnapshot(),
            mutation.Value.InvalidatedChunks,
            Array.Empty<TraversalLink>());

        Assert.True(refresh.IsSuccess);
        Assert.Equal(
            new[] { new ChunkId(0, 0), new ChunkId(1, 0) },
            refresh.Value.RebuiltChunks);
        Assert.DoesNotContain(new ChunkId(2, 0), refresh.Value.RebuiltChunks);
    }

    [Fact]
    public void Closing_passage_splits_regions_and_invalidates_old_path()
    {
        WorldState world = NavigationTestFactory.CreateGroundedCorridor(
            width: 12,
            height: 4,
            chunkSize: 4,
            corridorY: 1);
        NavigationMap map = NavigationTestFactory.BuildMap(
            world,
            TraversalProfile.CreateGroundedDwarf());
        NavigationSnapshot before = NavigationTestFactory.GetSnapshot(map);
        NavigationPathfinder pathfinder = new NavigationPathfinder();
        PathResult pathResult = pathfinder.FindPath(
            before,
            new PathRequest(
                new CellId(0, 1),
                new CellId(11, 1),
                before.NavigationVersion));
        Assert.True(pathResult.Succeeded);

        Result<WorldMutationResult> mutation = world.ApplyTerrainChanges(
            new[]
            {
                new TerrainChange(
                    new CellId(6, 1),
                    NavigationTestFactory.CreateState(NavigationTestFactory.Stone)),
            },
            tick: 2);
        Assert.True(mutation.IsSuccess);
        Assert.True(map.Refresh(
            world.CreateSnapshot(),
            mutation.Value.InvalidatedChunks,
            Array.Empty<TraversalLink>()).IsSuccess);

        NavigationSnapshot after = NavigationTestFactory.GetSnapshot(map);
        Assert.NotEqual(
            GetRegion(after, new CellId(0, 1)),
            GetRegion(after, new CellId(11, 1)));
        Assert.False(pathResult.Path!.IsValidFor(after));
    }

    [Fact]
    public void Free_and_grounded_profiles_produce_different_walkability()
    {
        WorldState world = NavigationTestFactory.CreateStoneWorld(
            width: 4,
            height: 5,
            chunkSize: 2);
        Result<WorldMutationResult> mutation = world.ApplyTerrainChanges(
            new[]
            {
                AirChange(1, 1),
                AirChange(1, 2),
                AirChange(1, 3),
            },
            tick: 1);
        Assert.True(mutation.IsSuccess);

        NavigationSnapshot grounded = NavigationTestFactory.GetSnapshot(
            NavigationTestFactory.BuildMap(
                world,
                TraversalProfile.CreateGroundedDwarf()));
        NavigationSnapshot free = NavigationTestFactory.GetSnapshot(
            NavigationTestFactory.BuildMap(
                world,
                TraversalProfile.CreateFreeMover()));

        Assert.True(grounded.IsWalkable(new CellId(1, 1)));
        Assert.False(grounded.IsWalkable(new CellId(1, 2)));
        Assert.True(free.IsWalkable(new CellId(1, 2)));
        Assert.True(free.IsWalkable(new CellId(1, 3)));
    }

    [Fact]
    public void Ladder_link_makes_unsupported_endpoints_traversable()
    {
        WorldState world = NavigationTestFactory.CreateStoneWorld(
            width: 4,
            height: 5,
            chunkSize: 2);
        Assert.True(world.ApplyTerrainChanges(
            new[]
            {
                AirChange(1, 1),
                AirChange(1, 2),
                AirChange(1, 3),
            },
            tick: 1).IsSuccess);
        TraversalLink ladder = new TraversalLink(
            "ladder-a",
            new CellId(1, 1),
            new CellId(1, 3),
            TraversalLinkKind.Ladder,
            cost: 20,
            bidirectional: true,
            sourceVersion: 1);
        NavigationSnapshot snapshot = NavigationTestFactory.GetSnapshot(
            NavigationTestFactory.BuildMap(
                world,
                TraversalProfile.CreateGroundedDwarf(),
                ladder));

        Assert.True(snapshot.IsWalkable(new CellId(1, 3)));
        PathResult result = new NavigationPathfinder().FindPath(
            snapshot,
            new PathRequest(
                new CellId(1, 1),
                new CellId(1, 3),
                snapshot.NavigationVersion));
        Assert.True(result.Succeeded);
        Assert.Equal(20, result.Path!.TotalCost);
        Assert.Equal(
            new[] { new CellId(1, 1), new CellId(1, 3) },
            result.Path.Cells);
    }

    [Fact]
    public void Snapshot_remains_immutable_after_map_refresh()
    {
        WorldState world = NavigationTestFactory.CreateGroundedCorridor(
            width: 8,
            height: 4,
            chunkSize: 4,
            corridorY: 1,
            blockedX: 3);
        NavigationMap map = NavigationTestFactory.BuildMap(
            world,
            TraversalProfile.CreateGroundedDwarf());
        NavigationSnapshot before = NavigationTestFactory.GetSnapshot(map);
        Assert.False(before.IsWalkable(new CellId(3, 1)));

        Result<WorldMutationResult> mutation = world.Excavate(
            new CellId(3, 1),
            NavigationTestFactory.Air,
            tick: 2);
        Assert.True(mutation.IsSuccess);
        Assert.True(map.Refresh(
            world.CreateSnapshot(),
            mutation.Value.InvalidatedChunks,
            Array.Empty<TraversalLink>()).IsSuccess);

        Assert.False(before.IsWalkable(new CellId(3, 1)));
        Assert.True(NavigationTestFactory.GetSnapshot(map).IsWalkable(
            new CellId(3, 1)));
    }

    private static int GetRegion(NavigationSnapshot snapshot, CellId cellId)
    {
        Assert.True(snapshot.TryGetRegion(cellId, out int regionId));
        return regionId;
    }

    private static TerrainChange AirChange(int x, int y)
    {
        return new TerrainChange(
            new CellId(x, y),
            NavigationTestFactory.CreateState(NavigationTestFactory.Air));
    }
}
}
