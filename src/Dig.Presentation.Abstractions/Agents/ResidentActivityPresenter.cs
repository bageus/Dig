using System;
using System.Collections.Generic;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Strategy;
using Dig.Domain.World;

namespace Dig.Presentation.Agents
{

internal static class ResidentActivityPresenter
{
    public static ResidentActivityDescriptor Present(
        AgentSnapshot agent,
        JobSnapshot? job)
    {
        if (agent is null)
        {
            throw new ArgumentNullException(nameof(agent));
        }

        if (job is not null)
        {
            return PresentJob(agent.Id, job);
        }

        if (agent.ActiveAction.HasValue)
        {
            return PresentAction(agent.Id, agent.ActiveAction.Value);
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

    private static ResidentActivityDescriptor PresentAction(
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

    private static ResidentActivityDescriptor PresentJob(
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
            arguments.Add(new ResidentLocalizationArgument(
                "destination",
                destination.Value.ToString()));
        }

        return arguments;
    }
}
}