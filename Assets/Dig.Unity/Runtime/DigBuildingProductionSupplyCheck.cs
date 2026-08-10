using System;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private Result AdvanceSupplyWorkstationCheck(
        JobSnapshot job,
        BuildingSupplyJobDefinition supply,
        AgentViewModel worker,
        long tick,
        out JobSnapshot current)
    {
        current = job;
        if (job.Status == JobStatus.Claimed)
        {
            if (!At(worker, supply.WorkPosition)
                || !IsAtPreciseWorkPose(job, worker))
            {
                return Result.Success();
            }

            Result started = _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
            if (started.IsFailure)
            {
                return started;
            }

            current = _jobRepository.Get().Get(job.Id)
                ?? throw new InvalidOperationException("Started supply job disappeared.");
        }

        if (current.Status == JobStatus.InProgress
            && current.Stage == JobStageKind.TravelToTarget)
        {
            if (!At(worker, supply.WorkPosition)
                || !IsAtPreciseWorkPose(current, worker))
            {
                return Result.Success();
            }

            Result checkedStock = _advanceHandler.Handle(
                new AdvanceJobCommand(current.Id, tick));
            if (checkedStock.IsFailure)
            {
                return checkedStock;
            }

            current = _jobRepository.Get().Get(current.Id)
                ?? throw new InvalidOperationException("Checked supply job disappeared.");
        }

        return Result.Success();
    }

    private ItemReservationAllocation? FindPendingSupplyAllocation(
        EntityId jobId,
        BuildingSupplyJobDefinition supply)
    {
        InventoryState inventory = _buildingInventoryRepository!.Get();
        return supply.Allocations
            .Where(value =>
            {
                ItemStackSnapshot? stack = inventory.GetStack(value.StackId);
                return stack?.Location.Kind == ItemLocationKind.World
                    && stack.Reservations.Any(reservation =>
                        reservation.JobId == jobId
                        && reservation.Quantity > 0);
            })
            .OrderBy(value => value.StackId.ToString(), StringComparer.Ordinal)
            .Select(value => (ItemReservationAllocation?)value)
            .FirstOrDefault();
    }
}

}
