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
        HashSet<ResidentInventorySlot> currentSlots =
            new HashSet<ResidentInventorySlot>();
        List<ItemStackState> expansions = new List<ItemStackState>();
        List<ItemStackState> pinned = new List<ItemStackState>();
        List<ResidentUnitCandidate> pendingUnits = new List<ResidentUnitCandidate>();

        for (int index = 0; index < stacks.Length; index++)
        {
            ItemStackState stack = stacks[index];
            ItemDefinition definition = Catalog.Get(stack.ItemId);
            if (definition.IsInventoryExpansion && stack.Quantity != 1)
            {
                return Result.Failure(InventoryErrors.ResidentInventoryLayoutInvalid);
            }

            if (stack.Location.HasResidentSlot)
            {
                ResidentInventorySlot slot = stack.Location.ResidentSlot;
                if (!currentSlots.Add(slot))
                {
                    return Result.Failure(InventoryErrors.ResidentInventoryLayoutInvalid);
                }

                if (definition.IsInventoryExpansion
                    && slot.Compartment != ResidentInventoryCompartment.Main)
                {
                    return Result.Failure(InventoryErrors.InventoryExpansionMainOnly);
                }
            }

            if (definition.IsInventoryExpansion)
            {
                expansions.Add(stack);
            }

            if (!definition.IsInventoryExpansion
                && stack.Quantity > 1
                && (stack.ReservedQuantity != 0 || stack.HeldQuantity != 0))
            {
                return Result.Failure(InventoryErrors.ResidentInventoryLayoutInvalid);
            }

            if (stack.HeldQuantity != 0)
            {
                if (!stack.Location.HasResidentSlot)
                {
                    return Result.Failure(InventoryErrors.ResidentInventoryLayoutInvalid);
                }

                pinned.Add(stack);
                continue;
            }

            if (definition.IsInventoryExpansion)
            {
                continue;
            }

            pendingUnits.Add(ResidentUnitCandidate.Original(stack));
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

        HashSet<ResidentInventorySlot> unavailable =
            new HashSet<ResidentInventorySlot>();
        Dictionary<ResidentInventorySlot, ItemStackState> occupied =
            pinned.ToDictionary(value => value.Location.ResidentSlot);
        ActiveInventoryExpansionSnapshot? activeCargo = ResolveActiveExpansion(
            expansions,
            InventoryExpansionGroup.Cargo);
        ActiveInventoryExpansionSnapshot? activeWeapon = ResolveActiveExpansion(
            expansions,
            InventoryExpansionGroup.Weapon);
        int cargoCapacity = activeCargo?.Definition.AddedSlots ?? 0;
        int weaponCapacity = activeWeapon?.Definition.AddedSlots ?? 0;
        foreach (ItemStackState stack in pinned)
        {
            Result valid = ValidatePlacedStack(
                stack,
                stack.Location.ResidentSlot,
                cargoCapacity,
                weaponCapacity,
                activeCargo,
                activeWeapon);
            if (valid.IsFailure)
            {
                return valid;
            }
        }

        if (activeWeapon.HasValue)
        {
            foreach (ResidentUnitCandidate candidate in pendingUnits
                .Where(value => activeWeapon.Value.Definition.Accepts(
                    Catalog.Get(value.Source.ItemId)))
                .OrderBy(value => ResidentUnitCompartmentRank(
                    value.Source,
                    prefersWeapon: true))
                .ThenBy(value => ResidentUnitSlotIndex(value.Source))
                .ThenBy(value => value.Source.Id.ToString(), StringComparer.Ordinal)
                .ThenBy(value => value.Ordinal))
            {
                if (!TryFindFreeSlot(
                        ResidentInventoryCompartment.Weapon,
                        weaponCapacity,
                        occupied,
                        unavailable,
                        out ResidentInventorySlot slot))
                {
                    break;
                }

                candidate.Assign(slot);
                occupied.Add(slot, candidate.Source);
            }
        }

        HashSet<EntityId> pinnedIds = pinned
            .Select(value => value.Id)
            .ToHashSet();
        List<ResidentPlacementCandidate> mainCandidates = expansions
            .Where(value => !pinnedIds.Contains(value.Id))
            .Select(ResidentPlacementCandidate.Expansion)
            .Concat(pendingUnits
                .Where(value => !value.HasAssignedSlot)
                .Select(ResidentPlacementCandidate.Ordinary))
            .OrderBy(value => ResidentPlacementCompartmentRank(value.Source))
            .ThenBy(value => ResidentUnitSlotIndex(value.Source))
            .ThenBy(value => value.UnitId.ToString(), StringComparer.Ordinal)
            .ThenBy(value => value.Ordinal)
            .ToList();
        int remainingExpansions = expansions.Count(
            value => !pinnedIds.Contains(value.Id));
        if (CountAvailableSlots(
                ResidentInventoryCompartment.Main,
                ResidentInventoryLayoutSnapshot.MainSlotCount,
                occupied,
                unavailable) < remainingExpansions)
        {
            return Result.Failure(InventoryErrors.ResidentInventoryCapacityExceeded);
        }

        Dictionary<EntityId, ResidentInventorySlot> expansionAssignments =
            new Dictionary<EntityId, ResidentInventorySlot>();
        List<ResidentUnitCandidate> cargoCandidates =
            new List<ResidentUnitCandidate>();
        foreach (ResidentPlacementCandidate candidate in mainCandidates)
        {
            int availableMain = CountAvailableSlots(
                ResidentInventoryCompartment.Main,
                ResidentInventoryLayoutSnapshot.MainSlotCount,
                occupied,
                unavailable);
            bool placeInMain = candidate.IsExpansion
                || availableMain > remainingExpansions;
            if (!placeInMain)
            {
                cargoCandidates.Add(candidate.Unit!);
                continue;
            }

            if (!TryFindFreeSlot(
                    ResidentInventoryCompartment.Main,
                    ResidentInventoryLayoutSnapshot.MainSlotCount,
                    occupied,
                    unavailable,
                    out ResidentInventorySlot slot))
            {
                return Result.Failure(InventoryErrors.ResidentInventoryCapacityExceeded);
            }

            candidate.Assign(slot);
            occupied.Add(slot, candidate.Source);
            if (candidate.IsExpansion)
            {
                expansionAssignments.Add(candidate.Source.Id, slot);
                remainingExpansions--;
            }
        }

        foreach (ResidentUnitCandidate candidate in cargoCandidates)
        {
            ItemDefinition definition = Catalog.Get(candidate.Source.ItemId);
            if (!activeCargo.HasValue
                || !activeCargo.Value.Definition.Accepts(definition)
                || !TryFindFreeSlot(
                    ResidentInventoryCompartment.Cargo,
                    cargoCapacity,
                    occupied,
                    unavailable,
                    out ResidentInventorySlot slot))
            {
                return Result.Failure(InventoryErrors.ResidentInventoryCapacityExceeded);
            }

            candidate.Assign(slot);
            occupied.Add(slot, candidate.Source);
        }

        Result<IReadOnlyList<ResidentInventorySlotClaimSnapshot>> claimPlan =
            PlanResidentSlotClaimReflow(
                residentId,
                occupied,
                activeCargo,
                activeWeapon);
        if (claimPlan.IsFailure)
        {
            return Result.Failure(claimPlan.Error!);
        }

        ApplyResidentInventoryCompaction(
            residentId,
            tick,
            expansionAssignments,
            pendingUnits);
        ApplyResidentSlotClaimReflow(residentId, claimPlan.Value, tick);

        return Result.Success();
    }

}

}
