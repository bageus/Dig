using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class TunnelMovementPlanningTests
{
    [Fact]
    public void Application_plans_route_without_teleporting_the_resident()
    {
        CellId start = new CellId(1, 1, 0);
        CellId middle = new CellId(2, 1, 0);
        CellId goal = new CellId(2, 1, 1);
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 4,
            height: 4,
            depth: 4,
            openCells: new[] { start, middle, goal },
            verticalCells: new CellId[0]);
        InMemoryAgentRepository repository = new InMemoryAgentRepository();
        AgentState agent = CreateAgent(
            "10000000000000000000000000000099",
            start);
        Assert.True(repository.Add(agent).IsSuccess);
        PlanAgentTunnelRouteCommandHandler handler =
            new PlanAgentTunnelRouteCommandHandler(repository, volume);

        PlanAgentTunnelRouteReport report = handler.Handle(
            new PlanAgentTunnelRouteCommand(agent.Id, goal));

        Assert.True(report.Result.IsSuccess);
        Assert.Equal(new[] { start, middle, goal }, report.Path!.Cells);
        Assert.Equal(start, repository.Get(agent.Id)!.Position);
        Assert.Empty(repository.Get(agent.Id)!.DequeueUncommittedEvents());
    }

    [Fact]
    public void Authoritative_execution_moves_one_route_cell_at_a_time()
    {
        CellId start = new CellId(0, 1, 0);
        CellId second = new CellId(1, 1, 0);
        CellId third = new CellId(1, 1, 1);
        CellId goal = new CellId(2, 1, 1);
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 4,
            height: 3,
            depth: 4,
            openCells: new[] { start, second, third, goal },
            verticalCells: new CellId[0]);
        AgentState agent = CreateAgent(
            "10000000000000000000000000000100",
            start);
        TunnelPath path = Assert.IsType<TunnelPath>(volume.FindPath(start, goal).Path);

        for (int index = 1; index < path.Cells.Count; index++)
        {
            CellId before = agent.Position;
            CellId next = path.Cells[index];
            Assert.True(volume.CanTraverseStep(before, next));
            Assert.True(agent.MoveTo(next, tick: index).IsSuccess);
            AgentMoved moved = Assert.IsType<AgentMoved>(
                Assert.Single(agent.DequeueUncommittedEvents()));
            Assert.Equal(before, moved.PreviousPosition);
            Assert.Equal(next, moved.CurrentPosition);
        }

        Assert.Equal(goal, agent.Position);
    }

    [Fact]
    public void Group_planner_returns_atomic_routes_without_moving_any_resident()
    {
        CellId firstStart = new CellId(0, 1, 0);
        CellId secondStart = new CellId(1, 1, 1);
        CellId goal = new CellId(3, 1, 1);
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 5,
            height: 3,
            depth: 4,
            openCells: new[]
            {
                firstStart,
                new CellId(1, 1, 0),
                secondStart,
                new CellId(2, 1, 1),
                goal,
            },
            verticalCells: new CellId[0]);
        InMemoryAgentRepository repository = new InMemoryAgentRepository();
        AgentState first = CreateAgent(
            "10000000000000000000000000000101",
            firstStart);
        AgentState second = CreateAgent(
            "10000000000000000000000000000102",
            secondStart);
        Assert.True(repository.Add(first).IsSuccess);
        Assert.True(repository.Add(second).IsSuccess);
        PlanAgentsTunnelRoutesCommandHandler handler =
            new PlanAgentsTunnelRoutesCommandHandler(repository, volume);

        PlanAgentsTunnelRoutesReport report = handler.Handle(
            new PlanAgentsTunnelRoutesCommand(
                new[] { first.Id, second.Id },
                goal));

        Assert.True(report.Result.IsSuccess);
        Assert.Equal(2, report.Entries.Count);
        Assert.All(report.Entries, entry =>
        {
            Assert.Equal(goal, entry.Path.Cells[entry.Path.Cells.Count - 1]);
            AssertRouteUsesOnlyAuthoritativeSteps(volume, entry.Path);
        });
        Assert.Equal(firstStart, repository.Get(first.Id)!.Position);
        Assert.Equal(secondStart, repository.Get(second.Id)!.Position);
    }

    [Fact]
    public void Group_planner_returns_no_routes_when_one_resident_is_unreachable()
    {
        CellId firstStart = new CellId(0, 1, 0);
        CellId goal = new CellId(1, 1, 0);
        CellId isolatedStart = new CellId(4, 1, 0);
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 5,
            height: 3,
            depth: 4,
            openCells: new[] { firstStart, goal, isolatedStart },
            verticalCells: new CellId[0]);
        InMemoryAgentRepository repository = new InMemoryAgentRepository();
        AgentState first = CreateAgent(
            "10000000000000000000000000000103",
            firstStart);
        AgentState isolated = CreateAgent(
            "10000000000000000000000000000104",
            isolatedStart);
        Assert.True(repository.Add(first).IsSuccess);
        Assert.True(repository.Add(isolated).IsSuccess);
        PlanAgentsTunnelRoutesCommandHandler handler =
            new PlanAgentsTunnelRoutesCommandHandler(repository, volume);

        PlanAgentsTunnelRoutesReport report = handler.Handle(
            new PlanAgentsTunnelRoutesCommand(
                new[] { first.Id, isolated.Id },
                goal));

        Assert.True(report.Result.IsFailure);
        Assert.Empty(report.Entries);
        Assert.Equal(firstStart, repository.Get(first.Id)!.Position);
        Assert.Equal(isolatedStart, repository.Get(isolated.Id)!.Position);
    }

    [Fact]
    public void Exact_cell_move_uses_the_requested_depth_layer()
    {
        AgentState agent = CreateAgent(
            "10000000000000000000000000000105",
            new CellId(1, 1, 3));

        Assert.True(agent.MoveTo(new CellId(4, 5), tick: 1).IsSuccess);

        Assert.Equal(new CellId(4, 5, 0), agent.Position);
    }

    private static void AssertRouteUsesOnlyAuthoritativeSteps(
        TunnelNavigationVolume volume,
        TunnelPath path)
    {
        for (int index = 1; index < path.Cells.Count; index++)
        {
            Assert.True(
                volume.CanTraverseStep(path.Cells[index - 1], path.Cells[index]),
                $"Invalid route step {path.Cells[index - 1]} -> {path.Cells[index]}.");
        }
    }

    private static AgentState CreateAgent(string id, CellId position)
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
