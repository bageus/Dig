using System;
using Dig.Presentation.Agents;
using Xunit;

namespace Dig.Tests
{

public sealed class AgentAutomaticPlanningPresentationTests
{
    [Theory]
    [InlineData("Work", true, true)]
    [InlineData("Work", false, false)]
    [InlineData("Rest", true, false)]
    [InlineData("Sleep", true, false)]
    [InlineData("Free", true, false)]
    public void Automatic_candidate_projection_requires_work_schedule_and_auto_on(
        string scheduledActivity,
        bool automaticPlanningEnabled,
        bool expected)
    {
        AgentViewModel resident = new AgentViewModel(
            "00000000000000000000000000000031",
            "Lina",
            version: 0,
            isAlive: true,
            cellX: 3,
            cellY: 2,
            nutrition: 7_000,
            alertness: 6_000,
            mood: 8_000,
            health: 9_000,
            scheduledActivity,
            activeIntent: "Idle",
            actionElapsedTicks: 0,
            actionRequiredTicks: 0,
            decisionReason: "test",
            decisionExplanation: "test",
            utilityOptions: Array.Empty<AgentUtilityOptionViewModel>(),
            automaticPlanningEnabled: automaticPlanningEnabled);

        Assert.Equal(
            string.Equals(scheduledActivity, "Work", StringComparison.Ordinal),
            resident.IsScheduledForWork);
        Assert.Equal(expected, resident.IsAvailableForAutomaticPlanning);
    }
}

}