using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Jobs;

namespace Dig.Unity
{
    public sealed partial class DigAgentRenderer
    {
        internal void SynchronizeWorkFacing(
            IReadOnlyList<JobOverlayViewModel> jobs,
            TunnelNavigationVolume tunnelVolume,
            WorldSnapshot world)
        {
            if (jobs == null)
            {
                throw new ArgumentNullException(nameof(jobs));
            }

            if (tunnelVolume == null)
            {
                throw new ArgumentNullException(nameof(tunnelVolume));
            }

            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
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

            HashSet<CellId> solidCells = new HashSet<CellId>(
                world.Chunks.SelectMany(chunk => chunk.Cells)
                    .Where(cell => cell.IsSolid)
                    .Select(cell => cell.Id));
            foreach (KeyValuePair<string, DigAgentVisual> pair in _agents)
            {
                CellId current = new CellId(
                    pair.Value.Model.CellX,
                    pair.Value.Model.CellY,
                    pair.Value.Model.CellZ);
                bool climbingWork = workTargets.ContainsKey(pair.Key)
                    && tunnelVolume.IsVerticalTunnel(current)
                    && !solidCells.Contains(new CellId(
                        current.X,
                        current.Y + 1,
                        current.Z));
                pair.Value.SetWorkTarget(
                    workTargets.TryGetValue(pair.Key, out CellId target)
                        ? target
                        : (CellId?)null,
                    climbingWork);
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
