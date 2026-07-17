using System;
using System.Collections.Generic;

namespace Dig.Presentation.Jobs
{

public static class JobSelectionProjection
{
    public static JobOverlayViewModel? Resolve(
        IReadOnlyList<JobOverlayViewModel> jobs,
        string? selectedJobId)
    {
        if (jobs is null)
        {
            throw new ArgumentNullException(nameof(jobs));
        }

        if (string.IsNullOrWhiteSpace(selectedJobId))
        {
            return null;
        }

        for (int index = 0; index < jobs.Count; index++)
        {
            JobOverlayViewModel model = jobs[index]
                ?? throw new ArgumentException(
                    "Job projections cannot contain null values.",
                    nameof(jobs));
            if (string.Equals(model.Id, selectedJobId, StringComparison.Ordinal))
            {
                return model;
            }
        }

        return null;
    }
}
}
