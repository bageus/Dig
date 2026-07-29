using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    private bool ConsolidateResidentStacks(EntityId residentId, long tick)
    {
        bool changed = false;
        ItemStackState[] candidates = GetResidentStacks(residentId)
            .Where(CanConsolidateResidentStack)
            .OrderBy(value => value.Location.HasResidentSlot ? 0 : 1)
            .ThenBy(value => value.Location)
            .ThenBy(value => value.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        foreach (IGrouping<ItemId, ItemStackState> group in candidates
            .GroupBy(value => value.ItemId)
            .OrderBy(value => value.Key.ToString(), StringComparer.Ordinal))
        {
            ItemDefinition definition = Catalog.Get(group.Key);
            if (definition.IsInventoryExpansion || definition.MaximumStackSize <= 1)
            {
                continue;
            }

            ItemStackState[] stacks = group.ToArray();
            for (int targetIndex = 0; targetIndex < stacks.Length; targetIndex++)
            {
                ItemStackState target = stacks[targetIndex];
                if (!_stacks.ContainsKey(target.Id))
                {
                    continue;
                }

                int capacity = definition.MaximumStackSize - target.Quantity;
                for (int sourceIndex = targetIndex + 1;
                    capacity > 0 && sourceIndex < stacks.Length;
                    sourceIndex++)
                {
                    ItemStackState source = stacks[sourceIndex];
                    if (!_stacks.ContainsKey(source.Id)
                        || source.ItemId != target.ItemId
                        || !CanConsolidateResidentStack(source))
                    {
                        continue;
                    }

                    int quantity = Math.Min(capacity, source.Quantity);
                    if (quantity <= 0)
                    {
                        continue;
                    }

                    ItemLocation sourceLocation = source.Location;
                    source.ConsumeAvailable(quantity);
                    target.AddQuantity(quantity);
                    if (source.Quantity == 0)
                    {
                        _stacks.Remove(source.Id);
                    }

                    Raise(new ItemStackMoved(
                        tick,
                        source.Id,
                        target.Id,
                        source.ItemId,
                        quantity,
                        sourceLocation,
                        target.Location));
                    capacity -= quantity;
                    changed = true;
                }
            }
        }

        if (changed)
        {
            IncrementVersion();
        }

        return changed;
    }

    private bool CanConsolidateResidentStack(ItemStackState stack)
    {
        if (stack.ReservedQuantity != 0
            || stack.HeldQuantity != 0
            || Catalog.Get(stack.ItemId).IsInventoryExpansion)
        {
            return false;
        }

        if (!stack.Location.HasResidentSlot)
        {
            return true;
        }

        ResidentInventorySlot slot = stack.Location.ResidentSlot;
        return !_residentSlotClaims.Any(value =>
            value.ResidentId == stack.Location.OwnerId && value.Slot == slot);
    }
}

}
