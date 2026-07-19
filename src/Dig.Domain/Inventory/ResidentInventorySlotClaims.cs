using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public readonly struct ResidentInventorySlotClaimSnapshot
{
    public ResidentInventorySlotClaimSnapshot(
        EntityId jobId,
        EntityId residentId,
        ItemId itemId,
        ResidentInventorySlot slot,
        int quantity)
    {
        if (jobId.IsEmpty || residentId.IsEmpty || itemId.IsEmpty)
        {
            throw new ArgumentException("Claim identifiers are required.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        JobId = jobId;
        ResidentId = residentId;
        ItemId = itemId;
        Slot = slot;
        Quantity = quantity;
    }

    public EntityId JobId { get; }

    public EntityId ResidentId { get; }

    public ItemId ItemId { get; }

    public ResidentInventorySlot Slot { get; }

    public int Quantity { get; }
}

public sealed partial class InventoryState
{
    private readonly List<ResidentInventorySlotClaimSnapshot> _residentSlotClaims =
        new List<ResidentInventorySlotClaimSnapshot>();

    public Result<IReadOnlyList<ResidentInventorySlotClaimSnapshot>>
        ReserveResidentSlotCapacity(
            EntityId jobId,
            EntityId residentId,
            ItemId itemId,
            int quantity,
            long tick)
    {
        ValidateTick(tick);
        ValidateJobId(jobId);
        ValidateResidentId(residentId);
        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Item id is required.", nameof(itemId));
        }

        if (quantity <= 0)
        {
            return Result<IReadOnlyList<ResidentInventorySlotClaimSnapshot>>.Failure(
                InventoryErrors.InvalidQuantity);
        }

        IReadOnlyList<ResidentInventorySlotClaimSnapshot> existing =
            GetResidentSlotClaims(jobId);
        if (existing.Count > 0)
        {
            bool same = existing.All(claim =>
                    claim.ResidentId == residentId && claim.ItemId == itemId)
                && existing.Sum(claim => claim.Quantity) == quantity;
            return same
                ? Result<IReadOnlyList<ResidentInventorySlotClaimSnapshot>>.Success(existing)
                : Result<IReadOnlyList<ResidentInventorySlotClaimSnapshot>>.Failure(
                    InventoryErrors.ResidentSlotClaimConflict);
        }

        Result normalized = NormalizeResidentInventory(residentId, tick);
        if (normalized.IsFailure)
        {
            return Result<IReadOnlyList<ResidentInventorySlotClaimSnapshot>>.Failure(
                normalized.Error!);
        }

        ItemDefinition definition = Catalog.Get(itemId);
        ResidentInventoryLayoutSnapshot layout = GetResidentInventoryLayout(residentId);
        Dictionary<ResidentInventorySlot, ItemStackState> occupied =
            CreateSlottedOccupancy(residentId);
        List<SlotCapacity> capacities = BuildClaimCapacities(
            residentId,
            definition,
            layout,
            occupied);
        int remaining = quantity;
        List<ResidentInventorySlotClaimSnapshot> planned =
            new List<ResidentInventorySlotClaimSnapshot>();
        for (int index = 0; index < capacities.Count && remaining > 0; index++)
        {
            SlotCapacity capacity = capacities[index];
            int claimed = Math.Min(remaining, capacity.AvailableQuantity);
            if (claimed <= 0)
            {
                continue;
            }

            planned.Add(new ResidentInventorySlotClaimSnapshot(
                jobId,
                residentId,
                itemId,
                capacity.Slot,
                claimed));
            remaining -= claimed;
        }

        if (remaining > 0)
        {
            return Result<IReadOnlyList<ResidentInventorySlotClaimSnapshot>>.Failure(
                InventoryErrors.ResidentInventoryCapacityExceeded);
        }

        _residentSlotClaims.AddRange(planned);
        IncrementVersion();
        for (int index = 0; index < planned.Count; index++)
        {
            ResidentInventorySlotClaimSnapshot claim = planned[index];
            Raise(new ResidentInventorySlotClaimChanged(
                tick,
                claim.JobId,
                claim.ResidentId,
                claim.ItemId,
                claim.Slot,
                claim.Quantity));
        }

        return Result<IReadOnlyList<ResidentInventorySlotClaimSnapshot>>.Success(
            new ReadOnlyCollection<ResidentInventorySlotClaimSnapshot>(planned));
    }

    public IReadOnlyList<ResidentInventorySlotClaimSnapshot> GetResidentSlotClaims(
        EntityId jobId)
    {
        ValidateJobId(jobId);
        return new ReadOnlyCollection<ResidentInventorySlotClaimSnapshot>(
            _residentSlotClaims
                .Where(claim => claim.JobId == jobId)
                .OrderBy(claim => claim.Slot.Compartment)
                .ThenBy(claim => claim.Slot.Index)
                .ToArray());
    }

    public IReadOnlyList<ResidentInventorySlotClaimSnapshot> GetResidentSlotClaims()
    {
        return new ReadOnlyCollection<ResidentInventorySlotClaimSnapshot>(
            _residentSlotClaims
                .OrderBy(claim => claim.JobId.ToString(), StringComparer.Ordinal)
                .ThenBy(claim => claim.Slot.Compartment)
                .ThenBy(claim => claim.Slot.Index)
                .ToArray());
    }

    public int ReleaseResidentSlotClaims(EntityId jobId, long tick)
    {
        ValidateTick(tick);
        ValidateJobId(jobId);
        ResidentInventorySlotClaimSnapshot[] released = _residentSlotClaims
            .Where(claim => claim.JobId == jobId)
            .ToArray();
        if (released.Length == 0)
        {
            return 0;
        }

        _residentSlotClaims.RemoveAll(claim => claim.JobId == jobId);
        IncrementVersion();
        for (int index = 0; index < released.Length; index++)
        {
            ResidentInventorySlotClaimSnapshot claim = released[index];
            Raise(new ResidentInventorySlotClaimChanged(
                tick,
                claim.JobId,
                claim.ResidentId,
                claim.ItemId,
                claim.Slot,
                quantity: 0));
        }

        return released.Sum(claim => claim.Quantity);
    }

    private List<SlotCapacity> BuildClaimCapacities(
        EntityId residentId,
        ItemDefinition definition,
        ResidentInventoryLayoutSnapshot layout,
        IReadOnlyDictionary<ResidentInventorySlot, ItemStackState> occupied)
    {
        List<SlotCapacity> capacities = new List<SlotCapacity>();
        for (int index = 0; index < layout.Slots.Count; index++)
        {
            ResidentInventorySlotSnapshot snapshot = layout.Slots[index];
            ResidentInventorySlot slot = snapshot.Slot;
            if (!CanClaimSlot(definition, slot, layout))
            {
                continue;
            }

            ResidentInventorySlotClaimSnapshot[] claims = _residentSlotClaims
                .Where(claim => claim.ResidentId == residentId && claim.Slot == slot)
                .ToArray();
            if (occupied.TryGetValue(slot, out ItemStackState? stack))
            {
                if (stack.ItemId != definition.Id)
                {
                    continue;
                }

                int claimed = claims.Sum(claim => claim.Quantity);
                int available = definition.MaximumStackSize - stack.Quantity - claimed;
                if (available > 0)
                {
                    capacities.Add(new SlotCapacity(slot, available, mergeRank: 0));
                }

                continue;
            }

            if (claims.Any(claim => claim.ItemId != definition.Id))
            {
                continue;
            }

            int reserved = claims.Sum(claim => claim.Quantity);
            int emptyCapacity = definition.MaximumStackSize - reserved;
            if (emptyCapacity > 0)
            {
                capacities.Add(new SlotCapacity(
                    slot,
                    emptyCapacity,
                    EmptySlotRank(slot.Compartment)));
            }
        }

        return capacities
            .OrderBy(capacity => capacity.Rank)
            .ThenBy(capacity => capacity.Slot.Compartment)
            .ThenBy(capacity => capacity.Slot.Index)
            .ToList();
    }

    private static bool CanClaimSlot(
        ItemDefinition definition,
        ResidentInventorySlot slot,
        ResidentInventoryLayoutSnapshot layout)
    {
        if (definition.IsInventoryExpansion)
        {
            return slot.Compartment == ResidentInventoryCompartment.Main;
        }

        if (slot.Compartment == ResidentInventoryCompartment.Main)
        {
            return true;
        }

        ActiveInventoryExpansionSnapshot? active = slot.Compartment switch
        {
            ResidentInventoryCompartment.Cargo => layout.ActiveCargoExpansion,
            ResidentInventoryCompartment.Weapon => layout.ActiveWeaponExpansion,
            _ => null,
        };
        return active.HasValue && active.Value.Definition.Accepts(definition);
    }

    private static int EmptySlotRank(ResidentInventoryCompartment compartment)
    {
        return compartment switch
        {
            ResidentInventoryCompartment.Cargo => 1,
            ResidentInventoryCompartment.Weapon => 1,
            ResidentInventoryCompartment.Main => 2,
            _ => 3,
        };
    }

    private readonly struct SlotCapacity
    {
        public SlotCapacity(
            ResidentInventorySlot slot,
            int availableQuantity,
            int mergeRank)
        {
            Slot = slot;
            AvailableQuantity = availableQuantity;
            Rank = mergeRank;
        }

        public ResidentInventorySlot Slot { get; }

        public int AvailableQuantity { get; }

        public int Rank { get; }
    }
}

}