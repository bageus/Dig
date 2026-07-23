using System;
using System.Collections.Generic;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        internal Result PrepareResidentsForDirectCommand(
            IReadOnlyList<string> residentIds,
            long tick)
        {
            if (residentIds == null)
            {
                throw new ArgumentNullException(nameof(residentIds));
            }

            JobSystem jobs = _jobRepository.Get();
            InventoryState inventory = _inventoryRepository.Get();
            for (int residentIndex = 0; residentIndex < residentIds.Count; residentIndex++)
            {
                EntityId residentId = EntityId.Parse(residentIds[residentIndex]);
                ClearManualGroupForAgent(residentId);
                JobSnapshot[] assigned = CollectAssignedActiveJobs(jobs, residentId);
                for (int jobIndex = 0; jobIndex < assigned.Length; jobIndex++)
                {
                    JobSnapshot job = assigned[jobIndex];
                    Result released = job.Definition is WorldItemPickupJobDefinition
                        ? CancelPickupForDirectCommand(jobs, inventory, job, tick)
                        : ReleaseDigWorkForDirectCommand(job, tick);
                    if (released.IsFailure)
                    {
                        return released;
                    }

                    _routePlans.Remove(job.Id);
                }
            }

            _inventoryRepository.Save(inventory);
            _jobRepository.Save(jobs);
            return Result.Success();
        }

        private static JobSnapshot[] CollectAssignedActiveJobs(
            JobSystem jobs,
            EntityId residentId)
        {
            List<JobSnapshot> assigned = new List<JobSnapshot>();
            foreach (JobSnapshot job in jobs.GetAll())
            {
                if (!job.IsTerminal
                    && job.AssignedAgentId == residentId
                    && (job.Definition is WorldItemPickupJobDefinition
                        || job.Definition is DigJobDefinition))
                {
                    assigned.Add(job);
                }
            }

            return assigned.ToArray();
        }

        private static Result CancelPickupForDirectCommand(
            JobSystem jobs,
            InventoryState inventory,
            JobSnapshot job,
            long tick)
        {
            Result cancelled = jobs.Cancel(
                job.Id,
                new JobBlockReason(
                    "world_item_pickup_direct_command",
                    "Pickup was cancelled by a direct resident command."),
                tick);
            if (cancelled.IsFailure)
            {
                return cancelled;
            }

            inventory.ReleaseReservations(job.Id, tick);
            return Result.Success();
        }

        private Result ReleaseDigWorkForDirectCommand(JobSnapshot job, long tick)
        {
            if (_releaseAssignment == null)
            {
                return Result.Success();
            }

            return _releaseAssignment.Handle(
                new ReleaseJobAssignmentCommand(job.Id, tick));
        }
    }
}
