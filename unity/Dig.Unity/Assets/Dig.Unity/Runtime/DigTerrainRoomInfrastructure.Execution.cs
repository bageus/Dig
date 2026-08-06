using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Rooms;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Rooms;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private Result AdvanceRoomUpgradeAtTarget(
        JobSnapshot job,
        AgentViewModel agent,
        long tick)
    {
        CellId? destination = ResolveRoomUpgradeDestination(job);
        if (!destination.HasValue
            || agent.CellX != destination.Value.X
            || agent.CellY != destination.Value.Y
            || agent.CellZ != destination.Value.Z)
        {
            return Result.Success();
        }

        if (job.Definition is HaulJobDefinition)
        {
            return AdvanceRoomDeliveryAtTarget(job, tick);
        }

        if (job.Definition is not RoomUpgradeWorkJobDefinition work)
        {
            return Result.Failure(RoomUpgradeExecutionErrors.JobMismatch);
        }

        if (job.Status == JobStatus.Claimed
            || job.Stage == JobStageKind.TravelToTarget)
        {
            return _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
        }

        if (job.Stage == JobStageKind.PerformWork)
        {
            RoomMaterialUnitId? next =
                _roomInfrastructure!.Get().GetNextMaterialUnit(
                    work.RoomInfrastructureId);
            if (!next.HasValue)
            {
                return Result.Failure(
                    RoomInfrastructureErrors.InvalidMaterialUnit);
            }

            Result<RoomMaterialCommitResult> committed =
                _roomWorkInterval!.Handle(
                    new CommitRoomUpgradeWorkIntervalCommand(
                        job.Id,
                        next.Value,
                        tick));
            return committed.IsSuccess
                ? Result.Success()
                : Result.Failure(committed.Error!);
        }

        if (job.Stage == JobStageKind.Finalize)
        {
            Result completed = _roomWorkCompletion!.Handle(
                new CompleteRoomUpgradeWorkCommand(job.Id, tick));
            if (completed.IsSuccess)
            {
                _routePlans.Remove(job.Id);
            }

            return completed;
        }

        return Result.Success();
    }

    private Result AdvanceRoomDeliveryAtTarget(JobSnapshot job, long tick)
    {
        if (job.Status == JobStatus.Claimed)
        {
            return _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
        }

        if (job.Stage == JobStageKind.AcquireItem)
        {
            return _haulingAcquisition!.Handle(
                new AcquireHaulingItemCommand(
                    job.Id,
                    NextRoomTransitStackId(),
                    tick));
        }

        if (job.Stage == JobStageKind.TravelToDestination)
        {
            return _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
        }

        if (job.Stage != JobStageKind.DepositItem)
        {
            return Result.Success();
        }

        Result completed = _roomDeliveryCompletion!.Handle(
            new CompleteRoomUpgradeDeliveryCommand(
                job.Id,
                NextRoomTransitStackId(),
                tick));
        if (completed.IsSuccess)
        {
            _routePlans.Remove(job.Id);
        }

        return completed;
    }
}

}
