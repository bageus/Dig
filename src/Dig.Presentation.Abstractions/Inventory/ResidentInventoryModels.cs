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
    BuildingBox = 1,
}

public sealed class ResidentInventorySlotViewModel
{
    public ResidentInventorySlotViewModel(
        string stackId,
        string itemId,
        int quantity,
        int reservedQuantity,
        ResidentInventoryItemKind itemKind)
    {
        if (string.IsNullOrWhiteSpace(stackId) || string.IsNullOrWhiteSpace(itemId))
        {
            throw new ArgumentException("Inventory slot identifiers are required.");
        }

        if (quantity <= 0 || reservedQuantity < 0 || reservedQuantity > quantity)
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
        ItemKind = itemKind;
    }

    public string StackId { get; }
    public string ItemId { get; }
    public int Quantity { get; }
    public int ReservedQuantity { get; }
    public int AvailableQuantity => Quantity - ReservedQuantity;
    public ResidentInventoryItemKind ItemKind { get; }
    public bool IsBuildingBox => ItemKind == ResidentInventoryItemKind.BuildingBox;
    public bool CanStartPlacement => IsBuildingBox
        && Quantity == 1
        && AvailableQuantity == 1;
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
                .OrderByDescending(slot => slot.IsBuildingBox)
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
    private readonly ItemId _buildingBoxItemId;

    public ResidentInventoryPresenter(ItemId buildingBoxItemId)
    {
        if (buildingBoxItemId.IsEmpty)
        {
            throw new ArgumentException("BuildingBox item id is required.", nameof(buildingBoxItemId));
        }

        _buildingBoxItemId = buildingBoxItemId;
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

        ResidentInventorySlotViewModel[] slots = snapshot.Stacks
            .Where(stack => stack.Location.Kind == ItemLocationKind.AgentInventory
                && stack.Location.HasOwner
                && stack.Location.OwnerId == residentId)
            .Select(stack => new ResidentInventorySlotViewModel(
                stack.StackId.ToString(),
                stack.ItemId.ToString(),
                stack.Quantity,
                stack.ReservedQuantity,
                stack.ItemId == _buildingBoxItemId
                    ? ResidentInventoryItemKind.BuildingBox
                    : ResidentInventoryItemKind.Generic))
            .ToArray();
        return new ResidentInventoryViewModel(
            residentId.ToString(),
            snapshot.Version,
            slots);
    }
}
}
