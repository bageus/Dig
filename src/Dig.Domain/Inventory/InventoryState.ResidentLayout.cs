using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    public Result NormalizeResidentInventory(EntityId residentId, long tick)
    {
        ValidateTick(tick);
        ValidateResidentId(residentId);
        ItemStackState[] stacks = GetResidentStacks(residentId);
        Dictionary<ResidentInventorySlot, ItemStackState> occupied =
            new Dictionary<ResidentInventorySlot, ItemStackState>();
        List<ItemStackState> unslottedExpansions = new List<ItemStackState>();
        List<ResidentUnitCandidate> pendingUnits = new List<ResidentUnitCandidate>();

        for (int index = 0; index < stacks.Length; index++)
        {
            ItemStackState stack = stacks[index];
            ItemDefinition definition = Catalog.Get(stack.ItemId);
            if (definition.IsInventoryExpansion && stack.Quantity != 1)
            {
                return Result.Failure(InventoryErrors.ResidentInventoryLayoutInvalid);
            }

            if (!definition.IsInventoryExpansion
                && stack.Quantity > 1
                && (stack.ReservedQuantity != 0 || stack.HeldQuantity != 0))
            {
                return Result.Failure(InventoryErrors.ResidentInventoryLayoutInvalid);
            }

            if (stack.Location.HasResidentSlot)
            {
                ResidentInventorySlot slot = stack.Location.ResidentSlot;
                if (occupied.ContainsKey(slot))
                {
                    return Result.Failure(InventoryErrors.ResidentInventoryLayoutInvalid);
                }

                if (definition.IsInventoryExpansion
                    && slot.Compartment != ResidentInventoryCompartment.Main)
                {
                    return Result.Failure(InventoryErrors.InventoryExpansionMainOnly);
                }

                occupied.Add(slot, stack);
            }
            else if (definition.IsInventoryExpansion)
            {
                unslottedExpansions.Add(stack);
            }
            else
            {
                pendingUnits.Add(ResidentUnitCandidate.Original(stack));
            }

            if (definition.IsInventoryExpansion)
            {
                continue;
            }

            for (int ordinal = 1; ordinal < stack.Quantity; ordinal++)
            {
                Result<EntityId> unitId = CreateResidentUnitId(stack.Id, ordinal);
                if (unitId.IsFailure)
                {
                    return Result.Failure(unitId.Error!);
                }

                pendingUnits.Add(ResidentUnitCandidate.Split(stack, unitId.Value, ordinal));
            }
        }

        Dictionary<EntityId, ResidentInventorySlot> expansionAssignments =
            new Dictionary<EntityId, ResidentInventorySlot>();
        foreach (ItemStackState expansion in unslottedExpansions
            .OrderBy(value => Catalog.Get(value.ItemId).InventoryExpansion!.Group)
            .ThenByDescending(value => Catalog.Get(value.ItemId).InventoryExpansion!.Tier)
            .ThenBy(value => value.Id.ToString(), StringComparer.Ordinal))
        {
            if (!TryFindFreeSlot(
                    ResidentInventoryCompartment.Main,
                    ResidentInventoryLayoutSnapshot.MainSlotCount,
                    occupied,
                    out ResidentInventorySlot slot))
            {
                return Result.Failure(InventoryErrors.ResidentInventoryCapacityExceeded);
            }

            occupied.Add(slot, expansion);
            expansionAssignments.Add(expansion.Id, slot);
        }

        ActiveInventoryExpansionSnapshot? activeCargo = ResolveActiveExpansion(
            occupied,
            InventoryExpansionGroup.Cargo);
        ActiveInventoryExpansionSnapshot? activeWeapon = ResolveActiveExpansion(
            occupied,
            InventoryExpansionGroup.Weapon);
        int cargoCapacity = activeCargo?.Definition.AddedSlots ?? 0;
        int weaponCapacity = activeWeapon?.Definition.AddedSlots ?? 0;

        foreach (KeyValuePair<ResidentInventorySlot, ItemStackState> entry in occupied)
        {
            Result valid = ValidatePlacedStack(
                entry.Value,
                entry.Key,
                cargoCapacity,
                weaponCapacity,
                activeCargo,
                activeWeapon);
            if (valid.IsFailure)
            {
                return valid;
            }
        }

        foreach (ResidentUnitCandidate candidate in pendingUnits
            .OrderBy(value => value.Source.Id.ToString(), StringComparer.Ordinal)
            .ThenBy(value => value.Ordinal))
        {
            if (!TryFindCompatibleFreeSlot(
                    candidate.Source,
                    cargoCapacity,
                    weaponCapacity,
                    activeCargo,
                    activeWeapon,
                    occupied,
                    out ResidentInventorySlot slot))
            {
                return Result.Failure(InventoryErrors.ResidentInventoryCapacityExceeded);
            }

            candidate.Assign(slot);
            occupied.Add(slot, candidate.Source);
        }

        bool changed = false;
        foreach (KeyValuePair<EntityId, ResidentInventorySlot> assignment in expansionAssignments
            .OrderBy(value => value.Key.ToString(), StringComparer.Ordinal))
        {
            ItemStackState stack = Find(assignment.Key)!;
            ItemLocation source = stack.Location;
            ItemLocation destination = ItemLocation.InResidentSlot(
                residentId,
                assignment.Value.Compartment,
                assignment.Value.Index);
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

        foreach (ResidentUnitCandidate candidate in pendingUnits
            .OrderBy(value => value.UnitId.ToString(), StringComparer.Ordinal))
        {
            ItemStackState unit = candidate.Materialized!;
            ResidentInventorySlot slot = candidate.AssignedSlot;
            ItemLocation destination = ItemLocation.InResidentSlot(
                residentId,
                slot.Compartment,
                slot.Index);
            if (unit.Location == destination)
            {
                continue;
            }

            ItemLocation source = unit.Location;
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

        return Result.Success();
    }

    public ResidentInventoryLayoutSnapshot GetResidentInventoryLayout(EntityId residentId)
    {
        ValidateResidentId(residentId);
        Dictionary<ResidentInventorySlot, ItemStackState> occupied =
            CreateSlottedOccupancy(residentId);
        ActiveInventoryExpansionSnapshot? activeCargo = ResolveActiveExpansion(
            occupied,
            InventoryExpansionGroup.Cargo);
        ActiveInventoryExpansionSnapshot? activeWeapon = ResolveActiveExpansion(
            occupied,
            InventoryExpansionGroup.Weapon);
        int cargoCapacity = activeCargo?.Definition.AddedSlots ?? 0;
        int weaponCapacity = activeWeapon?.Definition.AddedSlots ?? 0;
        List<ResidentInventorySlotSnapshot> slots = new List<ResidentInventorySlotSnapshot>();
        AddSlotSnapshots(
            ResidentInventoryCompartment.Weapon,
            weaponCapacity,
            occupied,
            activeCargo,
            activeWeapon,
            slots);
        AddSlotSnapshots(
            ResidentInventoryCompartment.Main,
            ResidentInventoryLayoutSnapshot.MainSlotCount,
            occupied,
            activeCargo,
            activeWeapon,
            slots);
        AddSlotSnapshots(
            ResidentInventoryCompartment.Cargo,
            cargoCapacity,
            occupied,
            activeCargo,
            activeWeapon,
            slots);
        return new ResidentInventoryLayoutSnapshot(
            residentId,
            cargoCapacity,
            weaponCapacity,
            activeCargo,
            activeWeapon,
            slots);
    }

    public double GetResidentMoveSpeedMultiplier(EntityId residentId)
    {
        ValidateResidentId(residentId);
        ItemStackState? activeCargoStack = null;
        InventoryExpansionDefinition? activeCargoDefinition = null;
        bool cargoOccupied = false;
        foreach (ItemStackState stack in _stacks.Values)
        {
            ItemLocation location = stack.Location;
            if (location.Kind != ItemLocationKind.AgentInventory
                || !location.HasOwner
                || location.OwnerId != residentId
                || !location.HasResidentSlot)
            {
                continue;
            }

            if (location.ResidentCompartment == ResidentInventoryCompartment.Cargo)
            {
                cargoOccupied = true;
                continue;
            }

            if (location.ResidentCompartment != ResidentInventoryCompartment.Main)
            {
                continue;
            }

            InventoryExpansionDefinition? expansion =
                Catalog.Get(stack.ItemId).InventoryExpansion;
            if (expansion?.Group != InventoryExpansionGroup.Cargo)
            {
                continue;
            }

            if (activeCargoDefinition is null
                || expansion.Tier > activeCargoDefinition.Tier
                || (expansion.Tier == activeCargoDefinition.Tier
                    && string.Compare(
                        stack.Id.ToString(),
                        activeCargoStack!.Id.ToString(),
                        StringComparison.Ordinal) < 0))
            {
                activeCargoStack = stack;
                activeCargoDefinition = expansion;
            }
        }

        return !cargoOccupied || activeCargoDefinition is null
            ? 1d
            : activeCargoDefinition.MoveSpeedMultiplierWhenOccupied;
    }
}

}
