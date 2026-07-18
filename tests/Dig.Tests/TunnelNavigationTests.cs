using System.Collections.Generic;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class TunnelNavigationTests
{
    [Fact]
    public void Demo_has_two_four_depth_platforms_four_cells_apart()
    {
        TunnelNavigationVolume volume = TunnelNavigationVolume.CreateDemo(20, 14);
        TunnelDemoLayout layout = Assert.IsType<TunnelDemoLayout>(volume.DemoLayout);

        Assert.Equal(4, volume.Depth);
        Assert.Equal(4, layout.CaveFloorY - layout.SurfaceY);
        Assert.True(layout.CaveHeight >= 3);
        Assert.True(layout.CaveWidth >= 4);
        for (int z = 0; z < volume.Depth; z++)
        {
            Assert.True(volume.IsOpen(new SpatialCellId(
                layout.SurfaceMinX + 1,
                layout.SurfaceY,
                z)));
            Assert.True(volume.IsOpen(new SpatialCellId(
                layout.CaveMinX,
                layout.CaveFloorY,
                z)));
        }
    }

    [Fact]
    public void Demo_shaft_and_connector_use_only_the_nearest_depth_cell()
    {
        TunnelNavigationVolume volume = TunnelNavigationVolume.CreateDemo(20, 14);
        TunnelDemoLayout layout = volume.DemoLayout!;
        int shaftY = layout.SurfaceY + 1;

        Assert.Equal(0, layout.ShaftZ);
        Assert.True(volume.IsVerticalTunnel(new SpatialCellId(
            layout.ShaftX,
            shaftY,
            0)));
        for (int z = 1; z < volume.Depth; z++)
        {
            Assert.False(volume.IsOpen(new SpatialCellId(layout.ShaftX, shaftY, z)));
        }

        if (layout.CaveMinX - layout.ShaftX > 1)
        {
            int corridorX = layout.ShaftX + 1;
            Assert.True(volume.IsOpen(new SpatialCellId(
                corridorX,
                layout.CaveFloorY,
                0)));
            for (int z = 1; z < volume.Depth; z++)
            {
                Assert.False(volume.IsOpen(new SpatialCellId(
                    corridorX,
                    layout.CaveFloorY,
                    z)));
            }
        }
    }

    [Fact]
    public void Surface_route_walks_on_the_xz_plane_without_changing_y()
    {
        TunnelNavigationVolume volume = TunnelNavigationVolume.CreateDemo(20, 14);
        TunnelDemoLayout layout = volume.DemoLayout!;
        SpatialCellId start = new SpatialCellId(
            layout.SurfaceMinX,
            layout.SurfaceY,
            0);
        SpatialCellId goal = new SpatialCellId(
            layout.SurfaceMinX + 3,
            layout.SurfaceY,
            3);

        TunnelPathResult result = volume.FindPath(start, goal);

        Assert.True(result.Succeeded, result.Detail);
        Assert.All(result.Path!.Cells, cell => Assert.Equal(layout.SurfaceY, cell.Y));
        Assert.Contains(result.Path.Cells, cell => cell.Z == 3);
        Assert.Contains(result.Path.Cells, cell => cell.X == goal.X);
    }

    [Fact]
    public void Shaft_route_climbs_on_the_xy_plane_at_nearest_z()
    {
        TunnelNavigationVolume volume = TunnelNavigationVolume.CreateDemo(20, 14);
        TunnelDemoLayout layout = volume.DemoLayout!;
        SpatialCellId start = new SpatialCellId(
            layout.ShaftX,
            layout.SurfaceY,
            layout.ShaftZ);
        SpatialCellId goal = new SpatialCellId(
            layout.ShaftX,
            layout.CaveFloorY,
            layout.ShaftZ);

        TunnelPathResult result = volume.FindPath(start, goal);

        Assert.True(result.Succeeded, result.Detail);
        Assert.All(result.Path!.Cells, cell => Assert.Equal(layout.ShaftX, cell.X));
        Assert.All(result.Path.Cells, cell => Assert.Equal(0, cell.Z));
        Assert.Contains(result.Path.Cells, cell => cell.Y == layout.CaveFloorY);
    }

    [Fact]
    public void Demo_connects_surface_to_every_depth_of_the_lower_cave()
    {
        TunnelNavigationVolume volume = TunnelNavigationVolume.CreateDemo(20, 14);
        TunnelDemoLayout layout = volume.DemoLayout!;
        SpatialCellId start = new SpatialCellId(
            layout.SurfaceMinX,
            layout.SurfaceY,
            3);
        SpatialCellId goal = new SpatialCellId(
            layout.CaveMaxX,
            layout.CaveFloorY,
            3);

        TunnelPathResult result = volume.FindPath(start, goal);

        Assert.True(result.Succeeded, result.Detail);
        Assert.Equal(start, result.Path!.Cells[0]);
        Assert.Equal(goal, result.Path.Cells[result.Path.Cells.Count - 1]);
        Assert.Contains(result.Path.Cells, cell => volume.IsVerticalTunnel(cell));
        Assert.Contains(result.Path.Cells, cell => cell.Z == 0);
        Assert.Contains(result.Path.Cells, cell => cell.Z == 3);
    }

    [Fact]
    public void Vertical_motion_is_rejected_outside_a_vertical_tunnel()
    {
        SpatialCellId lower = new SpatialCellId(1, 1, 0);
        SpatialCellId upper = new SpatialCellId(1, 2, 0);
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 4,
            height: 4,
            depth: 4,
            openCells: new[] { lower, upper },
            verticalCells: new SpatialCellId[0]);

        TunnelPathResult result = volume.FindPath(lower, upper);

        Assert.False(result.Succeeded);
        Assert.Equal(TunnelPathFailureReason.Unreachable, result.FailureReason);
    }

    [Fact]
    public void Vertical_motion_succeeds_when_both_cells_are_in_the_shaft()
    {
        SpatialCellId lower = new SpatialCellId(1, 1, 0);
        SpatialCellId upper = new SpatialCellId(1, 2, 0);
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 4,
            height: 4,
            depth: 4,
            openCells: new[] { lower, upper },
            verticalCells: new[] { lower, upper });

        TunnelPathResult result = volume.FindPath(lower, upper);

        Assert.True(result.Succeeded, result.Detail);
        Assert.Equal(new[] { lower, upper }, result.Path!.Cells);
    }

    [Fact]
    public void Application_moves_the_authoritative_resident_to_the_validated_destination()
    {
        SpatialCellId start = new SpatialCellId(1, 1, 0);
        SpatialCellId middle = new SpatialCellId(2, 1, 0);
        SpatialCellId goal = new SpatialCellId(2, 1, 1);
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 4,
            height: 4,
            depth: 4,
            openCells: new[] { start, middle, goal },
            verticalCells: new SpatialCellId[0]);
        InMemoryAgentRepository repository = new InMemoryAgentRepository();
        AgentState agent = CreateAgent(
            "10000000000000000000000000000099",
            start);
        Assert.True(repository.Add(agent).IsSuccess);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        MoveAgentThroughTunnelCommandHandler handler =
            new MoveAgentThroughTunnelCommandHandler(repository, volume, journal);

        MoveAgentThroughTunnelReport report = handler.Handle(
            new MoveAgentThroughTunnelCommand(agent.Id, goal, tick: 5));

        Assert.True(report.Result.IsSuccess);
        Assert.Equal(new[] { start, middle, goal }, report.Path!.Cells);
        Assert.Equal(goal, repository.Get(agent.Id)!.SpatialPosition);
        AgentMoved moved = Assert.IsType<AgentMoved>(Assert.Single(journal.Events));
        Assert.Equal(start, moved.PreviousSpatialPosition);
        Assert.Equal(goal, moved.CurrentSpatialPosition);
    }

    [Fact]
    public void Group_command_moves_every_resident_to_one_validated_destination()
    {
        SpatialCellId firstStart = new SpatialCellId(0, 1, 0);
        SpatialCellId secondStart = new SpatialCellId(1, 1, 1);
        SpatialCellId goal = new SpatialCellId(3, 1, 1);
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 5,
            height: 3,
            depth: 4,
            openCells: new[]
            {
                firstStart,
                new SpatialCellId(1, 1, 0),
                secondStart,
                new SpatialCellId(2, 1, 1),
                goal,
            },
            verticalCells: new SpatialCellId[0]);
        InMemoryAgentRepository repository = new InMemoryAgentRepository();
        AgentState first = CreateAgent(
            "10000000000000000000000000000101",
            firstStart);
        AgentState second = CreateAgent(
            "10000000000000000000000000000102",
            secondStart);
        Assert.True(repository.Add(first).IsSuccess);
        Assert.True(repository.Add(second).IsSuccess);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        MoveAgentsThroughTunnelCommandHandler handler =
            new MoveAgentsThroughTunnelCommandHandler(repository, volume, journal);

        MoveAgentsThroughTunnelReport report = handler.Handle(
            new MoveAgentsThroughTunnelCommand(
                new[] { first.Id, second.Id },
                goal,
                tick: 7));

        Assert.True(report.Result.IsSuccess);
        Assert.Equal(2, report.Entries.Count);
        Assert.All(report.Entries, entry =>
            Assert.Equal(goal, entry.Path.Cells[entry.Path.Cells.Count - 1]));
        Assert.Equal(goal, repository.Get(first.Id)!.SpatialPosition);
        Assert.Equal(goal, repository.Get(second.Id)!.SpatialPosition);
        Assert.Equal(2, journal.Events.Count);
    }

    [Fact]
    public void Group_command_changes_no_resident_when_one_route_is_unreachable()
    {
        SpatialCellId firstStart = new SpatialCellId(0, 1, 0);
        SpatialCellId goal = new SpatialCellId(1, 1, 0);
        SpatialCellId isolatedStart = new SpatialCellId(4, 1, 0);
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 5,
            height: 3,
            depth: 4,
            openCells: new[] { firstStart, goal, isolatedStart },
            verticalCells: new SpatialCellId[0]);
        InMemoryAgentRepository repository = new InMemoryAgentRepository();
        AgentState first = CreateAgent(
            "10000000000000000000000000000103",
            firstStart);
        AgentState isolated = CreateAgent(
            "10000000000000000000000000000104",
            isolatedStart);
        Assert.True(repository.Add(first).IsSuccess);
        Assert.True(repository.Add(isolated).IsSuccess);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        MoveAgentsThroughTunnelCommandHandler handler =
            new MoveAgentsThroughTunnelCommandHandler(repository, volume, journal);

        MoveAgentsThroughTunnelReport report = handler.Handle(
            new MoveAgentsThroughTunnelCommand(
                new[] { first.Id, isolated.Id },
                goal,
                tick: 8));

        Assert.True(report.Result.IsFailure);
        Assert.Equal(firstStart, repository.Get(first.Id)!.SpatialPosition);
        Assert.Equal(isolatedStart, repository.Get(isolated.Id)!.SpatialPosition);
        Assert.Empty(journal.Events);
    }

    [Fact]
    public void Existing_two_dimensional_move_preserves_the_current_depth_layer()
    {
        AgentState agent = CreateAgent(
            "10000000000000000000000000000105",
            new SpatialCellId(1, 1, 3));

        Assert.True(agent.MoveTo(new CellId(4, 5), tick: 1).IsSuccess);

        Assert.Equal(new SpatialCellId(4, 5, 3), agent.SpatialPosition);
    }

    private static AgentState CreateAgent(string id, SpatialCellId position)
    {
        return new AgentState(
            EntityId.Parse(id),
            "Tunnel Tester",
            new AgentNeedsSnapshot(
                new NeedValue(8_000),
                new NeedValue(8_000),
                new NeedValue(8_000),
                new NeedValue(10_000)),
            DailySchedule.CreateBalanced(24),
            skills: null,
            traits: null,
            initialPosition: position);
    }
}

}
