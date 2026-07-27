using System;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Xunit;

namespace Dig.Tests
{
public sealed class MushroomResidentPresentationTests
{
    [Fact]
    public void Assigned_mushroom_job_reports_gather_status_and_target()
    {
        EntityId agentId = Id(71);
        EntityId jobId = Id(72);
        EntityId siteId = Id(73);
        CellId target = new CellId(6, 5, 1);
        CellId work = new CellId(5, 5, 1);
        AgentSnapshot agent = Agent(agentId);
        JobSnapshot job = new JobSnapshot(
            new MushroomChopJobDefinition(
                jobId,
                siteId,
                target,
                work,
                growthGeneration: 0,
                requiredSwings: 4,
                priority: 900,
                createdTick: 1,
                JobRetryPolicy.Default),
            JobStatus.InProgress,
            JobStageKind.PerformWork,
            agentId,
            retryCount: 0,
            nextRetryTick: 0,
            version: 2,
            reason: null);

        ResidentActivityDescriptor activity = Assert.Single(
            new ResidentRosterPresenter().Present(
                new[] { agent },
                society: null,
                jobs: new[] { job },
                selectedResidentId: agentId).Rows).Activity;

        Assert.Equal(ResidentActivityKind.GatherMushroom, activity.Kind);
        Assert.Equal("Добывает гриб", activity.LocalizationKey);
        Assert.Equal(siteId.ToString(), activity.SubjectId);
        Assert.Equal(target, activity.Destination);
        Assert.Equal(jobId.ToString(), activity.SourceJobId);
    }

    private static AgentSnapshot Agent(EntityId id)
    {
        return new AgentSnapshot(
            id,
            "Gatherer",
            version: 1,
            isAlive: true,
            needs: new AgentNeedsSnapshot(
                new NeedValue(6000),
                new NeedValue(6000),
                new NeedValue(6000),
                new NeedValue(6000)),
            scheduledActivity: ScheduleActivity.Work,
            activeAction: null,
            playerOrder: null,
            lastActionSwitchTick: -1,
            lastDecision: null,
            skills: Array.Empty<AgentSkillValue>(),
            traits: Array.Empty<AgentTraitId>(),
            position: new CellId(1, 1));
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}
