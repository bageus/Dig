using System;
using System.Linq;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Jobs;

namespace Dig.Application.Jobs
{

public sealed class BypassSuggestedJobToolCommand : ICommand<Result>
{
    public BypassSuggestedJobToolCommand(EntityId jobId, long tick)
    {
        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        JobId = jobId;
        Tick = tick;
    }

    public EntityId JobId { get; }

    public long Tick { get; }
}

public sealed class BypassSuggestedJobToolHandler
    : ICommandHandler<BypassSuggestedJobToolCommand, Result>
{
    private readonly IJobRepository _jobs;
    private readonly IJobAssignmentReportSource _reports;
    private readonly IJobAssignmentReportSink _reportSink;
    private readonly IEventSink _eventSink;

    public BypassSuggestedJobToolHandler(
        IJobRepository jobs,
        IJobAssignmentReportSource reports,
        IJobAssignmentReportSink reportSink,
        IEventSink eventSink)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
        _reportSink = reportSink ?? throw new ArgumentNullException(nameof(reportSink));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(BypassSuggestedJobToolCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        JobSystem jobs = _jobs.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job is null)
        {
            return Result.Failure(JobErrors.NotFound);
        }

        if ((job.Status != JobStatus.Claimed && job.Status != JobStatus.InProgress)
            || !job.AssignedAgentId.HasValue)
        {
            return Result.Failure(JobErrors.InvalidStatus);
        }

        JobAssignmentReport? report = _reports.Find(command.JobId);
        JobAssignment? suggestion = report?.Assignments.SingleOrDefault(
            value => value.JobId == command.JobId);
        if (suggestion is null
            || suggestion.ToolPreparation != JobToolPreparationOutcome.Suggested
            || !suggestion.ToolStackId.HasValue)
        {
            return Result.Failure(PrepareSuggestedJobToolErrors.SuggestionUnavailable);
        }

        if (report!.Tick > command.Tick)
        {
            return Result.Failure(PrepareSuggestedJobToolErrors.SuggestionStale);
        }

        EntityId toolStackId = suggestion.ToolStackId.Value;
        bool ownsToolReservation = jobs.GetReservations().Any(
            value => value.JobId == command.JobId
                && value.Key == ReservationKey.ForTool(toolStackId));
        if (ownsToolReservation)
        {
            Result released = jobs.ReleaseToolReservation(
                command.JobId,
                toolStackId,
                command.Tick);
            if (released.IsFailure)
            {
                return released;
            }
        }

        _jobs.Save(jobs);
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        _reportSink.Record(new JobAssignmentReport(
            command.Tick,
            new[]
            {
                new JobAssignment(
                    command.JobId,
                    job.AssignedAgentId.Value,
                    suggestion.Score,
                    JobToolPreparationOutcome.Bypassed,
                    toolStackId),
            },
            Array.Empty<JobAssignmentFailure>()));
        return Result.Success();
    }
}
}
