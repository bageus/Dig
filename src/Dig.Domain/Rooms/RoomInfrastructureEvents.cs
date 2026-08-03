using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Domain.Rooms
{

public sealed class RoomInfrastructureRegistered : IDomainEvent
{
    public RoomInfrastructureRegistered(
        long tick,
        EntityId roomInfrastructureId,
        string templateInstanceId)
    {
        Tick = tick;
        RoomInfrastructureId = roomInfrastructureId;
        TemplateInstanceId = templateInstanceId;
    }

    public long Tick { get; }
    public EntityId RoomInfrastructureId { get; }
    public string TemplateInstanceId { get; }
}

public sealed class RoomUpgradeOrdered : IDomainEvent
{
    public RoomUpgradeOrdered(long tick, EntityId roomInfrastructureId)
    {
        Tick = tick;
        RoomInfrastructureId = roomInfrastructureId;
    }

    public long Tick { get; }
    public EntityId RoomInfrastructureId { get; }
}

public sealed class RoomTemporaryStockAssigned : IDomainEvent
{
    public RoomTemporaryStockAssigned(
        long tick,
        EntityId roomInfrastructureId,
        CellId stockCell)
    {
        Tick = tick;
        RoomInfrastructureId = roomInfrastructureId;
        StockCell = stockCell;
    }

    public long Tick { get; }
    public EntityId RoomInfrastructureId { get; }
    public CellId StockCell { get; }
}

public sealed class RoomRequestedPurposeChanged : IDomainEvent
{
    public RoomRequestedPurposeChanged(
        long tick,
        EntityId roomInfrastructureId,
        RoomPurposeKind previous,
        RoomPurposeKind current)
    {
        Tick = tick;
        RoomInfrastructureId = roomInfrastructureId;
        Previous = previous;
        Current = current;
    }

    public long Tick { get; }
    public EntityId RoomInfrastructureId { get; }
    public RoomPurposeKind Previous { get; }
    public RoomPurposeKind Current { get; }
}


public sealed class RoomActivePurposeChanged : IDomainEvent
{
    public RoomActivePurposeChanged(
        long tick,
        EntityId roomInfrastructureId,
        RoomPurposeKind previous,
        RoomPurposeKind current)
    {
        Tick = tick;
        RoomInfrastructureId = roomInfrastructureId;
        Previous = previous;
        Current = current;
    }

    public long Tick { get; }
    public EntityId RoomInfrastructureId { get; }
    public RoomPurposeKind Previous { get; }
    public RoomPurposeKind Current { get; }
}

public sealed class RoomMaterialDelivered : IDomainEvent
{
    public RoomMaterialDelivered(
        long tick,
        EntityId roomInfrastructureId,
        ItemId itemId,
        int quantity)
    {
        Tick = tick;
        RoomInfrastructureId = roomInfrastructureId;
        ItemId = itemId;
        Quantity = quantity;
    }

    public long Tick { get; }
    public EntityId RoomInfrastructureId { get; }
    public ItemId ItemId { get; }
    public int Quantity { get; }
}

public sealed class RoomUpgradeReadyForWork : IDomainEvent
{
    public RoomUpgradeReadyForWork(long tick, EntityId roomInfrastructureId)
    {
        Tick = tick;
        RoomInfrastructureId = roomInfrastructureId;
    }

    public long Tick { get; }
    public EntityId RoomInfrastructureId { get; }
}

public sealed class RoomUpgradeCancelled : IDomainEvent
{
    public RoomUpgradeCancelled(
        long tick,
        EntityId roomInfrastructureId,
        string reason)
    {
        Tick = tick;
        RoomInfrastructureId = roomInfrastructureId;
        Reason = reason;
    }

    public long Tick { get; }
    public EntityId RoomInfrastructureId { get; }
    public string Reason { get; }
}

public sealed class RoomUpgradeWorkStarted : IDomainEvent
{
    public RoomUpgradeWorkStarted(long tick, EntityId roomInfrastructureId)
    {
        Tick = tick;
        RoomInfrastructureId = roomInfrastructureId;
    }

    public long Tick { get; }
    public EntityId RoomInfrastructureId { get; }
}

public sealed class RoomMaterialUnitCommitted : IDomainEvent
{
    public RoomMaterialUnitCommitted(
        long tick,
        EntityId roomInfrastructureId,
        RoomMaterialUnitId unitId)
    {
        Tick = tick;
        RoomInfrastructureId = roomInfrastructureId;
        UnitId = unitId;
    }

    public long Tick { get; }
    public EntityId RoomInfrastructureId { get; }
    public RoomMaterialUnitId UnitId { get; }
}

public sealed class RoomUpgradeCompleted : IDomainEvent
{
    public RoomUpgradeCompleted(
        long tick,
        EntityId roomInfrastructureId,
        RoomPurposeKind activePurpose)
    {
        Tick = tick;
        RoomInfrastructureId = roomInfrastructureId;
        ActivePurpose = activePurpose;
    }

    public long Tick { get; }
    public EntityId RoomInfrastructureId { get; }
    public RoomPurposeKind ActivePurpose { get; }
}

}
