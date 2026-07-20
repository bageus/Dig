using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Jobs;

namespace Dig.Presentation.Jobs
{

public sealed class JobOverlayPresenter
{
    private const string PrepareSuggestedToolLabel = "Equip suggested tool";
    private const string BypassSuggestedToolLabel = "Proceed without suggested tool";
    private readonly IQueryHandler<GetJobsQuery, IReadOnlyList<JobSnapshot>> _jobs;
    private readonly IQueryHandler<GetJobReservationsQuery, IReadOnlyList<ReservationSnapshot>> _reservations;

    public JobOverlayPresenter(
        IQueryHandler<GetJobsQuery, IReadOnlyList<JobSnapshot>> jobs,
        IQueryHandler<GetJobReservationsQuery, IReadOnlyList<ReservationSnapshot>> reservations)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _reservations = reservations ?? throw new ArgumentNullException(nameof(reservations));
    }

    public IReadOnlyList<JobOverlayViewModel> Load(
        JobAssignmentReport? latestAssignmentReport = null)
    {
        return LoadCore(latestAssignmentReport, assignmentReports: null);
    }

    public IReadOnlyList<JobOverlayViewModel> LoadIndexed(
        IReadOnlyDictionary<EntityId, JobAssignmentReport> assignmentReports)
    {
        if (assignmentReports is null)
        {
            throw new ArgumentNullException(nameof(assignmentReports));
        }

        return LoadCore(latestAssignmentReport: null, assignmentReports);
    }

    private IReadOnlyList<JobOverlayViewModel> LoadCore(
        JobAssignmentReport? latestAssignmentReport,
        IReadOnlyDictionary<EntityId, JobAssignmentReport>? assignmentReports)
    {
        IReadOnlyList<JobSnapshot> jobs = _jobs.Handle(new GetJobsQuery());
        IReadOnlyList<ReservationSnapshot> reservations = _reservations.Handle(
            new GetJobReservationsQuery());
        JobOverlayViewModel[] models = new JobOverlayViewModel[jobs.Count];
        for (int index = 0; index < jobs.Count; index++)
        {
            JobSnapshot job = jobs[index];
            JobAssignmentReport? report = ResolveReport(
                job.Id,
                latestAssignmentReport,
                assignmentReports);
            models[index] = Map(
                job,
                reservations,
                MapAssignment(job.Id, report),
                MapActions(job, reservations, report),
                JobExecutionReadinessProjection.Map(job, report));
        }

        return new ReadOnlyCollection<JobOverlayViewModel>(models);
    }

    private static JobOverlayViewModel Map(
        JobSnapshot job,
        IReadOnlyList<ReservationSnapshot> reservations,
        JobAssignmentDiagnosticViewModel? assignmentDiagnostic,
        IReadOnlyList<JobActionViewModel> actions,
        JobExecutionReadinessViewModel executionReadiness)
    {
        JobReservationViewModel[] values = reservations
            .Where(item => item.JobId == job.Id)
            .Select(item => new JobReservationViewModel(
                item.Key.Kind.ToString(),
                item.Key.Value,
                item.AgentId.ToString(),
                item.AcquiredTick))
            .OrderBy(item => item.Kind, StringComparer.Ordinal)
            .ThenBy(item => item.Value, StringComparer.Ordinal)
            .ToArray();
        int? targetX = null;
        int? targetY = null;
        int? targetZ = null;
        if (job.Definition is DigJobDefinition digging)
        {
            targetX = digging.Target.CellId.X;
            targetY = digging.Target.CellId.Y;
            targetZ = 0;
        }
        else if (job.Definition is SpatialDigJobDefinition spatial)
        {
            targetX = spatial.Target.TargetCell.X;
            targetY = spatial.Target.TargetCell.Y;
            targetZ = spatial.Target.TargetCell.Z;
        }

        return new JobOverlayViewModel(
            job.Id.ToString(),
            job.Definition.Description,
            job.Status.ToString(),
            job.Stage.ToString(),
            job.Definition.Priority,
            job.AssignedAgentId?.ToString(),
            targetX,
            targetY,
            job.RetryCount,
            job.NextRetryTick,
            job.Reason?.ToString(),
            new ReadOnlyCollection<JobReservationViewModel>(values),
            job.Definition.PreferredToolKind,
            assignmentDiagnostic,
            actions,
            executionReadiness,
            targetZ);
    }

