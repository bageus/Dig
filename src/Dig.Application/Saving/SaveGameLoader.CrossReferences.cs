using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameLoader
{
    private static Result ValidateCrossReferences(
        InventoryState inventory,
        JobSystem jobs,
        ProductionState production)
    {
        InventorySnapshot snapshot = inventory.CreateSnapshot();
        foreach (ItemStackSnapshot stack in snapshot.Stacks)
        {
            foreach (ItemQuantityReservationSnapshot reservation in stack.Reservations)
            {
                JobSnapshot? job = jobs.Get(reservation.JobId);
                ProductionOrderSnapshot? order = production.Get(reservation.JobId);
                if ((job is null || job.IsTerminal)
                    && (order is null || order.IsTerminal))
                {
                    return Result.Failure(SaveErrors.InvalidDocument);
                }
            }
        }

        foreach (IGrouping<EntityId, ResidentInventorySlotClaimSnapshot> group
            in snapshot.ResidentSlotClaims.GroupBy(claim => claim.JobId))
        {
            JobSnapshot? job = jobs.Get(group.Key);
            ResidentInventorySlotClaimSnapshot[] claims = group.ToArray();
            if (job is null
                || job.IsTerminal
                || !job.AssignedAgentId.HasValue
                || (job.Status != JobStatus.Claimed
                    && job.Status != JobStatus.InProgress)
                || claims.Any(claim => claim.ResidentId != job.AssignedAgentId.Value)
                || !ClaimsMatchJob(job.Definition, claims))
            {
                return Result.Failure(SaveErrors.InvalidDocument);
            }
        }

        return Result.Success();
    }

    private static bool ClaimsMatchJob(
        JobDefinition definition,
        IReadOnlyCollection<ResidentInventorySlotClaimSnapshot> claims)
    {
        if (definition is HaulJobDefinition hauling)
        {
            return claims.All(claim => claim.ItemId == hauling.ItemId)
                && claims.Sum(claim => claim.Quantity) == hauling.Quantity;
        }

        if (definition is BuildingSupplyJobDefinition supply)
        {
            Dictionary<ItemId, int> expected = supply.Allocations
                .GroupBy(value => value.ItemId)
                .ToDictionary(group => group.Key, group => group.Sum(value => value.Quantity));
            Dictionary<ItemId, int> actual = claims
                .GroupBy(value => value.ItemId)
                .ToDictionary(group => group.Key, group => group.Sum(value => value.Quantity));
            return expected.Count == actual.Count
                && expected.All(value => actual.TryGetValue(value.Key, out int quantity)
                    && quantity == value.Value);
        }

        return false;
    }


}

}
