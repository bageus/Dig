using System;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Jobs;

namespace Dig.Presentation.Jobs
{

internal static class JobExecutionReadinessProjection
{
    private const string ReadyLabel = "Ready";
    private const string WaitingLabel = "Waiting for tool decision";
    private const string WaitingReasonCode = "jobs.waiting_for_tool_decision";
    private const string WaitingReasonMessage =
        "Equip the suggested tool or proceed without it before this Job can advance.";

    public static JobExecutionReadinessViewModel Map(
        JobSnapshot job,
        JobAssignmentReport? report)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        JobAssignment? assignment = report?.Assignments.SingleOrDefault(
            value => value.JobId == job.Id);
        bool isActive = job.Status == JobStatus.Claimed
            || job.Status == JobStatus.InProgress;
        if (isActive
            && assignment?.ToolPreparation == JobToolPreparationOutcome.Suggested)
        {
            return new JobExecutionReadinessViewModel(
                JobExecutionReadinessKind.WaitingForToolDecision,
                WaitingLabel,
                WaitingReasonCode,
                WaitingReasonMessage);
        }

        return new JobExecutionReadinessViewModel(
            JobExecutionReadinessKind.Ready,
            ReadyLabel);
    }
}
}
