using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Rooms;
using Dig.Domain.World;

namespace Dig.Application.Rooms
{

public interface IRoomUpgradeJobIdSource
{
    EntityId NextJobId();
}

public sealed class SynchronizeRoomUpgradeJobsCommand
    : ICommand<Result<RoomUpgradeJobSynchronizationReport>>
{
    public SynchronizeRoomUpgradeJobsCommand(
        IEnumerable<CellId> revealedCells,
        IEnumerable<CellId> reachableCells,
        int priority,
        int maximumDeliveryJobs,
        long tick)
    {
        if (maximumDeliveryJobs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumDeliveryJobs));
        }

        RevealedCells = Copy(revealedCells, nameof(revealedCells));
        ReachableCells = Copy(reachableCells, nameof(reachableCells));
        Priority = priority;
        MaximumDeliveryJobs = maximumDeliveryJobs;
        Tick = tick;
    }

    public IReadOnlyList<CellId> RevealedCells { get; }
    public IReadOnlyList<CellId> ReachableCells { get; }
    public int Priority { get; }
    public int MaximumDeliveryJobs { get; }
    public long Tick { get; }

    private static IReadOnlyList<CellId> Copy(
        IEnumerable<CellId> values,
        string parameterName)
    {
        return new ReadOnlyCollection<CellId>(
            (values ?? throw new ArgumentNullException(parameterName))
                .Distinct()
                .OrderBy(value => value)
                .ToArray());
    }
}

public sealed class RoomUpgradeDeliveryJobPlan
{
    public RoomUpgradeDeliveryJobPlan(
        EntityId roomInfrastructureId,
        EntityId jobId,
        EntityId sourceStackId,
        int quantity)
    {
        RoomInfrastructureId = roomInfrastructureId;
        JobId = jobId;
        SourceStackId = sourceStackId;
        Quantity = quantity;
    }

    public EntityId RoomInfrastructureId { get; }
    public EntityId JobId { get; }
    public EntityId SourceStackId { get; }
    public int Quantity { get; }
}

public sealed class RoomUpgradeJobSynchronizationReport
{
    public RoomUpgradeJobSynchronizationReport(
        IEnumerable<EntityId> workJobsCreated,
        IEnumerable<RoomUpgradeDeliveryJobPlan> deliveriesCreated)
    {
        WorkJobsCreated = new ReadOnlyCollection<EntityId>(
            workJobsCreated
                .OrderBy(value => value.ToString(), StringComparer.Ordinal)
                .ToArray());
        DeliveriesCreated = new ReadOnlyCollection<RoomUpgradeDeliveryJobPlan>(
            deliveriesCreated
                .OrderBy(value => value.JobId.ToString(), StringComparer.Ordinal)
                .ToArray());
    }

    public IReadOnlyList<EntityId> WorkJobsCreated { get; }
    public IReadOnlyList<RoomUpgradeDeliveryJobPlan> DeliveriesCreated { get; }
}

public sealed class CompleteRoomUpgradeDeliveryCommand : ICommand<Result>
{
    public CompleteRoomUpgradeDeliveryCommand(
        EntityId jobId,
        EntityId depositedStackId,
        long tick)
    {
        JobId = jobId;
        DepositedStackId = depositedStackId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId DepositedStackId { get; }
    public long Tick { get; }
}

public sealed class CommitRoomUpgradeWorkIntervalCommand
    : ICommand<Result<RoomMaterialCommitResult>>
{
    public CommitRoomUpgradeWorkIntervalCommand(
        EntityId jobId,
        RoomMaterialUnitId unitId,
        long tick)
    {
        JobId = jobId;
        UnitId = unitId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public RoomMaterialUnitId UnitId { get; }
    public long Tick { get; }
}

public sealed class CompleteRoomUpgradeWorkCommand : ICommand<Result>
{
    public CompleteRoomUpgradeWorkCommand(EntityId jobId, long tick)
    {
        JobId = jobId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public long Tick { get; }
}

public sealed class CancelRoomUpgradeOperationCommand
    : ICommand<Result<RoomUpgradeCancellationResult>>
{
    public CancelRoomUpgradeOperationCommand(
        EntityId roomInfrastructureId,
        string reason,
        long tick)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Cancellation reason is required.", nameof(reason));
        }

        RoomInfrastructureId = roomInfrastructureId;
        Reason = reason.Trim();
        Tick = tick;
    }

    public EntityId RoomInfrastructureId { get; }
    public string Reason { get; }
    public long Tick { get; }
}

public static class RoomUpgradeExecutionErrors
{
    public static readonly DomainError JobMismatch = new DomainError(
        "room.upgrade.execution.job_mismatch",
        "The job does not belong to the room upgrade operation.");
    public static readonly DomainError WorkJobMissing = new DomainError(
        "room.upgrade.execution.work_job_missing",
        "The room upgrade operation has no active work job.");
    public static readonly DomainError InvalidStage = new DomainError(
        "room.upgrade.execution.invalid_stage",
        "The room upgrade job is not in the required execution stage.");
    public static readonly DomainError SourceUnavailable = new DomainError(
        "room.upgrade.execution.source_unavailable",
        "No revealed reachable unreserved source can satisfy the room material demand.");
    public static readonly DomainError StockReservationInvalid = new DomainError(
        "room.upgrade.execution.stock_reservation_invalid",
        "The temporary room stock reservation does not match the authoritative ledger.");
}

}
