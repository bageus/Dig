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
            HashSet<string> nonClimbingWorkers = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> barrelWorkers = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < jobs.Count; index++)
            {
                JobOverlayViewModel job = jobs[index];
                if (!IsActiveToolWork(job)
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
                if (job.IsMushroomChop || job.IsBarrelAttack)
                {
                    nonClimbingWorkers.Add(job.AssignedAgentId!);
                }

                if (job.IsBarrelAttack)
                {
                    barrelWorkers.Add(job.AssignedAgentId!);
                }
            }

            Dictionary<CellId, CellSnapshot> worldCells = world.Chunks
                .SelectMany(chunk => chunk.Cells)
                .ToDictionary(cell => cell.Id);
            foreach (KeyValuePair<string, DigAgentVisual> pair in _agents)
            {
                bool hasToolWork = workTargets.TryGetValue(pair.Key, out CellId target);
                CellId current = new CellId(
                    pair.Value.Model.CellX,
                    pair.Value.Model.CellY,
                    pair.Value.Model.CellZ);
                CellId below = new CellId(current.X, current.Y + 1, current.Z);
                bool hasFullSupport = worldCells.TryGetValue(
                        below,
                        out CellSnapshot support)
                    && support.IsSolid
                    && support.State.CompletedExcavationQuarters == ExcavationQuarter.None;
                bool climbingWork = RequiresClimbingWorkPose(
                    hasToolWork,
                    nonClimbingWorkers.Contains(pair.Key),
                    hasFullSupport);
                bool barrelAttack = hasToolWork && barrelWorkers.Contains(pair.Key);
                pair.Value.SetWorkTarget(
                    hasToolWork ? target : (CellId?)null,
                    climbingWork,
                    animateToolWork: hasToolWork && !barrelAttack,
                    animateAttackWork: barrelAttack);
            }
        }

        internal static bool RequiresClimbingWorkPose(
            bool hasToolWork,
            bool isNonClimbingWork,
            bool hasFullSupport)
        {
            // Support is authoritative. Once any quarter below is committed, or a
            // shaft worker has no floor at all, every mining direction uses the
            // stationary climbing pose. Do not depend on template/shaft provenance:
            // side excavation from an unsupported cell must behave identically.
            return hasToolWork && !isNonClimbingWork && !hasFullSupport;
        }

        private static bool IsActiveToolWork(JobOverlayViewModel job)
        {
            bool supportedTool = job.PreferredToolKind == JobToolKind.Mining
                || job.IsMushroomChop
                || job.IsBarrelAttack;
            return job.AssignedAgentId != null
                && job.HasTarget
                && supportedTool
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
