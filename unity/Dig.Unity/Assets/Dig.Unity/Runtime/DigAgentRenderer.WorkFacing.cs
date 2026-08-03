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
            Dictionary<string, ResidentWorkToolVisualKind> workTools =
                new Dictionary<string, ResidentWorkToolVisualKind>(StringComparer.Ordinal);
            HashSet<string> nonClimbingWorkers = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> attackWorkers = new HashSet<string>(StringComparer.Ordinal);
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

                string residentId = job.AssignedAgentId!;
                workTargets.Add(
                    residentId,
                    new CellId(
                        job.TargetX!.Value,
                        job.TargetY!.Value,
                        job.TargetZ!.Value));
                workTools.Add(residentId, job.WorkToolVisualKind);
                if (job.IsMushroomChop
                    || job.IsBarrelAttack
                    || job.IsProductionWork
                    || job.WorkToolVisualKind == ResidentWorkToolVisualKind.Hammer)
                {
                    nonClimbingWorkers.Add(residentId);
                }

                if (job.IsBarrelAttack)
                {
                    attackWorkers.Add(residentId);
                }

                if (job.IsProductionWork)
                {
                    productionWorkers.Add(residentId);
                }
            }

            Dictionary<CellId, CellSnapshot> worldCells = world.Chunks
                .SelectMany(chunk => chunk.Cells)
                .ToDictionary(cell => cell.Id);
            foreach (KeyValuePair<string, DigAgentVisual> pair in _agents)
            {
                bool hasToolWork = workTargets.TryGetValue(pair.Key, out CellId target);
                ResidentWorkToolVisualKind workTool = hasToolWork
                    ? workTools[pair.Key]
                    : ResidentWorkToolVisualKind.None;
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
                bool attackWork = hasToolWork && attackWorkers.Contains(pair.Key);
                bool productionWork = hasToolWork
                    && productionWorkers.Contains(pair.Key);
                CellId? poseTarget = hasToolWork
                    ? target
                    : climbingWork ? current : (CellId?)null;
                pair.Value.SetWorkTarget(
                    poseTarget,
                    climbingWork,
                    workTool,
                    animateToolWork: hasToolWork
                        && !attackWork
                        && !productionWork
                        && workTool != ResidentWorkToolVisualKind.Hammer,
                    animateAttackWork: attackWork,
                    animateBuildWork: productionWork
                        || workTool == ResidentWorkToolVisualKind.Hammer);
            }
        }

        internal static bool RequiresClimbingWorkPose(
            bool isNonClimbingWork,
            bool hasFullSupport,
            bool isOpenTunnelCell)
        {
            return !isNonClimbingWork && !hasFullSupport && isOpenTunnelCell;
        }

        private static bool IsActiveToolWork(JobOverlayViewModel job)
        {
            bool supportedTool = job.WorkToolVisualKind != ResidentWorkToolVisualKind.None
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
