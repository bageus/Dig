using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.Runtime;
using Xunit;

namespace Dig.Tests
{

public sealed class AgentPositionPresentationTests
{
    [Fact]
    public void Move_command_updates_position_and_publishes_one_event()
    {
        AgentState agent = CreateAgent(
            "00000000000000000000000000000001",
            "Borin",
            new CellId(2, 3));
        InMemoryAgentRepository repository = new InMemoryAgentRepository();
        Assert.True(repository.Add(agent).IsSuccess);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        MoveAgentCommandHandler handler = new MoveAgentCommandHandler(repository, journal);

        Result result = handler.Handle(new MoveAgentCommand(
            agent.Id,
            new CellId(5, 4),
            tick: 7));

        Assert.True(result.IsSuccess);
        Assert.Equal(new CellId(5, 4), agent.CreateSnapshot(7).Position);
        AgentMoved moved = Assert.IsType<AgentMoved>(Assert.Single(journal.Events));
        Assert.Equal(new CellId(2, 3), moved.PreviousPosition);
        Assert.Equal(new CellId(5, 4), moved.CurrentPosition);
        Assert.Equal(7, moved.Tick);
    }

    [Fact]
    public void Invalid_move_preserves_position_and_emits_nothing()
    {
        AgentState agent = CreateAgent(
            "00000000000000000000000000000002",
            "Dora",
            new CellId(3, 2));
        InMemoryAgentRepository repository = new InMemoryAgentRepository();
        Assert.True(repository.Add(agent).IsSuccess);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        MoveAgentCommandHandler handler = new MoveAgentCommandHandler(repository, journal);

        Result result = handler.Handle(new MoveAgentCommand(
            agent.Id,
            new CellId(-1, 4),
            tick: 3));

        Assert.True(result.IsFailure);
        Assert.Equal(AgentErrors.InvalidPosition, result.Error);
        Assert.Equal(new CellId(3, 2), agent.CreateSnapshot(3).Position);
        Assert.Empty(journal.Events);
    }

    [Fact]
    public void Presenter_exposes_stable_resident_order_and_utility_diagnostics()
    {
        AgentState later = CreateAgent(
            "00000000000000000000000000000020",
            "Fara",
            new CellId(7, 3));
        AgentState earlier = CreateAgent(
            "00000000000000000000000000000010",
            "Einar",
            new CellId(4, 6));
        InMemoryAgentRepository repository = new InMemoryAgentRepository();
        Assert.True(repository.Add(later).IsSuccess);
        Assert.True(repository.Add(earlier).IsSuccess);
        ApplyDecision(earlier, tick: 1);
        ApplyDecision(later, tick: 1);

        AgentPresenter presenter = new AgentPresenter(
            new GetAgentSnapshotsQueryHandler(repository));
        IReadOnlyList<AgentViewModel> residents = presenter.Load(tick: 1);

        Assert.Equal(new[] { "Einar", "Fara" }, residents.Select(item => item.Name));
        AgentViewModel first = residents[0];
        Assert.Equal(4, first.CellX);
        Assert.Equal(6, first.CellY);
        Assert.Equal(7, first.UtilityOptions.Count);
        Assert.Single(first.UtilityOptions, option => option.Selected);
        Assert.NotEqual("agents.decision.pending", first.DecisionReason);
        Assert.Contains(first.ActiveIntent, first.DecisionExplanation);
    }

    [Fact]
    public void Presenter_exposes_automatic_planning_eligibility()
    {
        AgentState agent = CreateAgent(
            "00000000000000000000000000000030",
            "Kara",
            new CellId(2, 2));
        Assert.True(agent.SetAutomaticPlanningEnabled(false, tick: 1).IsSuccess);
        agent.DequeueUncommittedEvents();
        InMemoryAgentRepository repository = new InMemoryAgentRepository();
        Assert.True(repository.Add(agent).IsSuccess);

        AgentViewModel resident = Assert.Single(new AgentPresenter(
            new GetAgentSnapshotsQueryHandler(repository)).Load(tick: 1));

        Assert.False(resident.AutomaticPlanningEnabled);
        Assert.False(resident.IsAvailableForAutomaticPlanning);
    }

