using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    private Result<IReadOnlyList<ResidentInventorySlotClaimSnapshot>>
        PlanResidentSlotClaimReflow(
            EntityId residentId,
            IReadOnlyDictionary<ResidentInventorySlot, ItemStackState> occupied,
            ActiveInventoryExpansionSnapshot? activeCargo,
            ActiveInventoryExpansionSnapshot? activeWeapon)
    {
        ResidentInventorySlotClaimSnapshot[] claims = _residentSlotClaims
            .Where(value => value.ResidentId == residentId)
            .OrderBy(value => ResolveClaimSourceRank(
                value,
                activeCargo,
                activeWeapon))
            .ThenBy(value => value.Slot.Index)
            .ThenBy(value => value.JobId.ToString(), StringComparer.Ordinal)
            .ThenBy(value => value.ItemId.ToString(), StringComparer.Ordinal)
            .ToArray();
        if (claims.Length == 0)
        {
            return Result<IReadOnlyList<ResidentInventorySlotClaimSnapshot>>.Success(
                Array.Empty<ResidentInventorySlotClaimSnapshot>());
        }

        HashSet<ResidentInventorySlot> unavailable = occupied.Keys.ToHashSet();
        List<ResidentInventorySlotClaimSnapshot> planned =
            new List<ResidentInventorySlotClaimSnapshot>(claims.Length);
        for (int index = 0; index < claims.Length; index++)
        {
            ResidentInventorySlotClaimSnapshot claim = claims[index];
            ItemDefinition definition = Catalog.Get(claim.ItemId);
            if (claim.Quantity <= 0
                || claim.Quantity > definition.MaximumStackSize)
            {
                return Result<IReadOnlyList<ResidentInventorySlotClaimSnapshot>>.Failure(
                    InventoryErrors.ResidentSlotClaimStale);
            }

            if (!TryPlanResidentClaimSlot(
                    definition,
                    activeCargo,
                    activeWeapon,
                    unavailable,
                    out ResidentInventorySlot slot))
            {
                return Result<IReadOnlyList<ResidentInventorySlotClaimSnapshot>>.Failure(
                    InventoryErrors.ResidentInventoryCapacityExceeded);
            }

            unavailable.Add(slot);
            planned.Add(new ResidentInventorySlotClaimSnapshot(
                claim.JobId,
                claim.ResidentId,
                claim.ItemId,
                slot,
                claim.Quantity));
        }

        return Result<IReadOnlyList<ResidentInventorySlotClaimSnapshot>>.Success(
            planned.ToArray());
    }

    private bool TryPlanResidentClaimSlot(
        ItemDefinition definition,
        ActiveInventoryExpansionSnapshot? activeCargo,
        ActiveInventoryExpansionSnapshot? activeWeapon,
        ISet<ResidentInventorySlot> unavailable,
        out ResidentInventorySlot slot)
    {
        if (definition.IsInventoryExpansion)
        {
            return TryTakeResidentClaimSlot(
                ResidentInventoryCompartment.Main,
                ResidentInventoryLayoutSnapshot.MainSlotCount,
                unavailable,
                out slot);
        }

        bool prefersWeapon = activeWeapon.HasValue
            && activeWeapon.Value.Definition.Accepts(definition);
        if (prefersWeapon
            && TryTakeResidentClaimSlot(
                ResidentInventoryCompartment.Weapon,
                activeWeapon!.Value.Definition.AddedSlots,
                unavailable,
                out slot))
        {
            return true;
        }

        if (TryTakeResidentClaimSlot(
                ResidentInventoryCompartment.Main,
                ResidentInventoryLayoutSnapshot.MainSlotCount,
                unavailable,
                out slot))
        {
            return true;
        }

        if (activeCargo.HasValue
            && activeCargo.Value.Definition.Accepts(definition)
            && TryTakeResidentClaimSlot(
                ResidentInventoryCompartment.Cargo,
                activeCargo.Value.Definition.AddedSlots,
                unavailable,
                out slot))
        {
            return true;
        }

        slot = default;
        return false;
    }

    private static bool TryTakeResidentClaimSlot(
        ResidentInventoryCompartment compartment,
        int capacity,
        ISet<ResidentInventorySlot> unavailable,
        out ResidentInventorySlot slot)
    {
        for (int index = 0; index < capacity; index++)
        {
            ResidentInventorySlot candidate = new ResidentInventorySlot(
                compartment,
                index);
            if (!unavailable.Contains(candidate))
            {
                slot = candidate;
                return true;
            }
        }

        slot = default;
        return false;
    }

    private int ResolveClaimSourceRank(
        ResidentInventorySlotClaimSnapshot claim,
        ActiveInventoryExpansionSnapshot? activeCargo,
        ActiveInventoryExpansionSnapshot? activeWeapon)
    {
        ItemDefinition definition = Catalog.Get(claim.ItemId);
        if (definition.IsInventoryExpansion)
        {
            return claim.Slot.Compartment == ResidentInventoryCompartment.Main
                ? 0
                : 3;
        }

        bool prefersWeapon = activeWeapon.HasValue
            && activeWeapon.Value.Definition.Accepts(definition);
        if (prefersWeapon)
        {
            return claim.Slot.Compartment switch
            {
                ResidentInventoryCompartment.Weapon => 0,
                ResidentInventoryCompartment.Main => 1,
                ResidentInventoryCompartment.Cargo => 2,
                _ => 3,
            };
        }

        return claim.Slot.Compartment switch
        {
            ResidentInventoryCompartment.Main => 0,
            ResidentInventoryCompartment.Cargo => activeCargo.HasValue
                && activeCargo.Value.Definition.Accepts(definition) ? 1 : 3,
            _ => 3,
        };
    }

    private void ApplyResidentSlotClaimReflow(
        EntityId residentId,
        IReadOnlyList<ResidentInventorySlotClaimSnapshot> planned,
        long tick)
    {
        ResidentInventorySlotClaimSnapshot[] existing = _residentSlotClaims
            .Where(value => value.ResidentId == residentId)
            .OrderBy(value => value.JobId.ToString(), StringComparer.Ordinal)
            .ThenBy(value => value.Slot.Compartment)
            .ThenBy(value => value.Slot.Index)
            .ToArray();
        ResidentInventorySlotClaimSnapshot[] normalized = planned
            .OrderBy(value => value.JobId.ToString(), StringComparer.Ordinal)
            .ThenBy(value => value.Slot.Compartment)
            .ThenBy(value => value.Slot.Index)
            .ToArray();
        if (ClaimsMatch(existing, normalized))
        {
            return;
        }

        ResidentInventorySlotClaimSnapshot[] removed = existing
            .Where(value => !normalized.Any(candidate => SameClaim(value, candidate)))
            .ToArray();
        ResidentInventorySlotClaimSnapshot[] added = normalized
            .Where(value => !existing.Any(candidate => SameClaim(value, candidate)))
            .ToArray();

        _residentSlotClaims.RemoveAll(value => value.ResidentId == residentId);
        _residentSlotClaims.AddRange(normalized);
        IncrementVersion();

        for (int index = 0; index < removed.Length; index++)
        {
            ResidentInventorySlotClaimSnapshot claim = removed[index];
            Raise(new ResidentInventorySlotClaimChanged(
                tick,
                claim.JobId,
                claim.ResidentId,
                claim.ItemId,
                claim.Slot,
                quantity: 0));
        }

        for (int index = 0; index < added.Length; index++)
        {
            ResidentInventorySlotClaimSnapshot claim = added[index];
            Raise(new ResidentInventorySlotClaimChanged(
                tick,
                claim.JobId,
                claim.ResidentId,
                claim.ItemId,
                claim.Slot,
                claim.Quantity));
        }
    }

    private static bool ClaimsMatch(
        IReadOnlyList<ResidentInventorySlotClaimSnapshot> left,
        IReadOnlyList<ResidentInventorySlotClaimSnapshot> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (int index = 0; index < left.Count; index++)
        {
            if (!SameClaim(left[index], right[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool SameClaim(
        ResidentInventorySlotClaimSnapshot left,
        ResidentInventorySlotClaimSnapshot right)
    {
        return left.JobId == right.JobId
            && left.ResidentId == right.ResidentId
            && left.ItemId == right.ItemId
            && left.Slot == right.Slot
            && left.Quantity == right.Quantity;
    }
}

}
