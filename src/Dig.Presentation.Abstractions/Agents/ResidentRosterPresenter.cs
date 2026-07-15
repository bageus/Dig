using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Society;

namespace Dig.Presentation.Agents
{

public sealed class ResidentRosterSource
{
    public ResidentRosterSource(
        AgentSnapshot agent,
        ResidentSocietySnapshot? society = null,
        JobSnapshot? currentJob = null)
    {
        Agent = agent ?? throw new ArgumentNullException(nameof(agent));
        if (society is not null && society.Id != agent.Id)
        {
            throw new ArgumentException(
                "Society snapshot must describe the same resident.",
                nameof(society));
        }

        Society = society;
        CurrentJob = currentJob;
    }

    public AgentSnapshot Agent { get; }
    public ResidentSocietySnapshot? Society { get; }
    public JobSnapshot? CurrentJob { get; }
}

public sealed class ResidentRosterPresenter
{
    public ResidentRosterViewModel Present(
        IReadOnlyList<AgentSnapshot> agents,
        SocietySnapshot? society,
        IReadOnlyList<JobSnapshot> jobs,
        EntityId? selectedResidentId = null)
    {
        if (agents is null)
        {
            throw new ArgumentNullException(nameof(agents));
        }

        if (jobs is null)
        {
            throw new ArgumentNullException(nameof(jobs));
        }

        Dictionary<EntityId, ResidentSocietySnapshot> societyById = society?.Residents
            .ToDictionary(item => item.Id)
            ?? new Dictionary<EntityId, ResidentSocietySnapshot>();
        Dictionary<EntityId, JobSnapshot> jobByAgent = jobs
            .Where(item => !item.IsTerminal && item.AssignedAgentId.HasValue)
            .OrderBy(item => item.Definition.CreatedTick)
            .ThenBy(item => item.Id.ToString(), StringComparer.Ordinal)
            .GroupBy(item => item.AssignedAgentId!.Value)
            .ToDictionary(group => group.Key, group => group.First());
        ResidentRosterSource[] sources = agents.Select(agent =>
        {
            societyById.TryGetValue(agent.Id, out ResidentSocietySnapshot? resident);
            jobByAgent.TryGetValue(agent.Id, out JobSnapshot? job);
            return new ResidentRosterSource(agent, resident, job);
        }).ToArray();
        return Present(sources, selectedResidentId);
    }

    public ResidentRosterViewModel Present(
        IEnumerable<ResidentRosterSource> sources,
        EntityId? selectedResidentId = null)
    {
        if (sources is null)
        {
            throw new ArgumentNullException(nameof(sources));
        }

        ResidentRosterSource[] ordered = sources
            .Where(item => item is not null
                && item.Agent.IsAlive
                && (item.Society is null || item.Society.IsAlive))
            .OrderBy(item => item.Agent.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        if (ordered.Select(item => item.Agent.Id).Distinct().Count() != ordered.Length)
        {
            throw new ArgumentException(
                "Resident roster sources must have unique ids.",
                nameof(sources));
        }

        string? selectedId = selectedResidentId?.ToString();
        ResidentRosterRowViewModel[] rows = new ResidentRosterRowViewModel[ordered.Length];
        for (int index = 0; index < ordered.Length; index++)
        {
            ResidentRosterSource source = ordered[index];
            rows[index] = CreateRow(
                source,
                string.Equals(
                    source.Agent.Id.ToString(),
                    selectedId,
                    StringComparison.Ordinal));
        }

        return new ResidentRosterViewModel(rows, selectedId);
    }

    public static ResidentNeedBand ClassifyNeed(int value)
    {
        ValidateNeed(value);
        if (value <= 2_500)
        {
            return ResidentNeedBand.Critical;
        }

        return value <= 5_000
            ? ResidentNeedBand.Warning
            : ResidentNeedBand.Healthy;
    }

    public static ResidentMoodFace ClassifyMood(int value)
    {
        ValidateNeed(value);
        if (value <= 2_500)
        {
            return ResidentMoodFace.Sad;
        }

        return value <= 7_500
            ? ResidentMoodFace.Neutral
            : ResidentMoodFace.Joy;
    }

    private static ResidentRosterRowViewModel CreateRow(
        ResidentRosterSource source,
        bool isExpanded)
    {
        AgentSnapshot agent = source.Agent;
        ResidentActivityDescriptor activity = ResidentActivityPresenter.Present(
            agent,
            source.CurrentJob);
        ResidentSexIndicator sex = source.Society is null
            ? ResidentSexIndicator.Unknown
            : source.Society.Sex == ResidentSex.Female
                ? ResidentSexIndicator.Female
                : ResidentSexIndicator.Male;
        ResidentSkillSetViewModel skills = new ResidentSkillSetViewModel(
            agent.Skills.Select(item => new ResidentSkillViewModel(
                item.Id.ToString(),
                item.Level)));
        bool idleAtWork = agent.ScheduledActivity == ScheduleActivity.Work
            && agent.ActiveAction is null
            && source.CurrentJob is null
            && activity.Kind == ResidentActivityKind.Idle;

        return new ResidentRosterRowViewModel(
            agent.Id.ToString(),
            source.Society?.Name ?? agent.Name,
            agent.Version,
            agent.IsAlive,
            isExpanded,
            sex,
            SexAccessibilityKey(sex),
            agent.ScheduledActivity,
            ClassifyMood(agent.Needs.Mood.Points),
            Need(agent.Needs.Health.Points, "resident.need.health"),
            Need(agent.Needs.Nutrition.Points, "resident.need.nutrition"),
            Need(agent.Needs.Alertness.Points, "resident.need.alertness.vigor"),
            Need(agent.Needs.Mood.Points, "resident.need.mood"),
            activity,
            idleAtWork,
            skills);
    }

    private static ResidentNeedViewModel Need(int value, string accessibilityKey)
    {
        return new ResidentNeedViewModel(value, ClassifyNeed(value), accessibilityKey);
    }

    private static string SexAccessibilityKey(ResidentSexIndicator sex)
    {
        return sex switch
        {
            ResidentSexIndicator.Female => "resident.sex.female",
            ResidentSexIndicator.Male => "resident.sex.male",
            _ => "resident.sex.unknown",
        };
    }

    private static void ValidateNeed(int value)
    {
        if (value < NeedValue.Minimum || value > NeedValue.Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
    }
}
}