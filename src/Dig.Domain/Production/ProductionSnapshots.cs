using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.Production
{

public enum ProductionOrderStatus
{
    Queued = 0,
    InputsReserved = 1,
    InProgress = 2,
    ReadyToComplete = 3,
    Completed = 4,
    Cancelled = 5,
    Failed = 6,
}

public static class ProductionErrors
{
    public static readonly DomainError OrderAlreadyExists = new DomainError(
        "production.order_already_exists",
        "A production order with the same id already exists.");

    public static readonly DomainError OrderNotFound = new DomainError(
        "production.order_not_found",
        "The requested production order does not exist.");

    public static readonly DomainError InvalidStatus = new DomainError(
        "production.invalid_status",
        "The production order cannot perform that transition from its current status.");

    public static readonly DomainError QueueBlocked = new DomainError(
        "production.queue_blocked",
        "Another earlier or active order owns this workstation queue.");

    public static readonly DomainError TechnologyLocked = new DomainError(
        "production.technology_locked",
        "The recipe's required technology is not unlocked.");

    public static readonly DomainError WorkstationMismatch = new DomainError(
        "production.workstation_mismatch",
        "The selected completed building is not the recipe workstation.");

    public static readonly DomainError ToolUnavailable = new DomainError(
        "production.tool_unavailable",
        "The recipe's required tool is not available at the workstation.");

    public static readonly DomainError WorkPositionUnavailable = new DomainError(
        "production.work_position_unavailable",
        "The workstation work position is not currently reachable.");

    public static readonly DomainError EnergyUnavailable = new DomainError(
        "production.energy_unavailable",
        "The workstation cannot receive the recipe's required energy.");

    public static readonly DomainError OutputIdsMismatch = new DomainError(
        "production.output_ids_mismatch",
        "Output stack ids must match the recipe outputs exactly.");
}

public sealed class ProductionOrderSnapshot
{
    public ProductionOrderSnapshot(
        EntityId id,
        RecipeDefinition recipe,
        EntityId buildingId,
        long sequence,
        ProductionOrderStatus status,
        int completedWork,
        long version,
        IReadOnlyCollection<ItemReservationAllocation> inputAllocations,
        string? reason)
    {
        Id = id;
        Recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));
        BuildingId = buildingId;
        Sequence = sequence;
        Status = status;
        CompletedWork = completedWork;
        Version = version;
        InputAllocations = new ReadOnlyCollection<ItemReservationAllocation>(
            inputAllocations.OrderBy(value => value.StackId.ToString(), StringComparer.Ordinal)
                .ToArray());
        Reason = reason;
    }

    public EntityId Id { get; }

    public RecipeDefinition Recipe { get; }

    public EntityId BuildingId { get; }

    public long Sequence { get; }

    public ProductionOrderStatus Status { get; }

    public int CompletedWork { get; }

    public long Version { get; }

    public IReadOnlyList<ItemReservationAllocation> InputAllocations { get; }

    public string? Reason { get; }

    public bool IsTerminal => Status is ProductionOrderStatus.Completed
        or ProductionOrderStatus.Cancelled
        or ProductionOrderStatus.Failed;
}

public sealed class ProductionOrderStatusChanged : IDomainEvent
{
    public ProductionOrderStatusChanged(
        long tick,
        EntityId orderId,
        EntityId buildingId,
        ProductionOrderStatus previous,
        ProductionOrderStatus current,
        string? reason)
    {
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        if (orderId.IsEmpty || buildingId.IsEmpty)
            throw new ArgumentException("Order and building ids are required.");
        if (!Enum.IsDefined(typeof(ProductionOrderStatus), previous)
            || !Enum.IsDefined(typeof(ProductionOrderStatus), current))
            throw new ArgumentOutOfRangeException(nameof(current));
        Tick = tick;
        OrderId = orderId;
        BuildingId = buildingId;
        Previous = previous;
        Current = current;
        Reason = reason;
    }

    public long Tick { get; }

    public EntityId OrderId { get; }

    public EntityId BuildingId { get; }

    public ProductionOrderStatus Previous { get; }

    public ProductionOrderStatus Current { get; }

    public string? Reason { get; }
}

public sealed class ProductionWorkApplied : IDomainEvent
{
    public ProductionWorkApplied(
        long tick,
        EntityId orderId,
        EntityId buildingId,
        int effectiveWork,
        int completedWork,
        int requiredWork)
    {
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        if (orderId.IsEmpty || buildingId.IsEmpty)
            throw new ArgumentException("Order and building ids are required.");
        if (effectiveWork <= 0 || completedWork <= 0
            || requiredWork <= 0 || completedWork > requiredWork)
            throw new ArgumentOutOfRangeException(nameof(effectiveWork));
        Tick = tick;
        OrderId = orderId;
        BuildingId = buildingId;
        EffectiveWork = effectiveWork;
        CompletedWork = completedWork;
        RequiredWork = requiredWork;
    }

    public long Tick { get; }

    public EntityId OrderId { get; }

    public EntityId BuildingId { get; }

    public int EffectiveWork { get; }

    public int CompletedWork { get; }

    public int RequiredWork { get; }
}
}
