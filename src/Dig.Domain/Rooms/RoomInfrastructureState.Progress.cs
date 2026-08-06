using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.Rooms
{

public sealed partial class RoomInfrastructureState
{
    public Result AttachJob(EntityId roomInfrastructureId, EntityId jobId, long tick)
    {
        ValidateTick(tick);
        RoomInfrastructureProjectState? room = Find(roomInfrastructureId);
        if (room == null)
        {
            return Result.Failure(RoomInfrastructureErrors.RoomNotFound);
        }

        long previousVersion = room.Version;
        Result result = room.AttachJob(jobId);
        if (result.IsSuccess && room.Version != previousVersion)
        {
            IncrementVersion();
        }

        return result;
    }

    public Result DetachJob(EntityId roomInfrastructureId, EntityId jobId, long tick)
    {
        ValidateTick(tick);
        RoomInfrastructureProjectState? room = Find(roomInfrastructureId);
        if (room == null)
        {
            return Result.Failure(RoomInfrastructureErrors.RoomNotFound);
        }

        Result result = room.DetachJob(jobId);
        if (result.IsSuccess)
        {
            IncrementVersion();
        }

        return result;
    }

    public Result RecordDelivery(
        EntityId roomInfrastructureId,
        EntityId deliveryJobId,
        ItemId itemId,
        int quantity,
        long tick)
    {
        ValidateTick(tick);
        RoomInfrastructureProjectState? room = Find(roomInfrastructureId);
        if (room == null)
        {
            return Result.Failure(RoomInfrastructureErrors.RoomNotFound);
        }

        RoomImprovementStatus previousStatus = room.Status;
        Result result = room.RecordDelivery(deliveryJobId, itemId, quantity);
        if (result.IsSuccess)
        {
            IncrementVersion();
            Raise(new RoomMaterialDelivered(
                tick,
                roomInfrastructureId,
                itemId,
                quantity));
            if (previousStatus != RoomImprovementStatus.ReadyForWork
                && room.Status == RoomImprovementStatus.ReadyForWork)
            {
                Raise(new RoomUpgradeReadyForWork(tick, roomInfrastructureId));
            }
        }

        return result;
    }

    public Result StartImprovementWork(
        EntityId roomInfrastructureId,
        EntityId workJobId,
        long tick)
    {
        ValidateTick(tick);
        RoomInfrastructureProjectState? room = Find(roomInfrastructureId);
        if (room == null)
        {
            return Result.Failure(RoomInfrastructureErrors.RoomNotFound);
        }

        RoomImprovementStatus previous = room.Status;
        Result result = room.StartImprovementWork(workJobId);
        if (result.IsSuccess && previous != room.Status)
        {
            IncrementVersion();
            Raise(new RoomUpgradeWorkStarted(tick, roomInfrastructureId));
        }

        return result;
    }

    public Result<RoomMaterialCommitResult> CommitMaterialUnit(
        EntityId roomInfrastructureId,
        EntityId workJobId,
        RoomMaterialUnitId unitId,
        long tick)
    {
        ValidateTick(tick);
        RoomInfrastructureProjectState? room = Find(roomInfrastructureId);
        if (room == null)
        {
            return Result<RoomMaterialCommitResult>.Failure(
                RoomInfrastructureErrors.RoomNotFound);
        }

        RoomPurposeKind previousActive = room.ActivePurpose;
        Result<RoomMaterialCommitResult> result = room.CommitMaterialUnit(
            workJobId,
            unitId);
        if (result.IsSuccess && !result.Value.AlreadyCommitted)
        {
            IncrementVersion();
            Raise(new RoomMaterialUnitCommitted(
                tick,
                roomInfrastructureId,
                unitId));
            if (result.Value.ImprovementCompleted)
            {
                Raise(new RoomUpgradeCompleted(
                    tick,
                    roomInfrastructureId,
                    room.ActivePurpose));
                if (previousActive != room.ActivePurpose)
                {
                    Raise(new RoomActivePurposeChanged(
                        tick,
                        roomInfrastructureId,
                        previousActive,
                        room.ActivePurpose));
                }
            }
        }

        return result;
    }

    public Result<RoomUpgradeCancellationResult> CancelUpgradeBeforeWork(
        EntityId roomInfrastructureId,
        string reason,
        long tick)
    {
        ValidateTick(tick);
        RoomInfrastructureProjectState? room = Find(roomInfrastructureId);
        if (room == null)
        {
            return Result<RoomUpgradeCancellationResult>.Failure(
                RoomInfrastructureErrors.RoomNotFound);
        }

        Result<RoomUpgradeCancellationResult> result = room.CancelBeforeWork(reason);
        if (result.IsSuccess)
        {
            IncrementVersion();
            Raise(new RoomUpgradeCancelled(
                tick,
                roomInfrastructureId,
                reason.Trim()));
        }

        return result;
    }
}

}
