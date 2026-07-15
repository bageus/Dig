using System;
using Dig.Application.Agents;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Jobs;

namespace Dig.Application.Combat
{

public sealed class CompleteHealingJobHandler
    : ICommandHandler<CompleteHealingJobCommand, Result>
{
    private readonly IAgentRepository _agents;
    private readonly IJobRepository _jobs;
    private readonly IEventSink _eventSink;

    public CompleteHealingJobHandler(
        IAgentRepository agents,
        IJobRepository jobs,
        IEventSink eventSink)
    {
        _agents = agents ?? throw new ArgumentNullException(nameof(agents));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(CompleteHealingJobCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        JobSystem jobs = _jobs.Get();
        JobSnapshot? snapshot = jobs.Get(command.JobId);
        if (snapshot is null
            || snapshot.Definition is not HealingJobDefinition healing
            || healing.PatientId != command.PatientId
            || (snapshot.Status != JobStatus.Claimed
                && snapshot.Status != JobStatus.InProgress))
        {
            return Result.Failure(CombatApplicationErrors.HealingJobInvalid);
        }

        AgentState? patient = _agents.Get(command.PatientId);
        if (patient is null)
        {
            return Result.Failure(CombatApplicationErrors.AgentNotFound);
        }

        Result healed = patient.ApplyExternalNeedDelta(
            new NeedDelta(0, 0, 0, healing.HealthRestored),
            "healing-job:" + command.JobId,
            command.Tick);
        if (healed.IsFailure)
        {
            return healed;
        }

        Result completed = jobs.Complete(command.JobId, command.Tick);
        if (completed.IsFailure)
        {
            return completed;
        }

        _agents.Save(patient);
        _jobs.Save(jobs);
        _eventSink.Append(patient.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}
}
