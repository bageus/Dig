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
            HashSet<string> productionWorkers =
                new HashSet<string>(StringComparer.Ordinal);
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
                if (job.IsMushroomChop
                    || job.IsBarrelAttack
                    || job.IsProductionWork)
                {
                    nonClimbingWorkers.Add(job.AssignedAgentId!);
                }

                if (job.IsBarrelAttack)
                {
                    barrelWorkers.Add(job.AssignedAgentId!);
                }

                if (job.IsProductionWork)
                {
                    productionWorkers.Add(job.AssignedAgentId!);
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
                    nonClimbingWorkers.Contains(pair.Key),
                    hasFullSupport,
                    tunnelVolume.Contains(current));
                bool barrelAttack = hasToolWork && barrelWorkers.Contains(pair.Key);
                bool productionWork = hasToolWork
                    && productionWorkers.Contains(pair.Key);
                CellId? poseTarget = hasToolWork
                    ? target
                    : climbingWork ? current : (CellId?)null;
                pair.Value.SetWorkTarget(
                    poseTarget,
                    climbingWork,
                    animateToolWork: hasToolWork
                        && !barrelAttack
                        && !productionWork,
                    animateAttackWork: barrelAttack,
                    animateBuildWork: productionWork);
            }
        }

        internal static bool RequiresClimbingWorkPose(
            bool isNonClimbingWork,
            bool hasFullSupport,
            bool isOpenTunnelCell)
        {
            // World support remains authoritative after a job becomes terminal. An
            // unsupported resident in an open tunnel keeps the stationary climbing
            // posture until another route starts or a supported landing is reached.
            return !isNonClimbingWork && !hasFullSupport && isOpenTunnelCell;
        }

        private static bool IsActiveToolWork(JobOverlayViewModel job)
        {
            bool supportedTool = job.PreferredToolKind == JobToolKind.Mining
                || job.IsMushroomChop
                || job.IsBarrelAttack
                || job.IsProductionWork;
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
