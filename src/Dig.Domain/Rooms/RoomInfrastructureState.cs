using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Rooms
{

public static class RoomInfrastructureErrors
{
    public static readonly DomainError RoomAlreadyExists = new DomainError(
        "room.infrastructure.room_already_exists",
        "Room infrastructure is already registered.");
    public static readonly DomainError TemplateInstanceAlreadyRegistered = new DomainError(
        "room.infrastructure.template_instance_already_registered",
        "Template instance already owns room infrastructure.");
    public static readonly DomainError RoomNotFound = new DomainError(
        "room.infrastructure.room_not_found",
        "Room infrastructure was not found.");
    public static readonly DomainError UpgradeAlreadyOrdered = new DomainError(
        "room.infrastructure.upgrade_already_ordered",
        "The room upgrade order count is already one.");
    public static readonly DomainError InvalidStatus = new DomainError(
        "room.infrastructure.invalid_status",
        "The room upgrade operation is not in a valid status for this action.");
    public static readonly DomainError StockCellAlreadyAssigned = new DomainError(
        "room.infrastructure.stock_cell_already_assigned",
        "The room upgrade already has a different temporary stock cell.");
    public static readonly DomainError MaterialNotRequired = new DomainError(
        "room.infrastructure.material_not_required",
        "The material is not part of this room upgrade cost.");
    public static readonly DomainError DeliveryExceedsRequirement = new DomainError(
        "room.infrastructure.delivery_exceeds_requirement",
        "The delivery exceeds the remaining room upgrade requirement.");
    public static readonly DomainError MaterialsIncomplete = new DomainError(
        "room.infrastructure.materials_incomplete",
        "All room upgrade materials must be delivered before work starts.");
    public static readonly DomainError DeliveredMaterialUnavailable = new DomainError(
        "room.infrastructure.delivered_material_unavailable",
        "The requested room material unit is not available in delivered stock.");
    public static readonly DomainError InvalidMaterialUnit = new DomainError(
        "room.infrastructure.invalid_material_unit",
        "The room material unit is outside the required quantity.");
    public static readonly DomainError CancellationLocked = new DomainError(
        "room.infrastructure.cancellation_locked",
        "Room upgrade cancellation is locked after actual improvement work starts.");
    public static readonly DomainError JobNotAttached = new DomainError(
        "room.infrastructure.job_not_attached",
        "The job is not attached to the room upgrade operation.");
    public static readonly DomainError InvalidSnapshot = new DomainError(
        "room.infrastructure.invalid_snapshot",
        "Room infrastructure snapshot is malformed or inconsistent.");
}

public sealed partial class RoomInfrastructureState : AggregateRoot
{
    private readonly Dictionary<EntityId, RoomInfrastructureProjectState> _rooms =
        new Dictionary<EntityId, RoomInfrastructureProjectState>();
    private readonly Dictionary<string, EntityId> _roomsByTemplateInstance =
        new Dictionary<string, EntityId>(StringComparer.Ordinal);

    public long Version { get; private set; }

    public Result RegisterCompletedTemplateRoom(
        EntityId roomInfrastructureId,
        string templateInstanceId,
        RoomTemplateKind templateKind,
        long tick)
    {
        ValidateTick(tick);
        if (_rooms.ContainsKey(roomInfrastructureId))
        {
            return Result.Failure(RoomInfrastructureErrors.RoomAlreadyExists);
        }

        if (string.IsNullOrWhiteSpace(templateInstanceId))
        {
            throw new ArgumentException("Template instance id is required.", nameof(templateInstanceId));
        }

        string normalized = templateInstanceId.Trim();
        if (_roomsByTemplateInstance.ContainsKey(normalized))
        {
            return Result.Failure(RoomInfrastructureErrors.TemplateInstanceAlreadyRegistered);
        }

        RoomInfrastructureProjectState room = new RoomInfrastructureProjectState(
            roomInfrastructureId,
            normalized,
            templateKind);
        _rooms.Add(roomInfrastructureId, room);
        _roomsByTemplateInstance.Add(normalized, roomInfrastructureId);
        IncrementVersion();
        Raise(new RoomInfrastructureRegistered(
            tick,
            roomInfrastructureId,
            normalized));
        return Result.Success();
    }

    public Result OrderUpgrade(
        EntityId roomInfrastructureId,
        RoomPurposeKind requestedPurpose,
        long tick)
    {
        ValidateTick(tick);
        RoomInfrastructureProjectState? room = Find(roomInfrastructureId);
        if (room == null)
        {
            return Result.Failure(RoomInfrastructureErrors.RoomNotFound);
        }

        Result result = room.Order(requestedPurpose);
        if (result.IsSuccess)
        {
            IncrementVersion();
            Raise(new RoomUpgradeOrdered(tick, roomInfrastructureId));
            if (requestedPurpose != RoomPurposeKind.None)
            {
                Raise(new RoomRequestedPurposeChanged(
                    tick,
                    roomInfrastructureId,
                    RoomPurposeKind.None,
                    requestedPurpose));
            }
        }

        return result;
    }

    public Result AssignTemporaryStockCell(
        EntityId roomInfrastructureId,
        CellId cell,
        long tick)
    {
        ValidateTick(tick);
        RoomInfrastructureProjectState? room = Find(roomInfrastructureId);
        if (room == null)
        {
            return Result.Failure(RoomInfrastructureErrors.RoomNotFound);
        }

        CellId? previous = room.TemporaryStockCell;
        Result result = room.AssignTemporaryStockCell(cell);
        if (result.IsSuccess && !previous.HasValue)
        {
            IncrementVersion();
            Raise(new RoomTemporaryStockAssigned(tick, roomInfrastructureId, cell));
        }

        return result;
    }

    public Result ChangeRequestedPurpose(
        EntityId roomInfrastructureId,
        RoomPurposeKind purpose,
        long tick)
    {
        ValidateTick(tick);
        RoomInfrastructureProjectState? room = Find(roomInfrastructureId);
        if (room == null)
        {
            return Result.Failure(RoomInfrastructureErrors.RoomNotFound);
        }

        RoomPurposeKind previousRequested = room.RequestedPurpose;
        RoomPurposeKind previousActive = room.ActivePurpose;
        Result result = room.ChangeRequestedPurpose(purpose);
        if (result.IsSuccess && previousRequested != purpose)
        {
            IncrementVersion();
            Raise(new RoomRequestedPurposeChanged(
                tick,
                roomInfrastructureId,
                previousRequested,
                purpose));
            if (previousActive != room.ActivePurpose)
            {
                Raise(new RoomActivePurposeChanged(
                    tick,
                    roomInfrastructureId,
                    previousActive,
                    room.ActivePurpose));
            }
        }

        return result;
    }
}

}
