using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.Production
{

public enum ProductionMaterialStepPhase
{
    AwaitingMaterial = 0,
    StagedOnWorkbench = 1,
    Processing = 2,
    ProcessedAwaitingPackage = 3,
    Deposited = 4,
}

public readonly struct ProductionMaterialStepSnapshot
{
    public ProductionMaterialStepSnapshot(
        int index,
        ItemId itemId,
        long requiredTicks,
        long completedTicks,
        bool consumed)
        : this(
            index,
            itemId,
            requiredTicks,
            completedTicks,
            consumed
                ? ProductionMaterialStepPhase.Deposited
                : ProductionMaterialStepPhase.AwaitingMaterial)
    {
    }

    public ProductionMaterialStepSnapshot(
        int index,
        ItemId itemId,
        long requiredTicks,
        long completedTicks,
        ProductionMaterialStepPhase phase)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Material item id is required.", nameof(itemId));
        }

        if (requiredTicks < 0
            || completedTicks < 0
            || completedTicks > requiredTicks)
        {
            throw new ArgumentOutOfRangeException(nameof(completedTicks));
        }

        if (!Enum.IsDefined(typeof(ProductionMaterialStepPhase), phase))
        {
            throw new ArgumentOutOfRangeException(nameof(phase));
        }

        Index = index;
        ItemId = itemId;
        RequiredTicks = requiredTicks;
        CompletedTicks = completedTicks;
        Phase = phase;
    }

    public int Index { get; }
    public ItemId ItemId { get; }
    public long RequiredTicks { get; }
    public long CompletedTicks { get; }
    public ProductionMaterialStepPhase Phase { get; }
    public bool Consumed => Phase == ProductionMaterialStepPhase.Deposited;
    public bool IsStaged => Phase is ProductionMaterialStepPhase.StagedOnWorkbench
        or ProductionMaterialStepPhase.Processing
        or ProductionMaterialStepPhase.ProcessedAwaitingPackage;
    public bool IsProcessed => Phase is ProductionMaterialStepPhase.ProcessedAwaitingPackage
        or ProductionMaterialStepPhase.Deposited;
}

public sealed class ProductionMaterialWorkResult
{
    public ProductionMaterialWorkResult(
        IReadOnlyCollection<ItemId> processedItems,
        bool readyForPackageDeposit,
        long appliedTicks)
    {
        if (processedItems is null)
        {
            throw new ArgumentNullException(nameof(processedItems));
        }

        if (appliedTicks < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(appliedTicks));
        }

        ProcessedItems = new ReadOnlyCollection<ItemId>(processedItems.ToArray());
        ReadyForPackageDeposit = readyForPackageDeposit;
        AppliedTicks = appliedTicks;
    }

    public IReadOnlyList<ItemId> ProcessedItems { get; }

    // Compatibility alias for callers compiled against the pre-workbench contract.
    public IReadOnlyList<ItemId> ConsumedItems => ProcessedItems;

    public bool ReadyForPackageDeposit { get; }

    // Material work alone never completes an order; package deposit owns that transition.
    public bool ReadyToComplete => false;

    public long AppliedTicks { get; }
}

public sealed class ProductionMaterialStepPhaseChanged : IDomainEvent
{
    public ProductionMaterialStepPhaseChanged(
        long tick,
        EntityId orderId,
        EntityId buildingId,
        int stepIndex,
        ItemId itemId,
        ProductionMaterialStepPhase previous,
        ProductionMaterialStepPhase current)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (orderId.IsEmpty || buildingId.IsEmpty || itemId.IsEmpty)
        {
            throw new ArgumentException("Production material phase ids are required.");
        }

        if (stepIndex < 0
            || !Enum.IsDefined(typeof(ProductionMaterialStepPhase), previous)
            || !Enum.IsDefined(typeof(ProductionMaterialStepPhase), current))
        {
            throw new ArgumentOutOfRangeException(nameof(stepIndex));
        }

        Tick = tick;
        OrderId = orderId;
        BuildingId = buildingId;
        StepIndex = stepIndex;
        ItemId = itemId;
        Previous = previous;
        Current = current;
    }

    public long Tick { get; }
    public EntityId OrderId { get; }
    public EntityId BuildingId { get; }
    public int StepIndex { get; }
    public ItemId ItemId { get; }
    public ProductionMaterialStepPhase Previous { get; }
    public ProductionMaterialStepPhase Current { get; }
}

}
