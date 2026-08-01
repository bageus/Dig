using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Presentation.Inventory
{

public enum ResidentInventoryItemKind
{
    Generic = 0,
    Tool = 1,
    BuildingBox = 2,
}

public sealed class ResidentInventorySlotViewModel
{
    public ResidentInventorySlotViewModel(
        string stackId,
        string itemId,
        int quantity,
        int reservedQuantity,
        ResidentInventoryItemKind itemKind,
        bool isEquipped = false,
        int heldQuantity = 0,
        ItemInteractionProfile? interactionProfile = null)
    {
        if (string.IsNullOrWhiteSpace(stackId) || string.IsNullOrWhiteSpace(itemId))
        {
            throw new ArgumentException("Inventory slot identifiers are required.");
        }

        if (quantity <= 0
            || reservedQuantity < 0
            || heldQuantity < 0
            || reservedQuantity + heldQuantity > quantity)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (!Enum.IsDefined(typeof(ResidentInventoryItemKind), itemKind))
        {
            throw new ArgumentOutOfRangeException(nameof(itemKind));
        }

        StackId = stackId.Trim();
        ItemId = itemId.Trim();
        Quantity = quantity;
        ReservedQuantity = reservedQuantity;
        HeldQuantity = heldQuantity;
        ItemKind = itemKind;
        IsEquipped = isEquipped || heldQuantity > 0;
        InteractionProfile = interactionProfile ?? ResolveCompatibilityProfile(itemKind);
    }

    public string StackId { get; }
    public string ItemId { get; }
    public int Quantity { get; }
    public int ReservedQuantity { get; }
    public int HeldQuantity { get; }
    public int AvailableQuantity => Quantity - ReservedQuantity - HeldQuantity;
    public ResidentInventoryItemKind ItemKind { get; }
    public bool IsEquipped { get; }
    public ItemInteractionProfile InteractionProfile { get; }
    public bool IsConsumable =>
        InteractionProfile.DirectUseFeedback == ItemInteractionFeedbackKind.Eat;
    public bool IsBuildingBox => ItemKind == ResidentInventoryItemKind.BuildingBox;
    public bool IsTool => ItemKind == ResidentInventoryItemKind.Tool;
    public bool CanPlace => !IsEquipped
        && AvailableQuantity > 0
        && (InteractionProfile.InventoryPrimaryAction
                == ItemInventoryInteractionAction.PlaceItem
            || InteractionProfile.InventoryPrimaryAction
                == ItemInventoryInteractionAction.PlaceBuilding);
    public bool CanStartPlacement => CanPlace
        && InteractionProfile.InventoryPrimaryAction
            == ItemInventoryInteractionAction.PlaceBuilding;
    public bool CanUse => !IsEquipped
        && AvailableQuantity > 0
        && InteractionProfile.SupportsInventoryAction(
            ItemInventoryInteractionAction.DirectUse);
    public bool CanDrop => InteractionProfile.InventoryQuickDropAllowed
        && ReservedQuantity == 0
        && HeldQuantity == 0;

    private static ItemInteractionProfile ResolveCompatibilityProfile(
        ResidentInventoryItemKind itemKind)
    {
        return itemKind switch
        {
            ResidentInventoryItemKind.BuildingBox => ItemInteractionProfiles.BuildingBox,
            ResidentInventoryItemKind.Tool => ItemInteractionProfiles.Tool,
            _ => ItemInteractionProfiles.Generic,
        };
    }
}

public sealed class ResidentInventoryViewModel
{
    public ResidentInventoryViewModel(
        string residentId,
        long inventoryVersion,
        IReadOnlyCollection<ResidentInventorySlotViewModel> slots)
    {
        if (string.IsNullOrWhiteSpace(residentId))
        {
            throw new ArgumentException("Resident id is required.", nameof(residentId));
        }

        if (inventoryVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inventoryVersion));
        }

        ResidentId = residentId.Trim();
        InventoryVersion = inventoryVersion;
        Slots = new ReadOnlyCollection<ResidentInventorySlotViewModel>(
            (slots ?? throw new ArgumentNullException(nameof(slots)))
                .OrderByDescending(slot => slot.IsEquipped)
                .ThenByDescending(slot => slot.IsBuildingBox)
                .ThenByDescending(slot => slot.IsTool)
                .ThenBy(slot => slot.ItemId, StringComparer.Ordinal)
                .ThenBy(slot => slot.StackId, StringComparer.Ordinal)
                .ToArray());
    }

    public string ResidentId { get; }
    public long InventoryVersion { get; }
    public IReadOnlyList<ResidentInventorySlotViewModel> Slots { get; }
}

public sealed class ResidentInventoryPresenter
{
    private readonly ItemCatalog _catalog;

    public ResidentInventoryPresenter(ItemCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public ResidentInventoryViewModel Present(
        InventorySnapshot snapshot,
        EntityId residentId)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (residentId.IsEmpty)
        {
            throw new ArgumentException("Resident id is required.", nameof(residentId));
        }

        HeldItemReferenceSnapshot? held = snapshot.HeldItems
            .Where(item => item.ResidentId == residentId)
            .Select(item => (HeldItemReferenceSnapshot?)item)
            .SingleOrDefault();
        ResidentInventorySlotViewModel[] slots = snapshot.Stacks
            .Where(stack => IsOwnedByResident(stack.Location, residentId))
            .Select(stack => PresentStack(stack, held))
            .ToArray();
        return new ResidentInventoryViewModel(
            residentId.ToString(),
            snapshot.Version,
            slots);
    }

    private ResidentInventorySlotViewModel PresentStack(
        ItemStackSnapshot stack,
        HeldItemReferenceSnapshot? held)
    {
        int heldQuantity = held.HasValue && held.Value.StackId == stack.StackId
            ? held.Value.Quantity
            : 0;
        ItemDefinition definition = _catalog.Get(stack.ItemId);
        return new ResidentInventorySlotViewModel(
            stack.StackId.ToString(),
            stack.ItemId.ToString(),
            stack.Quantity,
            stack.ReservedQuantity,
            ResolveKind(definition),
            isEquipped: heldQuantity > 0
                || stack.Location.Kind == ItemLocationKind.Equipped,
            heldQuantity,
            definition.Interactions);
    }

    private static bool IsOwnedByResident(ItemLocation location, EntityId residentId)
    {
        return location.HasOwner
            && location.OwnerId == residentId
            && (location.Kind == ItemLocationKind.AgentInventory
                || location.Kind == ItemLocationKind.Equipped);
    }

    private static ResidentInventoryItemKind ResolveKind(ItemDefinition definition)
    {
        if (definition.Interactions.InventoryPrimaryAction
            == ItemInventoryInteractionAction.PlaceBuilding)
        {
            return ResidentInventoryItemKind.BuildingBox;
        }

        return definition.IsTool
            ? ResidentInventoryItemKind.Tool
            : ResidentInventoryItemKind.Generic;
    }
}

}
