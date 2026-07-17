using System;
using System.Collections.Generic;
using System.Linq;

namespace Dig.Presentation.Jobs
{

public static class JobAttentionProjection
{
    public const int DefaultMaximumItems = 3;

    public static JobAttentionSummaryViewModel Project(
        IReadOnlyList<JobOverlayViewModel> jobs,
        int maximumItems = DefaultMaximumItems)
    {
        if (jobs is null)
        {
            throw new ArgumentNullException(nameof(jobs));
        }

        if (maximumItems <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumItems));
        }

        JobOverlayViewModel[] waiting = jobs
            .Where(job => !job.ExecutionReadiness.IsReady)
            .OrderByDescending(job => job.Priority)
            .ThenBy(job => job.Id, StringComparer.Ordinal)
            .ToArray();
        JobAttentionItemViewModel[] items = waiting
            .Take(maximumItems)
            .Select(job => new JobAttentionItemViewModel(
                job.Id,
                job.Description,
                job.ExecutionReadiness.Label,
                job.ExecutionReadiness.ReasonCode!,
                job.AssignedAgentId))
            .ToArray();
        return new JobAttentionSummaryViewModel(waiting.Length, items);
    }
}
}