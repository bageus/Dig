using System;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;

namespace Dig.Application.Inventory
{

internal static class HaulingResidentSlotClaimReconciler
{
    public static int ReleaseStale(
        InventoryState inventory,
        JobSystem jobs,
        long tick)
    {
        if (inventory is null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        if (jobs is null)
        {
            throw new ArgumentNullException(nameof(jobs));
        }

        EntityId[] staleJobs = inventory.GetResidentSlotClaims()
            .GroupBy(claim => claim.JobId)
            .Where(group => IsStale(jobs.Get(group.Key), group.ToArray()))
            .Select(group => group.Key)
            .OrderBy(jobId => jobId.ToString(), StringComparer.Ordinal)
            .ToArray();
        int released = 0;
        for (int index = 0; index < staleJobs.Length; index++)
        {
            released = checked(released
                + inventory.ReleaseResidentSlotClaims(staleJobs[index], tick));
        }

        return released;
    }

    private static bool IsStale(
        JobSnapshot? job,
        ResidentInventorySlotClaimSnapshot[] claims)
    {
        if (job is null
            || job.IsTerminal
            || job.Definition is not HaulJobDefinition hauling
            || !job.AssignedAgentId.HasValue
            || (job.Status != JobStatus.Claimed && job.Status != JobStatus.InProgress))
        {
            return true;
        }

        EntityId residentId = job.AssignedAgentId.Value;
        return claims.Any(claim => claim.ResidentId != residentId
                || claim.ItemId != hauling.ItemId)
            || claims.Sum(claim => claim.Quantity) != hauling.Quantity;
    }
}

}