    private static IReadOnlyList<JobActionViewModel> MapActions(
        JobSnapshot job,
        IReadOnlyList<ReservationSnapshot> reservations,
        JobAssignmentReport? report)
    {
        JobAssignment? suggestion = report?.Assignments.SingleOrDefault(
            item => item.JobId == job.Id);
        if (suggestion is null
            || suggestion.ToolPreparation != JobToolPreparationOutcome.Suggested
            || !suggestion.ToolStackId.HasValue)
        {
            return Array.Empty<JobActionViewModel>();
        }

        DomainError? prepareDisabledReason = ResolvePrepareDisabledReason(
            job,
            suggestion,
            reservations);
        DomainError? bypassDisabledReason = ResolveBypassDisabledReason(job);
        return new[]
        {
            new JobActionViewModel(
                JobActionKind.PrepareSuggestedTool,
                PrepareSuggestedToolLabel,
                isEnabled: prepareDisabledReason is null,
                disabledReasonCode: prepareDisabledReason?.Code,
                disabledReasonMessage: prepareDisabledReason?.Message),
            new JobActionViewModel(
                JobActionKind.BypassSuggestedTool,
                BypassSuggestedToolLabel,
                isEnabled: bypassDisabledReason is null,
                disabledReasonCode: bypassDisabledReason?.Code,
                disabledReasonMessage: bypassDisabledReason?.Message),
        };
    }

    private static DomainError? ResolvePrepareDisabledReason(
        JobSnapshot job,
        JobAssignment suggestion,
        IReadOnlyList<ReservationSnapshot> reservations)
    {
        DomainError? statusReason = ResolveBypassDisabledReason(job);
        if (statusReason != null)
        {
            return statusReason;
        }

        if (suggestion.AgentId != job.AssignedAgentId!.Value)
        {
            return PrepareSuggestedJobToolErrors.SuggestionStale;
        }

        EntityId toolStackId = suggestion.ToolStackId!.Value;
        ReservationSnapshot? reservation = reservations.SingleOrDefault(
            value => value.JobId == job.Id
                && value.Key == ReservationKey.ForTool(toolStackId));
        return reservation is null || reservation.AgentId != suggestion.AgentId
            ? PrepareSuggestedJobToolErrors.ToolReservationMissing
            : null;
    }

    private static DomainError? ResolveBypassDisabledReason(JobSnapshot job)
    {
        return (job.Status != JobStatus.Claimed && job.Status != JobStatus.InProgress)
            || !job.AssignedAgentId.HasValue
            ? JobErrors.InvalidStatus
            : null;
    }

    private static JobAssignmentDiagnosticViewModel? MapAssignment(
        EntityId jobId,
        JobAssignmentReport? report)
    {
        if (report is null)
        {
            return null;
        }

        JobAssignment? assignment = report.Assignments
            .FirstOrDefault(item => item.JobId == jobId);
        if (assignment != null)
        {
            return new JobAssignmentDiagnosticViewModel(
                report.Tick,
                assignment.Score,
                assignment.ToolPreparation,
                assignment.ToolStackId?.ToString(),
                failureCode: null,
                failureMessage: null);
        }

        JobAssignmentFailure? failure = report.Failures
            .FirstOrDefault(item => item.JobId == jobId);
        return failure is null
            ? null
            : new JobAssignmentDiagnosticViewModel(
                report.Tick,
                score: null,
                toolPreparation: null,
                toolStackId: null,
                failureCode: failure.Error.Code,
                failureMessage: failure.Error.Message);
    }

    private static JobAssignmentReport? ResolveReport(
        EntityId jobId,
        JobAssignmentReport? latestAssignmentReport,
        IReadOnlyDictionary<EntityId, JobAssignmentReport>? assignmentReports)
    {
        if (assignmentReports != null
            && assignmentReports.TryGetValue(jobId, out JobAssignmentReport? indexedReport))
        {
            return indexedReport;
        }

        return latestAssignmentReport;
    }
}
}