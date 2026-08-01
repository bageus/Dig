using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    private void ApplyResidentInventoryCompaction(
        EntityId residentId,
        long tick,
        IReadOnlyDictionary<EntityId, ResidentInventorySlot> expansionAssignments,
        IReadOnlyCollection<ResidentUnitCandidate> pendingUnits)
    {
        bool changed = false;
        foreach (ResidentUnitCandidate candidate in pendingUnits
            .Where(value => !value.IsOriginal)
            .OrderBy(value => value.Source.Id.ToString(), StringComparer.Ordinal)
            .ThenBy(value => value.Ordinal))
        {
            ItemLocation sourceLocation = candidate.Source.Location;
            ItemStackState unit = candidate.Source.Split(
                candidate.UnitId,
                quantity: 1,
                ItemLocation.InAgent(residentId));
            _stacks.Add(unit.Id, unit);
            candidate.Materialize(unit);
            Raise(new ItemStackMoved(
                tick,
                candidate.Source.Id,
                unit.Id,
                unit.ItemId,
                quantity: 1,
                sourceLocation,
                unit.Location));
            changed = true;
        }

        foreach (ResidentUnitCandidate candidate in pendingUnits
            .Where(value => value.IsOriginal))
        {
            candidate.Materialize(candidate.Source);
        }

        Dictionary<EntityId, ItemLocation> originalLocations =
            new Dictionary<EntityId, ItemLocation>();
        foreach (KeyValuePair<EntityId, ResidentInventorySlot> assignment
            in expansionAssignments)
        {
            ItemStackState stack = Find(assignment.Key)!;
            ItemLocation destination = ItemLocation.InResidentSlot(
                residentId,
                assignment.Value.Compartment,
                assignment.Value.Index);
            if (stack.Location != destination)
            {
                originalLocations.Add(stack.Id, stack.Location);
            }
        }

        foreach (ResidentUnitCandidate candidate in pendingUnits
            .Where(value => value.IsOriginal))
        {
            ItemStackState stack = candidate.Source;
            ItemLocation destination = ItemLocation.InResidentSlot(
                residentId,
                candidate.AssignedSlot.Compartment,
                candidate.AssignedSlot.Index);
            if (stack.Location != destination)
            {
                originalLocations.Add(stack.Id, stack.Location);
            }
        }

        foreach (EntityId stackId in originalLocations.Keys
            .OrderBy(value => value.ToString(), StringComparer.Ordinal))
        {
            ItemStackState stack = Find(stackId)!;
            if (stack.Location.HasResidentSlot)
            {
                stack.MoveFull(ItemLocation.InAgent(residentId));
            }
        }

        foreach (KeyValuePair<EntityId, ResidentInventorySlot> assignment
            in expansionAssignments
                .OrderBy(value => value.Value.Index)
                .ThenBy(value => value.Key.ToString(), StringComparer.Ordinal))
        {
            ItemStackState stack = Find(assignment.Key)!;
            ItemLocation destination = ItemLocation.InResidentSlot(
                residentId,
                assignment.Value.Compartment,
                assignment.Value.Index);
            if (!originalLocations.TryGetValue(stack.Id, out ItemLocation source))
            {
                continue;
            }

            stack.MoveFull(destination);
            Raise(new ItemStackMoved(
                tick,
                stack.Id,
                stack.Id,
                stack.ItemId,
                stack.Quantity,
                source,
                destination));
            changed = true;
        }

        foreach (ResidentUnitCandidate candidate in pendingUnits
            .OrderBy(value => value.AssignedSlot.Compartment)
            .ThenBy(value => value.AssignedSlot.Index)
            .ThenBy(value => value.UnitId.ToString(), StringComparer.Ordinal))
        {
            ItemStackState unit = candidate.Materialized!;
            ResidentInventorySlot slot = candidate.AssignedSlot;
            ItemLocation destination = ItemLocation.InResidentSlot(
                residentId,
                slot.Compartment,
                slot.Index);
            ItemLocation source = originalLocations.TryGetValue(
                unit.Id,
                out ItemLocation original)
                    ? original
                    : unit.Location;
            if (source == destination)
            {
                continue;
            }

            unit.MoveFull(destination);
            Raise(new ItemStackMoved(
                tick,
                unit.Id,
                unit.Id,
                unit.ItemId,
                quantity: 1,
                source,
                destination));
            changed = true;
        }

        if (changed)
        {
            IncrementVersion();
        }

    }

}

}
