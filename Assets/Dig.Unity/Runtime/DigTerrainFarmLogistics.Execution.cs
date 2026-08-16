using Dig.Application.Farming;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private Result AdvanceFarmDeliveryAtTarget(
        JobSnapshot job,
        AgentViewModel agent,
        long tick)
    {
        CellId? destination = ResolveFarmLogisticsDestination(job);
        if (!destination.HasValue
            || agent.CellX != destination.Value.X
            || agent.CellY != destination.Value.Y
            || agent.CellZ != destination.Value.Z)
        {
            return Result.Success();
        }

        if (job.Status == JobStatus.Claimed)
        {
            return _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
        }
        if (job.Stage == JobStageKind.AcquireItem)
        {
            return _farmAcquisition!.Handle(new AcquireHaulingItemCommand(
                job.Id, NextFarmRuntimeId("stack"), tick));
        }
        if (job.Stage == JobStageKind.TravelToDestination)
        {
            return _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
        }
        if (job.Stage != JobStageKind.DepositItem) return Result.Success();

        _farmLogisticsReservations.TryGet(
            job.Id,
            out FarmLogisticsReservation reservation);
        Result completed = reservation.Direction == FarmLogisticsDirection.Outgoing
            ? _farmOutputCompletion!.Handle(new CompleteFarmOutputCommand(
                job.Id, NextFarmRuntimeId("stack"), tick))
            : _farmDeliveryCompletion!.Handle(new CompleteFarmDeliveryCommand(
                job.Id, NextFarmRuntimeId("stack"), tick));
        if (completed.IsSuccess) _routePlans.Remove(job.Id);
        return completed;
    }
}

}
