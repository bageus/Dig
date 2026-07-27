using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Inventory;

namespace Dig.Domain.Production
{

public readonly struct ProductionMaterialStepSnapshot
{
    public ProductionMaterialStepSnapshot(
        int index,
        ItemId itemId,
        long requiredTicks,
        long completedTicks,
        bool consumed)
    {
        Index = index;
        ItemId = itemId;
        RequiredTicks = requiredTicks;
        CompletedTicks = completedTicks;
        Consumed = consumed;
    }

    public int Index { get; }
    public ItemId ItemId { get; }
    public long RequiredTicks { get; }
    public long CompletedTicks { get; }
    public bool Consumed { get; }
}

public sealed class ProductionMaterialWorkResult
{
    public ProductionMaterialWorkResult(
        IReadOnlyCollection<ItemId> consumedItems,
        bool readyToComplete)
    {
        ConsumedItems = new ReadOnlyCollection<ItemId>(consumedItems.ToArray());
        ReadyToComplete = readyToComplete;
    }

    public IReadOnlyList<ItemId> ConsumedItems { get; }
    public bool ReadyToComplete { get; }
}

}
