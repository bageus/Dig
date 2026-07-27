using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Domain.WorldObjects
{

public sealed class BarrelCreated : IDomainEvent
{
    public BarrelCreated(long tick, EntityId barrelId, CellId cell, ItemId contentsItemId)
    {
        Tick = tick;
        BarrelId = barrelId;
        Cell = cell;
        ContentsItemId = contentsItemId;
    }

    public long Tick { get; }
    public EntityId BarrelId { get; }
    public CellId Cell { get; }
    public ItemId ContentsItemId { get; }
}

public sealed class BarrelSupportLost : IDomainEvent
{
    public BarrelSupportLost(long tick, EntityId barrelId, CellId sourceCell, CellId landingCell)
    {
        Tick = tick;
        BarrelId = barrelId;
        SourceCell = sourceCell;
        LandingCell = landingCell;
    }

    public long Tick { get; }
    public EntityId BarrelId { get; }
    public CellId SourceCell { get; }
    public CellId LandingCell { get; }
}

public sealed class BarrelLanded : IDomainEvent
{
    public BarrelLanded(long tick, EntityId barrelId, CellId sourceCell, CellId landingCell)
    {
        Tick = tick;
        BarrelId = barrelId;
        SourceCell = sourceCell;
        LandingCell = landingCell;
    }

    public long Tick { get; }
    public EntityId BarrelId { get; }
    public CellId SourceCell { get; }
    public CellId LandingCell { get; }
}

public sealed class BarrelDestroyed : IDomainEvent
{
    public BarrelDestroyed(
        long tick,
        EntityId barrelId,
        EntityId jobId,
        EntityId workerId,
        CellId cell,
        ItemId contentsItemId,
        long contentsGeneration)
    {
        Tick = tick;
        BarrelId = barrelId;
        JobId = jobId;
        WorkerId = workerId;
        Cell = cell;
        ContentsItemId = contentsItemId;
        ContentsGeneration = contentsGeneration;
    }

    public long Tick { get; }
    public EntityId BarrelId { get; }
    public EntityId JobId { get; }
    public EntityId WorkerId { get; }
    public CellId Cell { get; }
    public ItemId ContentsItemId { get; }
    public long ContentsGeneration { get; }
}

public sealed class BarrelContentsMaterialized : IDomainEvent
{
    public BarrelContentsMaterialized(
        long tick,
        EntityId barrelId,
        EntityId outputUnitId,
        ItemId itemId,
        CellId cell,
        long contentsGeneration)
    {
        Tick = tick;
        BarrelId = barrelId;
        OutputUnitId = outputUnitId;
        ItemId = itemId;
        Cell = cell;
        ContentsGeneration = contentsGeneration;
    }

    public long Tick { get; }
    public EntityId BarrelId { get; }
    public EntityId OutputUnitId { get; }
    public ItemId ItemId { get; }
    public CellId Cell { get; }
    public long ContentsGeneration { get; }
}

}