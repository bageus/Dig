using System;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{
public sealed class WorldOwnedExcavationNavigationTests
{
    private static readonly MaterialId Rock = new MaterialId("rock");
    private static readonly MaterialId Air = new MaterialId("air");

    [Fact]
    public void Tunnel_path_prefers_depth_detour_over_horizontal_shaft_gap()
    {
        CellId start = new CellId(0, 0, 0);
        CellId shaft = new CellId(1, 0, 0);
        CellId goal = new CellId(2, 0, 0);
        CellId[] open =
        {
            start,
            shaft,
            goal,
            new CellId(0, 0, 1),
            new CellId(1, 0, 1),
            new CellId(2, 0, 1),
        };
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 3,
            height: 1,
            depth: 2,
            open,
            verticalCells: new[] { shaft },
            supportedCells: open.Where(cell => cell != shaft).ToArray());

        TunnelPathResult route = volume.FindPath(start, goal);

        Assert.True(route.Succeeded);
        Assert.DoesNotContain(
            TunnelTraversalKind.ShaftGapTraverse,
            route.Path!.TraversalKinds);
        Assert.Contains(TunnelTraversalKind.DepthTraverse, route.Path.TraversalKinds);
        Assert.Contains(new CellId(1, 0, 1), route.Path.Cells);
    }

    [Fact]
    public void Direct_horizontal_shaft_crossing_is_typed_as_climbing_gap()
    {
        CellId start = new CellId(0, 0, 0);
        CellId shaft = new CellId(1, 0, 0);
        CellId goal = new CellId(2, 0, 0);
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 3,
            height: 1,
            depth: 1,
            openCells: new[] { start, shaft, goal },
            verticalCells: new[] { shaft },
            supportedCells: new[] { start, goal });

        TunnelPathResult route = volume.FindPath(start, goal);

        Assert.True(route.Succeeded);
        Assert.All(
            route.Path!.TraversalKinds,
            kind => Assert.Equal(TunnelTraversalKind.ShaftGapTraverse, kind));
    }

    [Fact]
    public void Navigation_pathfinder_prefers_depth_detour_over_excavated_shaft_gap()
    {
        WorldState world = CreateFilledWorld(new WorldSize(3, 2, 2));
        long tick = 1;
        CellId start = new CellId(0, 0, 0);
        CellId crossing = new CellId(1, 0, 0);
        CellId goal = new CellId(2, 0, 0);
        CellId[] openRouteCells =
        {
            start,
            crossing,
            goal,
            new CellId(0, 0, 1),
            new CellId(1, 0, 1),
            new CellId(2, 0, 1),
        };
        for (int index = 0; index < openRouteCells.Length; index++)
        {
            Assert.True(world.Excavate(openRouteCells[index], Air, tick++).IsSuccess);
        }

        CellId shaftBelow = new CellId(1, 1, 0);
        OpenVerticalCell(world, shaftBelow, ref tick);
        NavigationMap map = new NavigationMap(TraversalProfile.CreateFreeMover());
        Assert.True(map.Rebuild(
            world.CreateSnapshot(),
            Array.Empty<TraversalLink>()).IsSuccess);
        NavigationSnapshot snapshot = map.GetSnapshot().Value;
        PathResult result = new NavigationPathfinder().FindPath(
            snapshot,
            new PathRequest(start, goal, snapshot.NavigationVersion));

        Assert.True(result.Succeeded);
        Assert.Contains(new CellId(1, 0, 1), result.Path!.Cells);
        Assert.DoesNotContain(crossing, result.Path.Cells.Skip(1).SkipLast(1));
    }

    [Fact]
    public void Horizontal_entry_above_vertical_endpoint_is_a_shaft_gap()
    {
        CellId start = new CellId(0, 0, 0);
        CellId crossing = new CellId(1, 0, 0);
        CellId goal = new CellId(2, 0, 0);
        CellId shaftBelow = new CellId(1, 1, 0);
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 3,
            height: 2,
            depth: 1,
            openCells: new[] { start, crossing, goal, shaftBelow },
            verticalCells: new[] { shaftBelow },
            supportedCells: new[] { start, goal });

        Assert.True(volume.IsShaftGapCell(crossing));
        Assert.Equal(
            TunnelTraversalKind.ShaftGapTraverse,
            volume.ClassifyTraversal(start, crossing));
        Assert.Equal(
            TunnelTraversalKind.ShaftGapTraverse,
            volume.ClassifyTraversal(crossing, goal));
    }

    [Fact]
    public void Partial_target_below_switches_work_position_to_climbing_posture()
    {
        WorldState world = CreateFilledWorld(new WorldSize(1, 2, 1));
        CellId workCell = new CellId(0, 0, 0);
        CellId target = new CellId(0, 1, 0);
        Assert.True(world.Excavate(workCell, Air, tick: 1).IsSuccess);
        Assert.True(world.SetDigDesignation(target, designated: true, tick: 2).IsSuccess);
        Assert.True(world.CommitExcavationQuarter(
            target,
            ExcavationQuarter.UpperLeft,
            ExcavationCutPattern.HorizontalRows,
            Air,
            tick: 3).IsSuccess);
        NavigationSnapshot navigation = BuildNavigation(world);

        Result<TerrainWorkRoutePlan> result = new TerrainWorkRoutePlanner(
            new NavigationPathfinder()).Plan(
                CreateDigJob(target),
                workCell,
                navigation,
                world.CreateSnapshot());

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Succeeded);
        Assert.Equal(workCell, result.Value.WorkCell);
        Assert.Equal(TerrainWorkPosture.Climbing, result.Value.Posture);
    }

    [Fact]
    public void Reachable_depth_work_cell_is_preferred_over_hanging_above_target()
    {
        WorldState world = CreateFilledWorld(new WorldSize(1, 2, 2));
        CellId current = new CellId(0, 0, 0);
        CellId target = new CellId(0, 1, 0);
        CellId depthBridge = new CellId(0, 0, 1);
        CellId depthWorkCell = new CellId(0, 1, 1);
        Assert.True(world.Excavate(current, Air, tick: 1).IsSuccess);
        Assert.True(world.Excavate(depthBridge, Air, tick: 2).IsSuccess);
        Assert.True(world.Excavate(depthWorkCell, Air, tick: 3).IsSuccess);
        Assert.True(world.SetDigDesignation(target, designated: true, tick: 4).IsSuccess);
        Assert.True(world.CommitExcavationQuarter(
            target,
            ExcavationQuarter.UpperLeft,
            ExcavationCutPattern.HorizontalRows,
            Air,
            tick: 5).IsSuccess);
        NavigationSnapshot navigation = BuildNavigation(world);

        Result<TerrainWorkRoutePlan> result = new TerrainWorkRoutePlanner(
            new NavigationPathfinder()).Plan(
                CreateDigJob(target),
                current,
                navigation,
                world.CreateSnapshot());

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Succeeded);
        Assert.Equal(depthWorkCell, result.Value.WorkCell);
        Assert.Equal(TerrainWorkPosture.DepthBraced, result.Value.Posture);
    }

    private static WorldState CreateFilledWorld(WorldSize size)
    {
        return WorldState.CreateFilled(
            size,
            chunkSize: 1,
            new MaterialCatalog(new[]
            {
                new MaterialDefinition(Rock, isSolid: true, hardness: 100),
                new MaterialDefinition(Air, isSolid: false, hardness: 0),
            }),
            Rock,
            explored: true).Value;
    }

    private static void OpenVerticalCell(
        WorldState world,
        CellId cell,
        ref long tick)
    {
        Assert.True(world.SetDigDesignation(cell, designated: true, tick++).IsSuccess);
        foreach (ExcavationQuarter quarter in new[]
        {
            ExcavationQuarter.UpperLeft,
            ExcavationQuarter.UpperRight,
            ExcavationQuarter.LowerLeft,
            ExcavationQuarter.LowerRight,
        })
        {
            Assert.True(world.CommitExcavationQuarter(
                cell,
                quarter,
                ExcavationCutPattern.HorizontalRows,
                Air,
                tick++).IsSuccess);
        }
    }

    private static NavigationSnapshot BuildNavigation(WorldState world)
    {
        NavigationMap map = new NavigationMap(TraversalProfile.CreateFreeMover());
        Assert.True(map.Rebuild(
            world.CreateSnapshot(),
            Array.Empty<TraversalLink>()).IsSuccess);
        return map.GetSnapshot().Value;
    }

    private static JobSnapshot CreateDigJob(CellId target)
    {
        EntityId jobId = EntityId.Parse("40000000000000000000000000000009");
        JobSystem jobs = new JobSystem();
        Assert.True(jobs.Add(new DigJobDefinition(
            jobId,
            new DigJobTarget(target),
            priority: 750,
            createdTick: 1,
            JobRetryPolicy.Default)).IsSuccess);
        Assert.True(jobs.MakeAvailable(jobId, tick: 1).IsSuccess);
        return jobs.Get(jobId)!;
    }
}
}
