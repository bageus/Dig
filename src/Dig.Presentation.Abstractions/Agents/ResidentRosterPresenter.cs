using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.Jobs;
using Dig.Domain.Society;
using Dig.Domain.Strategy;
using Dig.Domain.World;

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
            throw new ArgumentException("Society snapshot must describe the same resident.", nameof(society));
        }

        Agent = agent;
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
            throw new ArgumentException("Resident roster sources must have unique ids.", nameof(sources));
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
        ResidentActivityDescriptor activity = CreateActivity(agent, source.CurrentJob);
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

    private static ResidentActivityDescriptor CreateActivity(
        AgentSnapshot agent,
        JobSnapshot? job)
    {
        if (job is not null)
        {
            return CreateJobActivity(agent.Id, job);
        }

        if (agent.ActiveAction.HasValue)
        {
            return CreateActionActivity(agent.Id, agent.ActiveAction.Value);
        }

        ResidentActivityKind kind = agent.ScheduledActivity switch
        {
            ScheduleActivity.Work => ResidentActivityKind.Idle,
            ScheduleActivity.Rest => ResidentActivityKind.Rest,
            ScheduleActivity.Sleep => ResidentActivityKind.Sleep,
            ScheduleActivity.Free => ResidentActivityKind.FreeTime,
            _ => ResidentActivityKind.Idle,
        };
        return Descriptor(kind, agent.Id);
    }

    private static ResidentActivityDescriptor CreateActionActivity(
        EntityId actorId,
        AgentActionSnapshot action)
    {
        ResidentActivityKind kind = action.IntentKind switch
        {
            AgentIntentKind.Flee => ResidentActivityKind.Flee,
            AgentIntentKind.Eat => ResidentActivityKind.Eat,
            AgentIntentKind.Sleep => ResidentActivityKind.Sleep,
            AgentIntentKind.PlayerOrder => ResidentActivityKind.Move,
            AgentIntentKind.Work => ResidentActivityKind.Work,
            AgentIntentKind.Rest => ResidentActivityKind.Rest,
            AgentIntentKind.Idle => ResidentActivityKind.Idle,
            _ => ResidentActivityKind.Idle,
        };
        string? subjectId = action.Target?.EntityId.ToString();
        return new ResidentActivityDescriptor(
            kind,
            actorId.ToString(),
            LocalizationKey(kind),
            subjectId,
            sourceIntent: action.IntentKind,
            sourceOrderId: action.PlayerOrderId,
            progressCurrent: action.ElapsedTicks,
            progressMaximum: action.RequiredTicks,
            localizationArguments: Arguments(actorId, subjectId, destination: null));
    }

    private static ResidentActivityDescriptor CreateJobActivity(
        EntityId actorId,
        JobSnapshot job)
    {
        ResidentActivityKind kind = ClassifyJob(job.Definition, job.Stage);
        string? subjectId = SubjectId(job.Definition);
        CellId? destination = Destination(job.Definition);
        int progressMaximum = job.Definition.Stages.Count;
        int progressCurrent = ResolveStageIndex(job);
        if (job.Status == JobStatus.Blocked)
        {
            kind = ResidentActivityKind.Blocked;
        }

        return new ResidentActivityDescriptor(
            kind,
            actorId.ToString(),
            LocalizationKey(kind),
            subjectId,
            destination,
            job.Id.ToString(),
            sourceJobStage: job.Stage,
            progressCurrent: progressCurrent,
            progressMaximum: progressMaximum,
            blockReasonCode: job.Reason?.Code,
            localizationArguments: Arguments(actorId, subjectId, destination));
    }

    private static ResidentActivityKind ClassifyJob(
        JobDefinition definition,
        JobStageKind stage)
    {
        if (definition is DigJobDefinition)
        {
            return ResidentActivityKind.Dig;
        }

        if (definition is HaulJobDefinition)
        {
            return stage == JobStageKind.AcquireItem
                ? ResidentActivityKind.Pickup
                : ResidentActivityKind.Logistics;
        }

        if (definition is ProductionWorkJobDefinition)
        {
            return ResidentActivityKind.Craft;
        }

        if (definition is HealingJobDefinition)
        {
            return ResidentActivityKind.Service;
        }

        if (definition is StrategicExecutionJobDefinition strategic)
        {
            return strategic.Goal switch
            {
                StrategicGoalKind.Attack => ResidentActivityKind.Attack,
                StrategicGoalKind.Retreat => ResidentActivityKind.Flee,
                _ => ResidentActivityKind.Work,
            };
        }

        return ResidentActivityKind.Work;
    }

    private static string? SubjectId(JobDefinition definition)
    {
        return definition switch
        {
            HaulJobDefinition haul => haul.SourceStackId.ToString(),
            ProductionWorkJobDefinition production => production.BuildingId.ToString(),
            HealingJobDefinition healing => healing.PatientId.ToString(),
            StrategicExecutionJobDefinition strategic when strategic.TargetFactionId.HasValue =>
                strategic.TargetFactionId.Value.ToString(),
            _ => null,
        };
    }

    private static CellId? Destination(JobDefinition definition)
    {
        return definition switch
        {
            DigJobDefinition dig => dig.Target.CellId,
            HaulJobDefinition haul when haul.Destination.HasCell => haul.Destination.CellId,
            ProductionWorkJobDefinition production => production.WorkPosition,
            HealingJobDefinition healing => healing.WorkPosition,
            StrategicExecutionJobDefinition strategic => strategic.TargetCell,
            _ => null,
        };
    }

    private static int ResolveStageIndex(JobSnapshot job)
    {
        if (job.Status != JobStatus.InProgress || job.Stage == JobStageKind.None)
        {
            return 0;
        }

        for (int index = 0; index < job.Definition.Stages.Count; index++)
        {
            if (job.Definition.Stages[index] == job.Stage)
            {
                return index;
            }
        }

        return 0;
    }

    private static ResidentActivityDescriptor Descriptor(
        ResidentActivityKind kind,
        EntityId actorId)
    {
        return new ResidentActivityDescriptor(
            kind,
            actorId.ToString(),
            LocalizationKey(kind),
            localizationArguments: Arguments(actorId, subjectId: null, destination: null));
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

    private static string LocalizationKey(ResidentActivityKind kind)
    {
        return "resident.activity." + kind.ToString().ToLowerInvariant();
    }

    private static IReadOnlyList<ResidentLocalizationArgument> Arguments(
        EntityId actorId,
        string? subjectId,
        CellId? destination)
    {
        List<ResidentLocalizationArgument> arguments = new List<ResidentLocalizationArgument>
        {
            new ResidentLocalizationArgument("actor", actorId.ToString()),
        };
        if (!string.IsNullOrWhiteSpace(subjectId))
        {
            arguments.Add(new ResidentLocalizationArgument("subject", subjectId));
        }

        if (destination.HasValue)
        {
            arguments.Add(new ResidentLocalizationArgument("destination", destination.Value.ToString()));
        }

        return arguments;
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