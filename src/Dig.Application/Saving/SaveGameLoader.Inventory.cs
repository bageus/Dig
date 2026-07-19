using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameLoader
{
    private static InventorySnapshot BuildInventorySnapshot(InventorySaveData data)
    {
        if (data is null || data.Stacks is null)
        {
            throw new InvalidOperationException("Inventory save data is missing.");
        }

        List<HeldItemReferenceSnapshot> heldItems = ParseHeldItems(data.HeldItems);
        Dictionary<EntityId, int> heldByStack = heldItems
            .GroupBy(item => item.StackId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        List<ItemStackSnapshot> stacks = new List<ItemStackSnapshot>();
        foreach (ItemStackSaveData savedStack in data.Stacks
            .OrderBy(item => item.StackId, StringComparer.Ordinal))
        {
            if (savedStack is null || savedStack.Reservations is null)
            {
                throw new InvalidOperationException("Inventory stack save data is missing.");
            }

            EntityId stackId = EntityId.Parse(savedStack.StackId);
            List<ItemQuantityReservationSnapshot> reservations = savedStack.Reservations
                .OrderBy(item => item.JobId, StringComparer.Ordinal)
                .Select(item => new ItemQuantityReservationSnapshot(
                    EntityId.Parse(item.JobId),
                    item.Quantity))
                .ToList();
            heldByStack.TryGetValue(stackId, out int heldQuantity);
            stacks.Add(new ItemStackSnapshot(
                stackId,
                new ItemId(savedStack.ItemId),
                savedStack.Quantity,
                ParseLocation(savedStack.Location),
                reservations,
                heldQuantity));
        }

        ResidentInventorySlotClaimSnapshot[] claims = (data.ResidentSlotClaims
                ?? new List<ResidentSlotClaimSaveData>())
            .OrderBy(value => value.JobId, StringComparer.Ordinal)
            .ThenBy(value => value.Compartment)
            .ThenBy(value => value.SlotIndex)
            .Select(ParseResidentSlotClaim)
            .ToArray();
        return new InventorySnapshot(data.Version, stacks, heldItems, claims);
    }

    private static List<HeldItemReferenceSnapshot> ParseHeldItems(
        IReadOnlyCollection<HeldItemReferenceSaveData>? savedItems)
    {
        List<HeldItemReferenceSnapshot> heldItems =
            new List<HeldItemReferenceSnapshot>();
        if (savedItems is null)
        {
            return heldItems;
        }

        foreach (HeldItemReferenceSaveData saved in savedItems
            .OrderBy(item => item.ResidentId, StringComparer.Ordinal))
        {
            if (saved is null
                || !Enum.IsDefined(typeof(HeldItemPurpose), saved.Purpose))
            {
                throw new InvalidOperationException("Held item save data is invalid.");
            }

            heldItems.Add(new HeldItemReferenceSnapshot(
                EntityId.Parse(saved.ResidentId),
                EntityId.Parse(saved.StackId),
                saved.Quantity,
                (HeldItemPurpose)saved.Purpose));
        }

        return heldItems;
    }
}

}