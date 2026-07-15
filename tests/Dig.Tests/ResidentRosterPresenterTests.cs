using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Society;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentRosterPresenterTests
{
    [Theory]
    [InlineData(0, ResidentNeedBand.Critical)]
    [InlineData(2500, ResidentNeedBand.Critical)]
    [InlineData(2600, ResidentNeedBand.Warning)]
    [InlineData(5000, ResidentNeedBand.Warning)]
    [InlineData(5100, ResidentNeedBand.Healthy)]
    [InlineData(10000, ResidentNeedBand.Healthy)]
    public void Need_thresholds_are_exact(int value, ResidentNeedBand expected)
    {
        Assert.Equal(expected, ResidentRosterPresenter.ClassifyNeed(value));
    }

    [Theory]
    [InlineData(0, ResidentMoodFace.Sad)]
    [InlineData(2500, ResidentMoodFace.Sad)]
    [InlineData(2600, ResidentMoodFace.Neutral)]
    [InlineData(7500, ResidentMoodFace.Neutral)]
    [InlineData(7600, ResidentMoodFace.Joy)]
    [InlineData(10000, ResidentMoodFace.Joy)]
    public void Mood_thresholds_are_exact(int value, ResidentMoodFace expected)
    {
        Assert.Equal(expected, ResidentRosterPresenter.ClassifyMood(value));
    }

    [Fact]
    public void Roster_is_stable_and_only_selected_row_is_expanded()
    {
        EntityId firstId = Id(1);
        EntityId secondId = Id(2);
        AgentSnapshot first = Agent(firstId, "First", ScheduleActivity.Free);
        AgentSnapshot second = Agent(secondId, "Second", ScheduleActivity.Work);
        ResidentRosterPresenter presenter = new ResidentRosterPresenter();

        ResidentRosterViewModel roster = presenter.Present(
            new[]
            {
                new ResidentRosterSource(second, Society(second, ResidentSex.Male)),
                new ResidentRosterSource(first, Society(first, ResidentSex.Female)),
            },
            selectedResidentId: secondId);

        Assert.Equal(new[] { firstId.ToString(), secondId.ToString() },
            roster.Rows.Select(item => item.Id).ToArray());
        Assert.False(roster.Rows[0].IsExpanded);
        Assert.True(roster.Rows[1].IsExpanded);
        Assert.Equal(ResidentSexIndicator.Female, roster.Rows[0].Sex);
        Assert.Equal("resident.sex.female", roster.Rows[0].SexAccessibilityKey);
        Assert.Equal(ResidentSexIndicator.Male, roster.Rows[1].Sex);
        Assert.Equal("resident.sex.male", roster.Rows[1].SexAccessibilityKey);
    }

    [Fact]
    public void Missing_society_identity_uses_accessible_fallback()
    {
        AgentSnapshot agent = Agent(Id(3), "Fallback", ScheduleActivity.Free);

        ResidentRosterRowViewModel row = Assert.Single(
            new ResidentRosterPresenter().Present(new[]
            {
                new ResidentRosterSource(agent),
            }).Rows);

        Assert.Equal(ResidentSexIndicator.Unknown, row.Sex);
        Assert.Equal("resident.sex.unknown", row.SexAccessibilityKey);
        Assert.Equal("Fallback", row.Name);
    }

    [Fact]
    public void Top_five_uses_full_skill_snapshot_and_stable_tie_break()
    {
        AgentSnapshot agent = Agent(
            Id(4),
            "Skilled",
            ScheduleActivity.Work,
            skills: new[]
            {
                Skill("skill.mining", 7000),
                Skill("skill.ranged_combat", 9000),
                Skill("skill.defense", 9000),
                Skill("skill.cooking", 8000),
                Skill("skill.crafting", 6000),
                Skill("skill.logistics", 5000),
                Skill("skill.research", 4000),
            });

        ResidentSkillSetViewModel skills = Assert.Single(
            new ResidentRosterPresenter().Present(new[]
            {
                new ResidentRosterSource(agent),
            }).Rows).Skills;

        Assert.Equal(7, skills.All.Count);
        Assert.Equal(new[]
        {
            "skill.defense",
            "skill.ranged_combat",
            "skill.cooking",
            "skill.mining",
            "skill.crafting",
        }, skills.TopFive.Select(item => item.SkillId).ToArray());
    }

    [Fact]
    public void Work_without_action_or_job_is_idle_at_work_not_free_time()
    {
        AgentSnapshot working = Agent(Id(5), "Worker", ScheduleActivity.Work);
        AgentSnapshot free = Agent(Id(6), "Free", ScheduleActivity.Free);
        ResidentRosterViewModel roster = new ResidentRosterPresenter().Present(new[]
        {
            new ResidentRosterSource(working),
            new ResidentRosterSource(free),
        });

        ResidentRosterRowViewModel workRow = roster.Rows.Single(item => item.Id == working.Id.ToString());
        ResidentRosterRowViewModel freeRow = roster.Rows.Single(item => item.Id == free.Id.ToString());
        Assert.Equal(ResidentActivityKind.Idle, workRow.Activity.Kind);
        Assert.True(workRow.IsIdleAtWork);
        Assert.Equal(ResidentActivityKind.FreeTime, freeRow.Activity.Kind);
        Assert.False(freeRow.IsIdleAtWork);
    }

    [Fact]
    public void Assigned_dig_job_overrides_generic_work_action_with_typed_destination()
    {
        EntityId agentId = Id(7);
        EntityId jobId = Id(70);
        CellId target = new CellId(9, 4);
        AgentSnapshot agent = Agent(
            agentId,
            "Digger",
            ScheduleActivity.Work,
            action: new AgentActionSnapshot(
                AgentIntentKind.Work,
                playerOrderId: null,
                startedTick: 10,
                requiredTicks: 4,
                elapsedTicks: 1));
        JobSnapshot job = new JobSnapshot(
            new DigJobDefinition(
                jobId,
                new DigJobTarget(target),
                priority: 10,
                createdTick: 8,
                JobRetryPolicy.Default),
            JobStatus.InProgress,
            JobStageKind.PerformWork,
            agentId,
            retryCount: 0,
            nextRetryTick: 0,
            version: 3,
            reason: null);

        ResidentRosterRowViewModel row = Assert.Single(
            new ResidentRosterPresenter().Present(
                new[] { agent },
                society: null,
                jobs: new[] { job },
                selectedResidentId: agentId).Rows);

        Assert.Equal(ResidentActivityKind.Dig, row.Activity.Kind);
        Assert.Equal(jobId.ToString(), row.Activity.SourceJobId);
        Assert.Equal(target, row.Activity.Destination);
        Assert.Null(row.Activity.SourceIntent);
        Assert.False(row.IsIdleAtWork);
    }

    [Fact]
    public void Action_descriptor_is_typed_and_missing_target_is_safe()
    {
        EntityId agentId = Id(8);
        AgentSnapshot agent = Agent(
            agentId,
            "Moving",
            ScheduleActivity.Work,
            action: new AgentActionSnapshot(
                AgentIntentKind.PlayerOrder,
                "order.move.1",
                startedTick: 1,
                requiredTicks: 5,
                elapsedTicks: 2,
                target: null));

        ResidentActivityDescriptor activity = Assert.Single(
            new ResidentRosterPresenter().Present(new[]
            {
                new ResidentRosterSource(agent),
            }).Rows).Activity;

        Assert.Equal(ResidentActivityKind.Move, activity.Kind);
        Assert.Equal(AgentIntentKind.PlayerOrder, activity.SourceIntent);
        Assert.Equal("order.move.1", activity.SourceOrderId);
        Assert.Null(activity.SubjectId);
        Assert.Equal("resident.activity.move", activity.LocalizationKey);
        Assert.Equal(0.4d, activity.Progress, precision: 6);
    }

    [Fact]
    public void Explicit_blocked_job_keeps_reason_code_without_ready_text()
    {
        EntityId agentId = Id(9);
        JobSnapshot blocked = new JobSnapshot(
            new DigJobDefinition(
                Id(90),
                new DigJobTarget(new CellId(2, 2)),
                priority: 5,
                createdTick: 0,
                JobRetryPolicy.Default),
            JobStatus.Blocked,
            JobStageKind.None,
            assignedAgentId: null,
            retryCount: 1,
            nextRetryTick: 10,
            version: 4,
            new JobBlockReason("path_missing", "Path is unavailable."));

        ResidentActivityDescriptor activity = Assert.Single(
            new ResidentRosterPresenter().Present(new[]
            {
                new ResidentRosterSource(
                    Agent(agentId, "Blocked", ScheduleActivity.Work),
                    currentJob: blocked),
            }).Rows).Activity;

        Assert.Equal(ResidentActivityKind.Blocked, activity.Kind);
        Assert.Equal("path_missing", activity.BlockReasonCode);
        Assert.Equal("resident.activity.blocked", activity.LocalizationKey);
    }

    [Fact]
    public void Virtualized_window_is_bounded_for_more_than_sixty_four_residents()
    {
        ResidentRosterSource[] sources = Enumerable.Range(1, 70)
            .Reverse()
            .Select(index => new ResidentRosterSource(
                Agent(Id(index), "Resident " + index, ScheduleActivity.Free)))
            .ToArray();

        ResidentRosterViewModel roster = new ResidentRosterPresenter().Present(sources);
        IReadOnlyList<ResidentRosterRowViewModel> window = roster.GetWindow(60, 16);

        Assert.Equal(70, roster.Rows.Count);
        Assert.Equal(10, window.Count);
        Assert.Equal(Id(61).ToString(), window[0].Id);
        Assert.Equal(Id(70).ToString(), window[9].Id);
        Assert.Empty(roster.GetWindow(70, 10));
    }

    private static AgentSnapshot Agent(
        EntityId id,
        string name,
        ScheduleActivity schedule,
        IReadOnlyCollection<AgentSkillValue>? skills = null,
        AgentActionSnapshot? action = null)
    {
        return new AgentSnapshot(
            id,
            name,
            version: 1,
            isAlive: true,
            new AgentNeedsSnapshot(
                new NeedValue(6000),
                new NeedValue(5100),
                new NeedValue(7600),
                new NeedValue(5000)),
            schedule,
            action,
            playerOrder: null,
            lastActionSwitchTick: -1,
            lastDecision: null,
            skills ?? Array.Empty<AgentSkillValue>(),
            traits: Array.Empty<AgentTraitId>(),
            position: new CellId(1, 1));
    }

    private static ResidentSocietySnapshot Society(
        AgentSnapshot agent,
        ResidentSex sex)
    {
        return new ResidentSocietySnapshot(
            agent.Id,
            agent.Name,
            sex,
            birthTick: 0,
            ResidentLifeStage.Adult,
            isAlive: true,
            motherId: null,
            fatherId: null,
            partnerId: null,
            pregnancy: null,
            agent.Position,
            deathCause: null,
            deathTick: null,
            new ResidentHeritage(5000));
    }

    private static AgentSkillValue Skill(string id, int level)
    {
        return new AgentSkillValue(new AgentSkillId(id), level);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}