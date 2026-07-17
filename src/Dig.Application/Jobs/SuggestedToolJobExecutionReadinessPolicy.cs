using System;
using System.Linq;
using Dig.Domain.Jobs;

namespace Dig.Application.Jobs
{

public sealed class SuggestedToolJobExecutionReadinessPolicy
    : IJobExecutionReadinessPolicy
{
    private readonly IJobAssignmentReportSource _reports;

    public SuggestedToolJobExecutionReadinessPolicy(IJobAssignmentReportSource reports)
    {
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
    }

    public bool CanAdvance(JobSnapshot job)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        JobAssignment? assignment = _reports.Find(job.Id)?.Assignments
            .SingleOrDefault(value => value.JobId == job.Id);
        return assignment?.ToolPreparation != JobToolPreparationOutcome.Suggested;
    }
}
}