    [Theory]
    [InlineData(0d, 2d, 4d)]
    [InlineData(0.5d, 5d, 8d)]
    [InlineData(1d, 8d, 12d)]
    [InlineData(2d, 8d, 12d)]
    public void Interpolator_clamps_progress_and_preserves_endpoints(
        double progress,
        double expectedX,
        double expectedY)
    {
        AgentInterpolatedPosition position = AgentPositionInterpolator.Interpolate(
            2,
            4,
            8,
            12,
            progress);

        Assert.Equal(expectedX, position.X, precision: 6);
        Assert.Equal(expectedY, position.Y, precision: 6);
    }

    [Fact]
    public void Playback_pause_blocks_ticks_and_step_consumes_exactly_one()
    {
        SimulationPlaybackState playback = new SimulationPlaybackState();

        playback.TogglePause();

        Assert.True(playback.IsPaused);
        Assert.Equal(0, playback.ConsumeDueTicks(10d, 0.8d));

        playback.StepOnce();

        Assert.Equal(1, playback.ConsumeDueTicks(0d, 0.8d));
        Assert.Equal(0, playback.ConsumeDueTicks(10d, 0.8d));
        Assert.True(playback.IsPaused);
    }

    [Theory]
    [InlineData(SimulationPlaybackSpeed.Normal, 1)]
    [InlineData(SimulationPlaybackSpeed.Fast, 2)]
    [InlineData(SimulationPlaybackSpeed.VeryFast, 4)]
    public void Playback_speed_controls_due_tick_count(
        SimulationPlaybackSpeed speed,
        int expectedTicks)
    {
        SimulationPlaybackState playback = new SimulationPlaybackState();
        playback.SetSpeed(speed);

        int due = playback.ConsumeDueTicks(0.8d, 0.8d);

        Assert.Equal(expectedTicks, due);
    }

    [Fact]
    public void Playback_speed_changes_are_bounded_and_reversible()
    {
        SimulationPlaybackState playback = new SimulationPlaybackState();

        playback.SpeedUp();
        Assert.Equal(SimulationPlaybackSpeed.Fast, playback.Speed);
        playback.SpeedUp();
        Assert.Equal(SimulationPlaybackSpeed.VeryFast, playback.Speed);
        playback.SpeedUp();
        Assert.Equal(SimulationPlaybackSpeed.VeryFast, playback.Speed);

        playback.SlowDown();
        Assert.Equal(SimulationPlaybackSpeed.Fast, playback.Speed);
        playback.SlowDown();
        Assert.Equal(SimulationPlaybackSpeed.Normal, playback.Speed);
        playback.SlowDown();
        Assert.True(playback.IsPaused);
        playback.SpeedUp();
        Assert.False(playback.IsPaused);
        Assert.Equal(SimulationPlaybackSpeed.Normal, playback.Speed);
    }

    [Fact]
    public void Playback_catch_up_is_bounded_per_frame()
    {
        SimulationPlaybackState playback = new SimulationPlaybackState();
        playback.SetSpeed(SimulationPlaybackSpeed.VeryFast);

        int due = playback.ConsumeDueTicks(100d, 0.8d, maximumTicksPerFrame: 3);

        Assert.Equal(3, due);
        Assert.Equal(0, playback.ConsumeDueTicks(0d, 0.8d, maximumTicksPerFrame: 3));
    }

    [Fact]
    public void Playback_rejects_invalid_arguments()
    {
        SimulationPlaybackState playback = new SimulationPlaybackState();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            playback.SetSpeed((SimulationPlaybackSpeed)3));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            playback.ConsumeDueTicks(-1d, 0.8d));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            playback.ConsumeDueTicks(0d, 0d));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            playback.ConsumeDueTicks(0d, 0.8d, 0));
    }

    private static AgentState CreateAgent(string id, string name, CellId position)
    {
        return new AgentState(
            EntityId.Parse(id),
            name,
            new AgentNeedsSnapshot(
                new NeedValue(7_000),
                new NeedValue(6_000),
                new NeedValue(8_000),
                new NeedValue(9_000)),
            DailySchedule.CreateBalanced(24),
            skills: null,
            traits: null,
            initialPosition: position);
    }

    private static void ApplyDecision(AgentState agent, long tick)
    {
        AgentBehaviorPolicy policy = AgentBehaviorPolicy.CreateDefault();
        AgentDecision decision = new AgentDecisionSystem().Decide(
            agent.CreateSnapshot(tick),
            AgentDecisionContext.AllAvailable(),
            policy,
            tick);
        Assert.True(agent.ApplyDecision(decision, policy, tick).IsSuccess);
    }
}
}
