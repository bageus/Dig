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
        List<ItemStackState> unslottedItems = new List<ItemStackState>();

        for (int index = 0; index < stacks.Length; index++)
        {
            ItemStackState stack = stacks[index];
            ItemDefinition definition = Catalog.Get(stack.ItemId);
            if (!stack.Location.HasResidentSlot)
            {
                (definition.IsInventoryExpansion
                    ? unslottedExpansions
                    : unslottedItems).Add(stack);
                continue;
            }

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

        Dictionary<EntityId, ResidentInventorySlot> assignments =
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
            assignments.Add(expansion.Id, slot);
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

        foreach (ItemStackState item in unslottedItems
            .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal))
        {
            if (!TryFindCompatibleFreeSlot(
                    item,
                    cargoCapacity,
                    weaponCapacity,
                    activeCargo,
                    activeWeapon,
                    occupied,
                    out ResidentInventorySlot slot))
            {
                return Result.Failure(InventoryErrors.ResidentInventoryCapacityExceeded);
            }

            occupied.Add(slot, item);
            assignments.Add(item.Id, slot);
        }

        if (assignments.Count == 0)
        {
            return Result.Success();
        }

        foreach (KeyValuePair<EntityId, ResidentInventorySlot> assignment in assignments
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
        }

        IncrementVersion();
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
        ResidentInventoryLayoutSnapshot layout = GetResidentInventoryLayout(residentId);
        if (!layout.ActiveCargoExpansion.HasValue
            || !layout.Slots.Any(slot =>
                slot.Slot.Compartment == ResidentInventoryCompartment.Cargo
                && !slot.IsEmpty))
        {
            return 1d;
        }

        return layout.ActiveCargoExpansion.Value
            .Definition.MoveSpeedMultiplierWhenOccupied;
    }

    private Dictionary<ResidentInventorySlot, ItemStackState> CreateSlottedOccupancy(
        EntityId residentId)
    {
        Dictionary<ResidentInventorySlot, ItemStackState> occupied =
            new Dictionary<ResidentInventorySlot, ItemStackState>();
        ItemStackState[] stacks = GetResidentStacks(residentId);
        for (int index = 0; index < stacks.Length; index++)
        {
            ItemStackState stack = stacks[index];
            if (!stack.Location.HasResidentSlot)
            {
                continue;
            }

            ResidentInventorySlot slot = stack.Location.ResidentSlot;
            if (occupied.ContainsKey(slot))
            {
                throw new InvalidOperationException(
                    InventoryErrors.ResidentInventoryLayoutInvalid.ToString());
            }

            occupied.Add(slot, stack);
        }

        return occupied;
    }

    private ItemStackState[] GetResidentStacks(EntityId residentId)
    {
        return _stacks.Values
            .Where(stack => stack.Location.Kind == ItemLocationKind.AgentInventory
                && stack.Location.HasOwner
                && stack.Location.OwnerId == residentId)
            .OrderBy(stack => stack.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
    }

    private ActiveInventoryExpansionSnapshot? ResolveActiveExpansion(
        IReadOnlyDictionary<ResidentInventorySlot, ItemStackState> occupied,
        InventoryExpansionGroup group)
    {
        ItemStackState? stack = occupied
            .Where(entry => entry.Key.Compartment == ResidentInventoryCompartment.Main)
            .Select(entry => entry.Value)
            .Where(value => Catalog.Get(value.ItemId).InventoryExpansion?.Group == group)
            .OrderByDescending(value => Catalog.Get(value.ItemId).InventoryExpansion!.Tier)
            .ThenBy(value => value.Id.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();
        if (stack is null)
        {
            return null;
        }

        return new ActiveInventoryExpansionSnapshot(
            stack.Id,
            stack.ItemId,
            Catalog.Get(stack.ItemId).InventoryExpansion!);
    }

    private static bool TryFindFreeSlot(
        ResidentInventoryCompartment compartment,
        int capacity,
        IReadOnlyDictionary<ResidentInventorySlot, ItemStackState> occupied,
        out ResidentInventorySlot slot)
    {
        for (int index = 0; index < capacity; index++)
        {
            ResidentInventorySlot candidate = new ResidentInventorySlot(compartment, index);
            if (!occupied.ContainsKey(candidate))
            {
                slot = candidate;
                return true;
            }
        }

        slot = default;
        return false;
    }

    private bool TryFindCompatibleFreeSlot(
        ItemStackState stack,
        int cargoCapacity,
        int weaponCapacity,
        ActiveInventoryExpansionSnapshot? activeCargo,
        ActiveInventoryExpansionSnapshot? activeWeapon,
        IReadOnlyDictionary<ResidentInventorySlot, ItemStackState> occupied,
        out ResidentInventorySlot slot)
    {
        if (TryFindFreeSlot(
                ResidentInventoryCompartment.Main,
                ResidentInventoryLayoutSnapshot.MainSlotCount,
                occupied,
                out slot))
        {
            return true;
        }

        ItemDefinition definition = Catalog.Get(stack.ItemId);
        if (activeCargo.HasValue
            && activeCargo.Value.Definition.Accepts(definition)
            && TryFindFreeSlot(
                ResidentInventoryCompartment.Cargo,
                cargoCapacity,
                occupied,
                out slot))
        {
            return true;
        }

        return activeWeapon.HasValue
            && activeWeapon.Value.Definition.Accepts(definition)
            && TryFindFreeSlot(
                ResidentInventoryCompartment.Weapon,
                weaponCapacity,
                occupied,
                out slot);
    }

    private Result ValidatePlacedStack(
        ItemStackState stack,
        ResidentInventorySlot slot,
        int cargoCapacity,
        int weaponCapacity,
        ActiveInventoryExpansionSnapshot? activeCargo,
        ActiveInventoryExpansionSnapshot? activeWeapon)
    {
        int capacity = slot.Compartment switch
        {
            ResidentInventoryCompartment.Main => ResidentInventoryLayoutSnapshot.MainSlotCount,
            ResidentInventoryCompartment.Cargo => cargoCapacity,
            ResidentInventoryCompartment.Weapon => weaponCapacity,
            _ => 0,
        };
        if (slot.Index >= capacity)
        {
            return Result.Failure(InventoryErrors.ResidentSlotOutOfRange);
        }

        ItemDefinition definition = Catalog.Get(stack.ItemId);
        if (definition.IsInventoryExpansion)
        {
            return slot.Compartment == ResidentInventoryCompartment.Main
                ? Result.Success()
                : Result.Failure(InventoryErrors.InventoryExpansionMainOnly);
        }

        if (slot.Compartment == ResidentInventoryCompartment.Cargo)
        {
            return activeCargo.HasValue && activeCargo.Value.Definition.Accepts(definition)
                ? Result.Success()
                : Result.Failure(InventoryErrors.ResidentSlotCategoryRejected);
        }

        if (slot.Compartment == ResidentInventoryCompartment.Weapon)
        {
            return activeWeapon.HasValue && activeWeapon.Value.Definition.Accepts(definition)
                ? Result.Success()
                : Result.Failure(InventoryErrors.ResidentSlotCategoryRejected);
        }

        return Result.Success();
    }

    private static void ValidateResidentId(EntityId residentId)
    {
        if (residentId.IsEmpty)
        {
            throw new ArgumentException("Resident id is required.", nameof(residentId));
        }
    }
}

}
