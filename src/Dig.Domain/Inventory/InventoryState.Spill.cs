using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    public Result DropReservedResidentStackWithSpill(
        EntityId stackId,
        EntityId reservationOwnerId,
        ItemLocation destination,
        long tick)
    {
        ValidateTick(tick);
        ValidateJobId(reservationOwnerId);
        ItemStackState? stack = Find(stackId);
        if (stack is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        if (destination.Kind != ItemLocationKind.World || !destination.HasCell)
        {
            return Result.Failure(InventoryErrors.ResidentInventoryLayoutInvalid);
        }

        if (stack.GetReservedQuantity(reservationOwnerId) != stack.Quantity
            || stack.ReservedQuantity != stack.Quantity)
        {
            return Result.Failure(InventoryErrors.ReservationNotFound);
        }

        ItemDefinition definition = Catalog.Get(stack.ItemId);
        if (!IsActiveResidentExpansion(
                stack,
                definition,
                out ResidentInventoryCompartment compartment))
        {
            return MoveReserved(
                stackId,
                reservationOwnerId,
                stack.Quantity,
                destination,
                splitStackId: default,
                tick);
        }

        Result validation = ValidateActiveExpansionSpill(
            stack,
            compartment,
            allowExpansionReservationOwner: reservationOwnerId,
            out ItemStackState[] affected);
        if (validation.IsFailure)
        {
            return validation;
        }

        IReadOnlyList<SpillStackPlan> plans = BuildSpillPlans(affected, destination);
        stack.ConsumeReservation(reservationOwnerId, stack.Quantity);
        Raise(new ItemQuantityReservationChanged(
            tick,
            stack.Id,
            reservationOwnerId,
            0));
        ApplySpillPlans(plans, destination, tick);
        return Result.Success();
    }

    public Result DropResidentStackWithSpill(
        EntityId stackId,
        ItemLocation destination,
        long tick)
    {
        ValidateTick(tick);
        ItemStackState? stack = Find(stackId);
        if (stack is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        if (destination.Kind != ItemLocationKind.World || !destination.HasCell)
        {
            return Result.Failure(InventoryErrors.ResidentInventoryLayoutInvalid);
        }

        if (stack.AvailableQuantity != stack.Quantity)
        {
            return Result.Failure(InventoryErrors.InsufficientAvailableQuantity);
        }

        ItemDefinition definition = Catalog.Get(stack.ItemId);
        if (!IsActiveResidentExpansion(stack, definition, out ResidentInventoryCompartment compartment))
        {
            return MoveAvailable(
                stackId,
                stack.Quantity,
                destination,
                splitStackId: default,
                tick);
        }

        Result validation = ValidateActiveExpansionSpill(
            stack,
            compartment,
            allowExpansionReservationOwner: null,
            out ItemStackState[] affected);
        if (validation.IsFailure)
        {
            return validation;
        }

        IReadOnlyList<SpillStackPlan> plans = BuildSpillPlans(affected, destination);
        ApplySpillPlans(plans, destination, tick);
        return Result.Success();
    }

    private Result ValidateActiveExpansionSpill(
        ItemStackState expansion,
        ResidentInventoryCompartment compartment,
        EntityId? allowExpansionReservationOwner,
        out ItemStackState[] affected)
    {
        affected = Array.Empty<ItemStackState>();
        bool hasClaims = _residentSlotClaims.Any(claim =>
            claim.ResidentId == expansion.Location.OwnerId
            && claim.Slot.Compartment == compartment);
        if (hasClaims)
        {
            return Result.Failure(InventoryErrors.ResidentSlotClaimConflict);
        }

        ItemStackState[] contents = _stacks.Values
            .Where(value => value.Location.Kind == ItemLocationKind.AgentInventory
                && value.Location.HasOwner
                && value.Location.OwnerId == expansion.Location.OwnerId
                && value.Location.HasResidentSlot
                && value.Location.ResidentCompartment == compartment)
            .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        affected = contents
            .Concat(new[] { expansion })
            .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        bool unavailable = affected.Any(value =>
        {
            if (value.Id == expansion.Id && allowExpansionReservationOwner.HasValue)
            {
                return value.HeldQuantity != 0
                    || value.GetReservedQuantity(allowExpansionReservationOwner.Value)
                        != value.Quantity
                    || value.ReservedQuantity != value.Quantity;
            }

            return value.AvailableQuantity != value.Quantity;
        });
        return unavailable
            ? Result.Failure(InventoryErrors.InsufficientAvailableQuantity)
            : Result.Success();
    }

    private bool IsActiveResidentExpansion(
        ItemStackState stack,
        ItemDefinition definition,
        out ResidentInventoryCompartment compartment)
    {
        compartment = default;
        if (!definition.IsInventoryExpansion
            || stack.Location.Kind != ItemLocationKind.AgentInventory
            || !stack.Location.HasOwner
            || !stack.Location.HasResidentSlot
            || stack.Location.ResidentCompartment != ResidentInventoryCompartment.Main)
        {
            return false;
        }

        Dictionary<ResidentInventorySlot, ItemStackState> occupied =
            CreateSlottedOccupancy(stack.Location.OwnerId);
        ActiveInventoryExpansionSnapshot? active = ResolveActiveExpansion(
            occupied,
            definition.InventoryExpansion!.Group);
        if (!active.HasValue || active.Value.StackId != stack.Id)
        {
            return false;
        }

        compartment = definition.InventoryExpansion.Group == InventoryExpansionGroup.Cargo
            ? ResidentInventoryCompartment.Cargo
            : ResidentInventoryCompartment.Weapon;
        return true;
    }

    private IReadOnlyList<SpillStackPlan> BuildSpillPlans(
        IReadOnlyList<ItemStackState> affected,
        ItemLocation destination)
    {
        HashSet<EntityId> affectedIds = new HashSet<EntityId>(
            affected.Select(value => value.Id));
        Dictionary<ItemId, List<SpillTargetCapacity>> targets = _stacks.Values
            .Where(value => value.Location == destination
                && !affectedIds.Contains(value.Id))
            .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal)
            .GroupBy(value => value.ItemId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(value => new SpillTargetCapacity(
                    value,
                    Catalog.Get(value.ItemId).MaximumStackSize - value.Quantity))
                    .ToList());
        List<SpillStackPlan> plans = new List<SpillStackPlan>();
        for (int index = 0; index < affected.Count; index++)
        {
            ItemStackState source = affected[index];
            if (!targets.TryGetValue(source.ItemId, out List<SpillTargetCapacity>? itemTargets))
            {
                itemTargets = new List<SpillTargetCapacity>();
                targets.Add(source.ItemId, itemTargets);
            }

            int remaining = source.Quantity;
            List<SpillMergePlan> merges = new List<SpillMergePlan>();
            for (int targetIndex = 0;
                targetIndex < itemTargets.Count && remaining > 0;
                targetIndex++)
            {
                SpillTargetCapacity target = itemTargets[targetIndex];
                int moved = Math.Min(remaining, target.RemainingCapacity);
                if (moved == 0)
                {
                    continue;
                }

                merges.Add(new SpillMergePlan(target.Stack, moved));
                target.RemainingCapacity -= moved;
                remaining -= moved;
            }

            SpillStackPlan plan = new SpillStackPlan(source, merges, remaining);
            plans.Add(plan);
            if (remaining > 0)
            {
                int maximum = Catalog.Get(source.ItemId).MaximumStackSize;
                itemTargets.Add(new SpillTargetCapacity(source, maximum - remaining));
            }
        }

        return plans;
    }

    private void ApplySpillPlans(
        IReadOnlyList<SpillStackPlan> plans,
        ItemLocation destination,
        long tick)
    {
        for (int index = 0; index < plans.Count; index++)
        {
            SpillStackPlan plan = plans[index];
            ItemLocation sourceLocation = plan.Source.Location;
            int mergedQuantity = 0;
            for (int mergeIndex = 0; mergeIndex < plan.Merges.Count; mergeIndex++)
            {
                SpillMergePlan merge = plan.Merges[mergeIndex];
                plan.Source.ConsumeAvailable(merge.Quantity);
                merge.Target.AddQuantity(merge.Quantity);
                mergedQuantity = checked(mergedQuantity + merge.Quantity);
                Raise(new ItemStackMoved(
                    tick,
                    plan.Source.Id,
                    merge.Target.Id,
                    plan.Source.ItemId,
                    merge.Quantity,
                    sourceLocation,
                    destination));
            }

            if (plan.RemainingQuantity == 0)
            {
                _stacks.Remove(plan.Source.Id);
            }
            else
            {
                plan.Source.MoveFull(destination);
                Raise(new ItemStackMoved(
                    tick,
                    plan.Source.Id,
                    plan.Source.Id,
                    plan.Source.ItemId,
                    plan.RemainingQuantity,
                    sourceLocation,
                    destination));
            }

            if (mergedQuantity + plan.RemainingQuantity <= 0)
            {
                throw new InvalidOperationException("A spill plan moved no quantity.");
            }
        }

        IncrementVersion();
    }

    private sealed class SpillTargetCapacity
    {
        public SpillTargetCapacity(ItemStackState stack, int remainingCapacity)
        {
            Stack = stack;
            RemainingCapacity = remainingCapacity;
        }

        public ItemStackState Stack { get; }

        public int RemainingCapacity { get; set; }
    }

    private readonly struct SpillMergePlan
    {
        public SpillMergePlan(ItemStackState target, int quantity)
        {
            Target = target;
            Quantity = quantity;
        }

        public ItemStackState Target { get; }

        public int Quantity { get; }
    }

    private sealed class SpillStackPlan
    {
        public SpillStackPlan(
            ItemStackState source,
            IReadOnlyList<SpillMergePlan> merges,
            int remainingQuantity)
        {
            Source = source;
            Merges = merges;
            RemainingQuantity = remainingQuantity;
        }

        public ItemStackState Source { get; }

        public IReadOnlyList<SpillMergePlan> Merges { get; }

        public int RemainingQuantity { get; }
    }
}

}