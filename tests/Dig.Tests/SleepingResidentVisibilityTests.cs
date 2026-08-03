using System;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Xunit;

namespace Dig.Tests
{

public sealed class SleepingResidentVisibilityTests
{
    [Fact]
    public void Living_sleeper_remains_in_roster_and_selected()
    {
        EntityId residentId = EntityId.Parse("be000000000000000000000000000001");
        AgentSnapshot sleeper = new AgentSnapshot(
            id: residentId,
            name: "Sleeper",
            version: 3,
            isAlive: true,
            needs: new AgentNeedsSnapshot(
                new NeedValue(8_000),
                new NeedValue(2_000),
                new NeedValue(6_000),
                new NeedValue(7_000)),
            scheduledActivity: ScheduleActivity.Sleep,
            activeAction: new AgentActionSnapshot(
                intentKind: AgentIntentKind.Sleep,
                playerOrderId: null,
                startedTick: 4,
                requiredTicks: 3,
                elapsedTicks: 1,
                target: new AgentActivityTarget(
                    AgentActivityTargetKind.FloorSleep,
                    residentId)),
            playerOrder: null,
            lastActionSwitchTick: 4,
            lastDecision: null,
            skills: Array.Empty<AgentSkillValue>(),
            traits: Array.Empty<AgentTraitId>(),
            position: new CellId(2, 3));

        ResidentRosterViewModel roster = new ResidentRosterPresenter().Present(
            new[] { new ResidentRosterSource(sleeper) },
            selectedResidentId: residentId);
        ResidentRosterRowViewModel row = Assert.Single(roster.Rows);

        Assert.True(row.IsAlive);
        Assert.True(row.IsExpanded);
        Assert.Equal(residentId.ToString(), row.Id);
        Assert.Equal(ResidentActivityKind.Sleep, row.Activity.Kind);
        Assert.Equal(AgentIntentKind.Sleep, row.Activity.SourceIntent);
    }
}

}
