using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
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

    private ActiveInventoryExpansionSnapshot? ResolveActiveExpansion(
        IEnumerable<ItemStackState> expansions,
        InventoryExpansionGroup group)
    {
        ItemStackState? stack = expansions
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
        ISet<ResidentInventorySlot> unavailable,
        out ResidentInventorySlot slot)
    {
        for (int index = 0; index < capacity; index++)
        {
            ResidentInventorySlot candidate = new ResidentInventorySlot(compartment, index);
            if (!occupied.ContainsKey(candidate) && !unavailable.Contains(candidate))
            {
                slot = candidate;
                return true;
            }
        }

        slot = default;
        return false;
    }

    private static int ResidentUnitCompartmentRank(
        ItemStackState stack,
        bool prefersWeapon)
    {
        if (!stack.Location.HasResidentSlot)
        {
            return 3;
        }

        ResidentInventoryCompartment compartment =
            stack.Location.ResidentCompartment;
        if (prefersWeapon)
        {
            return compartment switch
            {
                ResidentInventoryCompartment.Weapon => 0,
                ResidentInventoryCompartment.Main => 1,
                ResidentInventoryCompartment.Cargo => 2,
                _ => 3,
            };
        }

        return compartment switch
        {
            ResidentInventoryCompartment.Main => 0,
            ResidentInventoryCompartment.Cargo => 1,
            ResidentInventoryCompartment.Weapon => 2,
            _ => 3,
        };
    }

    private static int ResidentUnitSlotIndex(ItemStackState stack)
    {
        return stack.Location.HasResidentSlot
            ? stack.Location.ResidentSlotIndex
            : int.MaxValue;
    }

    private static int ResidentPlacementCompartmentRank(ItemStackState stack)
    {
        if (!stack.Location.HasResidentSlot)
        {
            return 3;
        }

        return stack.Location.ResidentCompartment switch
        {
            ResidentInventoryCompartment.Main => 0,
            ResidentInventoryCompartment.Cargo => 1,
            ResidentInventoryCompartment.Weapon => 2,
            _ => 3,
        };
    }

    private static int CountAvailableSlots(
        ResidentInventoryCompartment compartment,
        int capacity,
        IReadOnlyDictionary<ResidentInventorySlot, ItemStackState> occupied,
        ISet<ResidentInventorySlot> unavailable)
    {
        int available = 0;
        for (int index = 0; index < capacity; index++)
        {
            ResidentInventorySlot slot = new ResidentInventorySlot(compartment, index);
            if (!occupied.ContainsKey(slot) && !unavailable.Contains(slot))
            {
                available++;
            }
        }

        return available;
    }

    private Result ValidatePlacedStack(
        ItemStackState stack,
        ResidentInventorySlot slot,
        int cargoCapacity,
        int weaponCapacity,
        ActiveInventoryExpansionSnapshot? activeCargo,
        ActiveInventoryExpansionSnapshot? activeWeapon)
    {
        int capacity = GetCompartmentCapacity(
            slot.Compartment,
            cargoCapacity,
            weaponCapacity);
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

    private void AddSlotSnapshots(
        ResidentInventoryCompartment compartment,
        int capacity,
        IReadOnlyDictionary<ResidentInventorySlot, ItemStackState> occupied,
        ActiveInventoryExpansionSnapshot? activeCargo,
        ActiveInventoryExpansionSnapshot? activeWeapon,
        ICollection<ResidentInventorySlotSnapshot> destination)
    {
        EntityId? activeCargoId = activeCargo?.StackId;
        EntityId? activeWeaponId = activeWeapon?.StackId;
        for (int index = 0; index < capacity; index++)
        {
            ResidentInventorySlot slot = new ResidentInventorySlot(compartment, index);
            if (!occupied.TryGetValue(slot, out ItemStackState? stack))
            {
                destination.Add(new ResidentInventorySlotSnapshot(
                    slot,
                    stackId: null,
                    itemId: null,
                    quantity: 0,
                    reservedQuantity: 0,
                    isActiveExpansion: false));
                continue;
            }

            bool active = activeCargoId == stack.Id || activeWeaponId == stack.Id;
            destination.Add(new ResidentInventorySlotSnapshot(
                slot,
                stack.Id,
                stack.ItemId,
                stack.Quantity,
                stack.ReservedQuantity,
                active));
        }
    }

    private static int GetCompartmentCapacity(
        ResidentInventoryCompartment compartment,
        int cargoCapacity,
        int weaponCapacity)
    {
        return compartment switch
        {
            ResidentInventoryCompartment.Main => ResidentInventoryLayoutSnapshot.MainSlotCount,
            ResidentInventoryCompartment.Cargo => cargoCapacity,
            ResidentInventoryCompartment.Weapon => weaponCapacity,
            _ => throw new ArgumentOutOfRangeException(nameof(compartment)),
        };
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
