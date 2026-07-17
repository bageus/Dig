using System;
using System.Linq;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Jobs;

namespace Dig.Application.Jobs
{

public static class PrepareSuggestedJobToolErrors
{
    public static readonly DomainError SuggestionUnavailable = new DomainError(
        "jobs.tool_suggestion_unavailable",
        "The job does not have an active suggested tool assignment.");
    public static readonly DomainError SuggestionStale = new DomainError(
        "jobs.tool_suggestion_stale",
        "The suggested tool assignment no longer matches the active resident.");
    public static readonly DomainError ToolReservationMissing = new DomainError(
        "jobs.tool_reservation_missing",
        "The suggested tool is no longer reserved by the active job.");
}

public sealed class PrepareSuggestedJobToolCommand : ICommand<Result>
{
    public PrepareSuggestedJobToolCommand(EntityId jobId, long tick)
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

public sealed class PrepareSuggestedJobToolHandler
    : ICommandHandler<PrepareSuggestedJobToolCommand, Result>
{
    private readonly IJobRepository _jobs;
    private readonly IJobToolPreparationService _preparation;
    private readonly IJobAssignmentReportSource _reports;
    private readonly IJobAssignmentReportSink _reportSink;

    public PrepareSuggestedJobToolHandler(
        IJobRepository jobs,
        IJobToolPreparationService preparation,
        IJobAssignmentReportSource reports,
        IJobAssignmentReportSink reportSink)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _preparation = preparation ?? throw new ArgumentNullException(nameof(preparation));
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
        _reportSink = reportSink ?? throw new ArgumentNullException(nameof(reportSink));
    }

    public Result Handle(PrepareSuggestedJobToolCommand command)
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

        if (report!.Tick > command.Tick
            || suggestion.AgentId != job.AssignedAgentId.Value)
        {
            return Result.Failure(PrepareSuggestedJobToolErrors.SuggestionStale);
        }

        EntityId toolStackId = suggestion.ToolStackId.Value;
        ReservationSnapshot? reservation = jobs.GetReservations().SingleOrDefault(
            value => value.JobId == command.JobId
                && value.Key == ReservationKey.ForTool(toolStackId));
        if (reservation is null || reservation.AgentId != suggestion.AgentId)
        {
            return Result.Failure(PrepareSuggestedJobToolErrors.ToolReservationMissing);
        }

        Result prepared = _preparation.Prepare(
            suggestion.AgentId,
            toolStackId,
            command.Tick);
        if (prepared.IsFailure)
        {
            return prepared;
        }

        _reportSink.Record(new JobAssignmentReport(
            command.Tick,
            new[]
            {
                new JobAssignment(
                    command.JobId,
                    suggestion.AgentId,
                    suggestion.Score,
                    JobToolPreparationOutcome.Switched,
                    toolStackId),
            },
            Array.Empty<JobAssignmentFailure>()));
        return Result.Success();
    }
}
}
