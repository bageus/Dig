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
    public void Demo_uses_four_depth_cells_and_multiple_connected_levels()
    {
        TunnelNavigationVolume volume = TunnelNavigationVolume.CreateDemo(20, 14);
        SpatialCellId start = new SpatialCellId(3, 2, 0);
        SpatialCellId goal = new SpatialCellId(16, 11, 3);

        TunnelPathResult result = volume.FindPath(start, goal);

        Assert.Equal(4, volume.Depth);
        Assert.True(result.Succeeded, result.Detail);
        Assert.Equal(start, result.Path!.Cells[0]);
        Assert.Equal(goal, result.Path.Cells[result.Path.Cells.Count - 1]);
        Assert.Contains(result.Path.Cells, cell => cell.Z == 3);
        Assert.Contains(result.Path.Cells, cell => volume.IsVerticalTunnel(cell));
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
        SpatialCellId lower = new SpatialCellId(1, 1, 1);
        SpatialCellId upper = new SpatialCellId(1, 2, 1);
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
        AgentState agent = CreateAgent(start);
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
    public void Existing_two_dimensional_move_preserves_the_current_depth_layer()
    {
        AgentState agent = CreateAgent(new SpatialCellId(1, 1, 3));

        Assert.True(agent.MoveTo(new CellId(4, 5), tick: 1).IsSuccess);

        Assert.Equal(new SpatialCellId(4, 5, 3), agent.SpatialPosition);
    }

    private static AgentState CreateAgent(SpatialCellId position)
    {
        return new AgentState(
            EntityId.Parse("10000000000000000000000000000099"),
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
