using Dig.Domain.Agents;
using Dig.Domain.Core;

namespace Dig.Tests
{

internal static class AgentTestFactory
{
    public static readonly EntityId DefaultAgentId = EntityId.Parse(
        "11111111111111111111111111111111");

    public static AgentState CreateAgent(
        int nutrition = 8_000,
        int alertness = 8_000,
        int mood = 8_000,
        int health = 10_000,
        DailySchedule? schedule = null,
        EntityId? id = null)
    {
        return new AgentState(
            id ?? DefaultAgentId,
            "Test Dwarf",
            CreateNeeds(nutrition, alertness, mood, health),
            schedule ?? CreateWorkSchedule(),
            new[]
            {
                new AgentSkillValue(new AgentSkillId("general.work"), 4_000),
            },
            new[]
            {
                new AgentTraitId("steady"),
            });
    }

    public static AgentNeedsSnapshot CreateNeeds(
        int nutrition,
        int alertness,
        int mood,
        int health)
    {
        return new AgentNeedsSnapshot(
            new NeedValue(nutrition),
            new NeedValue(alertness),
            new NeedValue(mood),
            new NeedValue(health));
    }

    public static DailySchedule CreateWorkSchedule(int ticksPerDay = 12)
    {
        return new DailySchedule(
            ticksPerDay,
            new[]
            {
                new ScheduleSegment(0, ticksPerDay, ScheduleActivity.Work),
            });
    }

    public static DailySchedule CreateSleepSchedule(int ticksPerDay = 12)
    {
        return new DailySchedule(
            ticksPerDay,
            new[]
            {
                new ScheduleSegment(0, ticksPerDay, ScheduleActivity.Sleep),
            });
    }

    public static AgentDecision CreateForcedDecision(
        AgentIntentKind intentKind,
        long tick,
        string? playerOrderId = null)
    {
        UtilityOptionDiagnostic option = new UtilityOptionDiagnostic(
            intentKind,
            baseScore: 1,
            finalScore: 1,
            available: true,
            critical: false,
            selected: true,
            "selected.test",
            "Forced test decision.");
        return new AgentDecision(
            tick,
            intentKind,
            playerOrderId,
            selectedScore: 1,
            critical: false,
            "selected.test",
            "Forced test decision.",
            new[] { option });
    }
}
}
