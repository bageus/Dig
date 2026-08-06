using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Rooms;
using Dig.Domain.World;

namespace Dig.Application.Rooms
{

public interface IRoomInfrastructureRepository
{
    RoomInfrastructureState Get();
    void Save(RoomInfrastructureState state);
}

public sealed class SynchronizeCompletedRoomInfrastructureCommand
    : ICommand<Result<RoomInfrastructureSynchronizationResult>>
{
    public SynchronizeCompletedRoomInfrastructureCommand(
        IEnumerable<CompletedRoomInfrastructureProvenance> rooms,
        long tick)
    {
        Rooms = new ReadOnlyCollection<CompletedRoomInfrastructureProvenance>(
            (rooms ?? throw new ArgumentNullException(nameof(rooms)))
                .OrderBy(value => value.TemplateInstanceId, StringComparer.Ordinal)
                .ToArray());
        Tick = tick;
    }

    public IReadOnlyList<CompletedRoomInfrastructureProvenance> Rooms { get; }
    public long Tick { get; }
}

public sealed class RoomInfrastructureSynchronizationResult
{
    public RoomInfrastructureSynchronizationResult(int added, int retained)
    {
        if (added < 0 || retained < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(added));
        }

        Added = added;
        Retained = retained;
    }

    public int Added { get; }
    public int Retained { get; }
}

public sealed class OrderRoomUpgradeCommand : ICommand<Result>
{
    public OrderRoomUpgradeCommand(
        EntityId roomInfrastructureId,
        RoomPurposeKind requestedPurpose,
        long tick)
    {
        RoomInfrastructureId = roomInfrastructureId;
        RequestedPurpose = requestedPurpose;
        Tick = tick;
    }

    public EntityId RoomInfrastructureId { get; }
    public RoomPurposeKind RequestedPurpose { get; }
    public long Tick { get; }
}

public sealed class SynchronizeRoomTemporaryStockCellCommand
    : ICommand<Result<RoomTemporaryStockCellPlan>>
{
    public SynchronizeRoomTemporaryStockCellCommand(
        CompletedRoomInfrastructureProvenance room,
        WorldSnapshot world,
        IEnumerable<CellId> reachableCells,
        IEnumerable<CellId> occupiedCells,
        long tick)
    {
        Room = room ?? throw new ArgumentNullException(nameof(room));
        World = world ?? throw new ArgumentNullException(nameof(world));
        ReachableCells = Copy(reachableCells, nameof(reachableCells));
        OccupiedCells = Copy(occupiedCells, nameof(occupiedCells));
        Tick = tick;
    }

    public CompletedRoomInfrastructureProvenance Room { get; }
    public WorldSnapshot World { get; }
    public IReadOnlyList<CellId> ReachableCells { get; }
    public IReadOnlyList<CellId> OccupiedCells { get; }
    public long Tick { get; }

    private static IReadOnlyList<CellId> Copy(
        IEnumerable<CellId> cells,
        string parameterName)
    {
        return new ReadOnlyCollection<CellId>(
            (cells ?? throw new ArgumentNullException(parameterName))
                .Distinct()
                .OrderBy(value => value)
                .ToArray());
    }
}

public sealed class ChangeRoomRequestedPurposeCommand : ICommand<Result>
{
    public ChangeRoomRequestedPurposeCommand(
        EntityId roomInfrastructureId,
        RoomPurposeKind purpose,
        long tick)
    {
        RoomInfrastructureId = roomInfrastructureId;
        Purpose = purpose;
        Tick = tick;
    }

    public EntityId RoomInfrastructureId { get; }
    public RoomPurposeKind Purpose { get; }
    public long Tick { get; }
}

public sealed class AttachRoomUpgradeJobCommand : ICommand<Result>
{
    public AttachRoomUpgradeJobCommand(
        EntityId roomInfrastructureId,
        EntityId jobId,
        long tick)
    {
        RoomInfrastructureId = roomInfrastructureId;
        JobId = jobId;
        Tick = tick;
    }

    public EntityId RoomInfrastructureId { get; }
    public EntityId JobId { get; }
    public long Tick { get; }
}

public sealed class RecordRoomMaterialDeliveryCommand : ICommand<Result>
{
    public RecordRoomMaterialDeliveryCommand(
        EntityId roomInfrastructureId,
        EntityId deliveryJobId,
        ItemId itemId,
        int quantity,
        long tick)
    {
        RoomInfrastructureId = roomInfrastructureId;
        DeliveryJobId = deliveryJobId;
        ItemId = itemId;
        Quantity = quantity;
        Tick = tick;
    }

    public EntityId RoomInfrastructureId { get; }
    public EntityId DeliveryJobId { get; }
    public ItemId ItemId { get; }
    public int Quantity { get; }
    public long Tick { get; }
}

public sealed class StartRoomImprovementWorkCommand : ICommand<Result>
{
    public StartRoomImprovementWorkCommand(
        EntityId roomInfrastructureId,
        EntityId workJobId,
        long tick)
    {
        RoomInfrastructureId = roomInfrastructureId;
        WorkJobId = workJobId;
        Tick = tick;
    }

    public EntityId RoomInfrastructureId { get; }
    public EntityId WorkJobId { get; }
    public long Tick { get; }
}

public sealed class CommitRoomMaterialUnitCommand
    : ICommand<Result<RoomMaterialCommitResult>>
{
    public CommitRoomMaterialUnitCommand(
        EntityId roomInfrastructureId,
        EntityId workJobId,
        RoomMaterialUnitId unitId,
        long tick)
    {
        RoomInfrastructureId = roomInfrastructureId;
        WorkJobId = workJobId;
        UnitId = unitId;
        Tick = tick;
    }

    public EntityId RoomInfrastructureId { get; }
    public EntityId WorkJobId { get; }
    public RoomMaterialUnitId UnitId { get; }
    public long Tick { get; }
}

public sealed class CancelRoomUpgradeBeforeWorkCommand
    : ICommand<Result<RoomUpgradeCancellationResult>>
{
    public CancelRoomUpgradeBeforeWorkCommand(
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

public sealed class GetRoomInfrastructureQuery : IQuery<RoomInfrastructureSnapshot>
{
}

public static class RoomInfrastructureApplicationErrors
{
    public static readonly DomainError ProvenanceIdentityConflict = new DomainError(
        "room.infrastructure.application.provenance_identity_conflict",
        "Completed room provenance conflicts with registered room infrastructure identity.");
}

}
