using System;
using System.IO;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentFreeTimeTests
{
    [Fact]
    public void Direct_order_during_free_time_reduces_mood_once()
    {
        AgentState resident = CreateResident(ScheduleActivity.Free);

        Result applied = resident.ApplyFreeTimeDirectOrderPenalty(tick: 1);

        Assert.True(applied.IsSuccess);
        Assert.Equal(
            8_000 - AgentState.FreeTimeDirectOrderMoodPenalty,
            resident.CreateSnapshot(1).Needs.Mood.Points);
    }

    [Fact]
    public void Direct_order_during_work_does_not_reduce_mood()
    {
        AgentState resident = CreateResident(ScheduleActivity.Work);

        Result applied = resident.ApplyFreeTimeDirectOrderPenalty(tick: 1);

        Assert.True(applied.IsSuccess);
        Assert.Equal(8_000, resident.CreateSnapshot(1).Needs.Mood.Points);
    }

    [Fact]
    public void Unity_runtime_pairs_moves_and_rewards_free_time_residents()
    {
        string freeTime = ReadRuntime("DigAgentSession.FreeTime.cs");
        string movement = ReadRuntime("DigTerrainWorkSession.ResidentFreeTime.cs");
        string direct = ReadRuntime("DigTerrainWorkSession.DirectCommands.cs");
        string society = ReadRuntime("DigAgentSession.Society.cs");

        Assert.Contains("free_time_solo_play", freeTime);
        Assert.Contains("free_time_group_play", freeTime);
        Assert.Contains("free_time_social", freeTime);
        Assert.Contains("StartPregnancy", freeTime);
        Assert.Contains("TryPlanResidentFreeTimeMovement", movement);
        Assert.Contains("ApplyFreeTimeDirectOrderPenalty", direct);
        Assert.Contains("gestationTicks: GameTimeCadence.TicksPerDay", society);
    }

    private static AgentState CreateResident(ScheduleActivity activity)
    {
        return new AgentState(
            AgentTestFactory.DefaultAgentId,
            "Free Time Test",
            AgentTestFactory.CreateNeeds(8_000, 8_000, 8_000, 10_000),
            new DailySchedule(
                ticksPerDay: 4,
                new[] { new ScheduleSegment(0, 4, activity) }));
    }

    private static string ReadRuntime(string file)
    {
        return File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime",
            file));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Dig.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}

}
