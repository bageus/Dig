using System;
using System.Collections.Generic;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Production;
using Dig.Domain.World;

namespace Dig.Application.Production
{

public interface IBuildingSupplyRepository
{
    BuildingSupplyState Get();
    void Save(BuildingSupplyState supply);
}

public sealed class CreateBuildingSupplyJobCommand : ICommand<Result>
{
    public CreateBuildingSupplyJobCommand(
        EntityId jobId,
        EntityId buildingId,
        EntityId residentId,
        IReadOnlyCollection<CellId> revealedCells,
        IReadOnlyCollection<CellId> reachableCells,
        IReadOnlyCollection<EntityId> transitStackIds,
        IReadOnlyCollection<EntityId> depositStackIds,
        int priority,
        long tick)
    {
        JobId = jobId;
        BuildingId = buildingId;
        ResidentId = residentId;
        RevealedCells = revealedCells ?? throw new ArgumentNullException(nameof(revealedCells));
        ReachableCells = reachableCells ?? throw new ArgumentNullException(nameof(reachableCells));
        TransitStackIds = transitStackIds
            ?? throw new ArgumentNullException(nameof(transitStackIds));
        DepositStackIds = depositStackIds
            ?? throw new ArgumentNullException(nameof(depositStackIds));
        Priority = priority;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId BuildingId { get; }
    public EntityId ResidentId { get; }
    public IReadOnlyCollection<CellId> RevealedCells { get; }
    public IReadOnlyCollection<CellId> ReachableCells { get; }
    public IReadOnlyCollection<EntityId> TransitStackIds { get; }
    public IReadOnlyCollection<EntityId> DepositStackIds { get; }
    public int Priority { get; }
    public long Tick { get; }
}

public sealed class CreateDeferredBuildingSupplyJobCommand : ICommand<Result>
{
    public CreateDeferredBuildingSupplyJobCommand(
        EntityId jobId,
        EntityId buildingId,
        IReadOnlyCollection<ItemConsumptionRequest> requestedItems,
        IReadOnlyCollection<EntityId> dependencyJobIds,
        IReadOnlyCollection<EntityId> transitStackIds,
        IReadOnlyCollection<EntityId> depositStackIds,
        int priority,
        long tick)
    {
        JobId = jobId;
        BuildingId = buildingId;
        RequestedItems = requestedItems
            ?? throw new ArgumentNullException(nameof(requestedItems));
        DependencyJobIds = dependencyJobIds
            ?? throw new ArgumentNullException(nameof(dependencyJobIds));
        TransitStackIds = transitStackIds
            ?? throw new ArgumentNullException(nameof(transitStackIds));
        DepositStackIds = depositStackIds
            ?? throw new ArgumentNullException(nameof(depositStackIds));
        Priority = priority;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId BuildingId { get; }
    public IReadOnlyCollection<ItemConsumptionRequest> RequestedItems { get; }
    public IReadOnlyCollection<EntityId> DependencyJobIds { get; }
    public IReadOnlyCollection<EntityId> TransitStackIds { get; }
    public IReadOnlyCollection<EntityId> DepositStackIds { get; }
    public int Priority { get; }
    public long Tick { get; }
}

public sealed class ResolveDeferredBuildingSupplyJobCommand : ICommand<Result>
{
    public ResolveDeferredBuildingSupplyJobCommand(
        EntityId jobId,
        EntityId residentId,
        IReadOnlyCollection<CellId> revealedCells,
        IReadOnlyCollection<CellId> reachableCells,
        long tick)
    {
        JobId = jobId;
        ResidentId = residentId;
        RevealedCells = revealedCells
            ?? throw new ArgumentNullException(nameof(revealedCells));
        ReachableCells = reachableCells
            ?? throw new ArgumentNullException(nameof(reachableCells));
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId ResidentId { get; }
    public IReadOnlyCollection<CellId> RevealedCells { get; }
    public IReadOnlyCollection<CellId> ReachableCells { get; }
    public long Tick { get; }
}

public sealed class CancelDeferredBuildingSupplyJobCommand : ICommand<Result>
{
    public CancelDeferredBuildingSupplyJobCommand(
        EntityId jobId,
        string reason,
        long tick)
    {
        JobId = jobId;
        Reason = string.IsNullOrWhiteSpace(reason)
            ? throw new ArgumentException("Cancellation reason is required.", nameof(reason))
            : reason.Trim();
        Tick = tick;
    }

    public EntityId JobId { get; }
    public string Reason { get; }
    public long Tick { get; }
}

public sealed class AcquireBuildingSupplyCommand : ICommand<Result>
{
    public AcquireBuildingSupplyCommand(EntityId jobId, long tick)
    {
        JobId = jobId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public long Tick { get; }
}

public sealed class AcquireBuildingSupplySourceCommand : ICommand<Result>
{
    public AcquireBuildingSupplySourceCommand(
        EntityId jobId,
        EntityId sourceStackId,
        long tick)
    {
        JobId = jobId;
        SourceStackId = sourceStackId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId SourceStackId { get; }
    public long Tick { get; }
}

public sealed class DepositBuildingSupplyCommand : ICommand<Result>
{
    public DepositBuildingSupplyCommand(EntityId jobId, long tick)
    {
        JobId = jobId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public long Tick { get; }
}

public sealed class CancelBuildingSupplyCommand : ICommand<Result>
{
    public CancelBuildingSupplyCommand(EntityId jobId, string reason, long tick)
    {
        JobId = jobId;
        Reason = reason;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public string Reason { get; }
    public long Tick { get; }
}

public sealed class EnableProductionInputDeliveryCommand : ICommand<Result>
{
    public EnableProductionInputDeliveryCommand(
        EntityId buildingId,
        IReadOnlyCollection<ItemConsumptionRequest> inputs,
        long tick)
    {
        BuildingId = buildingId;
        Inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
        Tick = tick;
    }

    public EntityId BuildingId { get; }
    public IReadOnlyCollection<ItemConsumptionRequest> Inputs { get; }
    public long Tick { get; }
}

public sealed class SetBuildingStockDeliveryCommand : ICommand<Result>
{
    public SetBuildingStockDeliveryCommand(
        EntityId buildingId,
        Dig.Domain.Inventory.ItemId itemId,
        bool enabled,
        long tick)
    {
        BuildingId = buildingId;
        ItemId = itemId;
        Enabled = enabled;
        Tick = tick;
    }

    public EntityId BuildingId { get; }
    public Dig.Domain.Inventory.ItemId ItemId { get; }
    public bool Enabled { get; }
    public long Tick { get; }
}

}
