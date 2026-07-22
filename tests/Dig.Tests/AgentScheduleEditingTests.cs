using System.Linq;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class AgentScheduleEditingTests
{
    [Fact]
    public void Work_rest_schedule_marks_every_other_hour_as_rest()
    {
        DailySchedule schedule = DailySchedule.CreateWorkRest(
            ticksPerDay: 24,
            workStartTickInclusive: 8,
            workEndTickExclusive: 18);

        Assert.Equal(ScheduleActivity.Rest, schedule.GetActivity(0));
        Assert.Equal(ScheduleActivity.Rest, schedule.GetActivity(7));
        Assert.Equal(ScheduleActivity.Work, schedule.GetActivity(8));
        Assert.Equal(ScheduleActivity.Work, schedule.GetActivity(17));
        Assert.Equal(ScheduleActivity.Rest, schedule.GetActivity(18));
        Assert.True(schedule.TryGetWorkWindow(out int start, out int end));
        Assert.Equal(8, start);
        Assert.Equal(18, end);
    }

    [Fact]
    public void Work_window_can_cross_midnight()
    {
        DailySchedule schedule = DailySchedule.CreateWorkRest(
            ticksPerDay: 24,
            workStartTickInclusive: 20,
            workEndTickExclusive: 4);

        Assert.Equal(ScheduleActivity.Work, schedule.GetActivity(0));
        Assert.Equal(ScheduleActivity.Rest, schedule.GetActivity(10));
        Assert.Equal(ScheduleActivity.Work, schedule.GetActivity(21));
        Assert.True(schedule.TryGetWorkWindow(out int start, out int end));
        Assert.Equal(20, start);
        Assert.Equal(4, end);
    }

    [Fact]
    public void Agent_schedule_update_changes_version_and_emits_event()
    {
        AgentState agent = AgentTestFactory.CreateAgent(
            schedule: DailySchedule.CreateBalanced(24));
        long version = agent.Version;

        Result result = agent.SetWorkRestWindow(9, 17, tick: 3);

        Assert.True(result.IsSuccess);
        Assert.Equal(version + 1, agent.Version);
        Assert.Equal(ScheduleActivity.Rest, agent.Schedule.GetActivity(8));
        Assert.Equal(ScheduleActivity.Work, agent.Schedule.GetActivity(9));
        Assert.Contains(
            agent.PeekUncommittedEvents(),
            value => value is AgentScheduleChanged changed
                && changed.Tick == 3
                && changed.WorkStartTickInclusive == 9
                && changed.WorkEndTickExclusive == 17);
    }

    [Fact]
    public void Automatic_planning_is_enabled_by_default_and_can_be_disabled()
    {
        AgentState agent = AgentTestFactory.CreateAgent();
        long version = agent.Version;

        Result result = agent.SetAutomaticPlanningEnabled(enabled: false, tick: 3);

        Assert.True(result.IsSuccess);
        Assert.False(agent.AutomaticPlanningEnabled);
        Assert.False(agent.CreateSnapshot(3).AutomaticPlanningEnabled);
        Assert.Equal(version + 1, agent.Version);
        AgentAutomaticPlanningChanged changed = Assert.IsType<
            AgentAutomaticPlanningChanged>(Assert.Single(agent.PeekUncommittedEvents()));
        Assert.False(changed.Enabled);
        Assert.Equal(3, changed.Tick);
    }

    [Fact]
    public void Automatic_planning_command_persists_preference_without_touching_schedule()
    {
        AgentState agent = AgentTestFactory.CreateAgent(
            schedule: DailySchedule.CreateWorkRest(24, 8, 18));
        InMemoryAgentRepository repository = new InMemoryAgentRepository();
        Assert.True(repository.Add(agent).IsSuccess);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        SetAgentAutomaticPlanningCommandHandler handler =
            new SetAgentAutomaticPlanningCommandHandler(repository, journal);

        Result result = handler.Handle(new SetAgentAutomaticPlanningCommand(
            agent.Id,
            enabled: false,
            tick: 4));

        Assert.True(result.IsSuccess);
        AgentState saved = repository.Get(agent.Id)!;
        Assert.False(saved.AutomaticPlanningEnabled);
        Assert.Equal(ScheduleActivity.Work, saved.Schedule.GetActivity(9));
        Assert.Contains(journal.Events, value =>
            value is AgentAutomaticPlanningChanged changed && !changed.Enabled);
    }

    [Fact]
    public void Command_handler_persists_the_selected_work_window()
    {
        AgentState agent = AgentTestFactory.CreateAgent(
            schedule: DailySchedule.CreateBalanced(24));
        InMemoryAgentRepository repository = new InMemoryAgentRepository();
        Assert.True(repository.Add(agent).IsSuccess);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        SetAgentWorkRestWindowCommandHandler handler =
            new SetAgentWorkRestWindowCommandHandler(repository, journal);

        Result result = handler.Handle(new SetAgentWorkRestWindowCommand(
            agent.Id,
            workStartTickInclusive: 7,
            workEndTickExclusive: 15,
            tick: 4));

        Assert.True(result.IsSuccess);
        AgentState saved = repository.Get(agent.Id)!;
        Assert.True(saved.Schedule.TryGetWorkWindow(out int start, out int end));
        Assert.Equal(7, start);
        Assert.Equal(15, end);
        Assert.Contains(journal.Events, value => value is AgentScheduleChanged);
    }

    [Fact]
    public void Work_window_cannot_cover_the_entire_day()
    {
        AgentState agent = AgentTestFactory.CreateAgent(
            schedule: DailySchedule.CreateBalanced(24));

        Result result = agent.SetWorkRestWindow(6, 6, tick: 2);

        Assert.True(result.IsFailure);
        Assert.Equal(AgentErrors.InvalidSchedule, result.Error);
        Assert.DoesNotContain(
            agent.PeekUncommittedEvents().OfType<AgentScheduleChanged>(),
            _ => true);
    }
}

}
