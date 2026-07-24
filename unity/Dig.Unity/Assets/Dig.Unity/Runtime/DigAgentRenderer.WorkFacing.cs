using System;
using System.Collections.Generic;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Presentation.Jobs;

namespace Dig.Unity
{
    public sealed partial class DigAgentRenderer
    {
        internal void SynchronizeWorkFacing(IReadOnlyList<JobOverlayViewModel> jobs)
        {
            if (jobs == null)
            {
                throw new ArgumentNullException(nameof(jobs));
            }

            Dictionary<string, CellId> workTargets =
                new Dictionary<string, CellId>(StringComparer.Ordinal);
            for (int index = 0; index < jobs.Count; index++)
            {
                JobOverlayViewModel job = jobs[index];
                if (!IsActiveMiningWork(job)
                    || workTargets.ContainsKey(job.AssignedAgentId!))
                {
                    continue;
                }

                workTargets.Add(
                    job.AssignedAgentId!,
                    new CellId(
                        job.TargetX!.Value,
                        job.TargetY!.Value,
                        job.TargetZ!.Value));
            }

            foreach (KeyValuePair<string, DigAgentVisual> pair in _agents)
            {
                pair.Value.SetWorkTarget(
                    workTargets.TryGetValue(pair.Key, out CellId target)
                        ? target
                        : (CellId?)null);
            }
        }

        private static bool IsActiveMiningWork(JobOverlayViewModel job)
        {
            return job.AssignedAgentId != null
                && job.HasTarget
                && job.PreferredToolKind == JobToolKind.Mining
                && string.Equals(
                    job.Status,
                    JobStatus.InProgress.ToString(),
                    StringComparison.Ordinal)
                && string.Equals(
                    job.Stage,
                    JobStageKind.PerformWork.ToString(),
                    StringComparison.Ordinal);
        }
    }
}
