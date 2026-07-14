using System;
using System.IO;
using Dig.Application.Navigation;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class NavigationPathfinderTests
{
    [Fact]
    public void Unrelated_chunk_change_does_not_invalidate_existing_path()
    {
        WorldState world = NavigationTestFactory.CreateGroundedCorridor(
            width: 12,
            height: 8,
            chunkSize: 4,
            corridorY: 1);
        NavigationMap map = NavigationTestFactory.BuildMap(
            world,
            TraversalProfile.CreateGroundedDwarf());
        NavigationSnapshot before = NavigationTestFactory.GetSnapshot(map);
        PathResult result = new NavigationPathfinder().FindPath(
            before,
            new PathRequest(
                new CellId(0, 1),
                new CellId(11, 1),
                before.NavigationVersion));
        Assert.True(result.Succeeded);

        Result<WorldMutationResult> mutation = world.Excavate(
            new CellId(10, 6),
            NavigationTestFactory.Air,
            tick: 2);
        Assert.True(mutation.IsSuccess);
        Assert.True(map.Refresh(
            world.CreateSnapshot(),
            mutation.Value.InvalidatedChunks,
            Array.Empty<TraversalLink>()).IsSuccess);

        NavigationSnapshot after = NavigationTestFactory.GetSnapshot(map);
        Assert.True(result.Path!.IsValidFor(after));
        Assert.NotEqual(before.NavigationVersion, after.NavigationVersion);
    }

    [Fact]
    public void Request_for_old_snapshot_is_rejected_before_search()
    {
        WorldState world = NavigationTestFactory.CreateGroundedCorridor(
            width: 8,
            height: 4,
            chunkSize: 4,
            corridorY: 1);
        NavigationMap map = NavigationTestFactory.BuildMap(
            world,
            TraversalProfile.CreateGroundedDwarf());
        NavigationSnapshot before = NavigationTestFactory.GetSnapshot(map);
        Result<WorldMutationResult> mutation = world.ApplyTerrainChanges(
            new[]
            {
                new TerrainChange(
                    new CellId(3, 1),
                    NavigationTestFactory.CreateState(NavigationTestFactory.Stone)),
            },
            tick: 2);
        Assert.True(mutation.IsSuccess);
        Assert.True(map.Refresh(
            world.CreateSnapshot(),
            mutation.Value.InvalidatedChunks,
            Array.Empty<TraversalLink>()).IsSuccess);
        NavigationSnapshot after = NavigationTestFactory.GetSnapshot(map);

        PathResult result = new NavigationPathfinder().FindPath(
            after,
            new PathRequest(
                new CellId(0, 1),
                new CellId(7, 1),
                before.NavigationVersion));

        Assert.False(result.Succeeded);
        Assert.Equal(PathFailureReason.StaleSnapshot, result.Diagnostics.FailureReason);
        Assert.Equal(0, result.Diagnostics.ExpandedNodes);
    }

    [Fact]
    public void One_way_elevator_allows_only_declared_direction()
    {
        WorldState world = NavigationTestFactory.CreateStoneWorld(
            width: 4,
            height: 5,
            chunkSize: 2);
        Assert.True(world.ApplyTerrainChanges(
            new[]
            {
                AirChange(1, 1),
                AirChange(1, 3),
            },
            tick: 1).IsSuccess);
        TraversalLink elevator = new TraversalLink(
            "elevator-a",
            new CellId(1, 1),
            new CellId(1, 3),
            TraversalLinkKind.Elevator,
            cost: 15,
            bidirectional: false,
            sourceVersion: 1);
        NavigationSnapshot snapshot = NavigationTestFactory.GetSnapshot(
            NavigationTestFactory.BuildMap(
                world,
                TraversalProfile.CreateGroundedDwarf(),
                elevator));
        NavigationPathfinder pathfinder = new NavigationPathfinder();

        PathResult upward = pathfinder.FindPath(
            snapshot,
            new PathRequest(
                new CellId(1, 1),
                new CellId(1, 3),
                snapshot.NavigationVersion));
        PathResult downward = pathfinder.FindPath(
            snapshot,
            new PathRequest(
                new CellId(1, 3),
                new CellId(1, 1),
                snapshot.NavigationVersion));

        Assert.True(upward.Succeeded);
        Assert.False(downward.Succeeded);
        Assert.Equal(PathFailureReason.Unreachable, downward.Diagnostics.FailureReason);
        Assert.True(downward.Diagnostics.ExpandedNodes > 0);
    }

    [Fact]
    public void Search_budget_produces_explicit_failure()
    {
        MaterialCatalog materials = NavigationTestFactory.CreateMaterials();
        Result<WorldState> worldResult = WorldState.CreateFilled(
            new WorldSize(10, 10),
            chunkSize: 5,
            materials,
            NavigationTestFactory.Air,
            explored: true);
        Assert.True(worldResult.IsSuccess);
        NavigationSnapshot snapshot = NavigationTestFactory.GetSnapshot(
            NavigationTestFactory.BuildMap(
                worldResult.Value,
                TraversalProfile.CreateFreeMover()));

        PathResult result = new NavigationPathfinder().FindPath(
            snapshot,
            new PathRequest(
                new CellId(0, 0),
                new CellId(9, 9),
                snapshot.NavigationVersion,
                maxExpandedNodes: 1));

        Assert.False(result.Succeeded);
        Assert.Equal(
            PathFailureReason.SearchLimitExceeded,
            result.Diagnostics.FailureReason);
    }

    [Fact]
    public void Disconnected_regions_fail_without_repeated_search()
    {
        WorldState world = NavigationTestFactory.CreateGroundedCorridor(
            width: 8,
            height: 4,
            chunkSize: 4,
            corridorY: 1,
            blockedX: 4);
        NavigationSnapshot snapshot = NavigationTestFactory.GetSnapshot(
            NavigationTestFactory.BuildMap(
                world,
                TraversalProfile.CreateGroundedDwarf()));

        PathResult result = new NavigationPathfinder().FindPath(
            snapshot,
            new PathRequest(
                new CellId(0, 1),
                new CellId(7, 1),
                snapshot.NavigationVersion));

        Assert.False(result.Succeeded);
        Assert.Equal(PathFailureReason.Unreachable, result.Diagnostics.FailureReason);
        Assert.Equal(0, result.Diagnostics.ExpandedNodes);
        Assert.NotEqual(
            result.Diagnostics.StartRegion,
            result.Diagnostics.GoalRegion);
    }

    [Fact]
    public void Application_handlers_build_refresh_and_query_navigation()
    {
        WorldState world = NavigationTestFactory.CreateGroundedCorridor(
            width: 8,
            height: 4,
            chunkSize: 4,
            corridorY: 1);
        TraversalProfile profile = TraversalProfile.CreateGroundedDwarf();
        InMemoryNavigationRepository repository = new InMemoryNavigationRepository();
        RebuildNavigationCommandHandler rebuild =
            new RebuildNavigationCommandHandler(repository);
        FindPathQueryHandler find = new FindPathQueryHandler(
            repository,
            new NavigationPathfinder());

        Result<NavigationUpdateDiagnostics> buildResult = rebuild.Handle(
            new RebuildNavigationCommand(
                profile,
                world.CreateSnapshot(),
                Array.Empty<TraversalLink>()));
        Assert.True(buildResult.IsSuccess);
        NavigationMap map = repository.Get(profile.Id)!;
        NavigationSnapshot snapshot = NavigationTestFactory.GetSnapshot(map);
        Result<PathResult> path = find.Handle(
            new FindPathQuery(
                profile.Id,
                new PathRequest(
                    new CellId(0, 1),
                    new CellId(7, 1),
                    snapshot.NavigationVersion)));

        Assert.True(path.IsSuccess);
        Assert.True(path.Value.Succeeded);
    }

    private static TerrainChange AirChange(int x, int y)
    {
        return new TerrainChange(
            new CellId(x, y),
            NavigationTestFactory.CreateState(NavigationTestFactory.Air));
    }
}
}
