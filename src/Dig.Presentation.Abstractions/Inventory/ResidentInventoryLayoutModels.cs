using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Presentation.Inventory
{

public enum ResidentInventorySlotVisualKind
{
    Empty = 0,
    Generic = 1,
    Tool = 2,
    BuildingBox = 3,
    CargoExpansion = 4,
    WeaponExpansion = 5,
}

public sealed class ResidentInventoryLayoutSlotViewModel
{
    public ResidentInventoryLayoutSlotViewModel(
        ResidentInventoryCompartment compartment,
        int slotIndex,
        string? stackId,
        string? itemId,
        string displayName,
        int quantity,
        int reservedQuantity,
        int heldQuantity,
        ResidentInventorySlotVisualKind visualKind,
        bool isActiveExpansion)
    {
        if (slotIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex));
        }

        if (quantity < 0
            || reservedQuantity < 0
            || heldQuantity < 0
            || reservedQuantity + heldQuantity > quantity)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        bool empty = string.IsNullOrWhiteSpace(stackId);
        if (empty != string.IsNullOrWhiteSpace(itemId)
            || empty != (visualKind == ResidentInventorySlotVisualKind.Empty))
        {
            throw new ArgumentException("Empty slot identifiers and visual kind are inconsistent.");
        }

        if (empty && (quantity != 0 || reservedQuantity != 0 || heldQuantity != 0))
        {
            throw new ArgumentException("An empty slot cannot contain quantity state.");
        }

        Compartment = compartment;
        SlotIndex = slotIndex;
        StackId = empty ? null : stackId!.Trim();
        ItemId = empty ? null : itemId!.Trim();
        DisplayName = displayName?.Trim() ?? string.Empty;
        Quantity = quantity;
        ReservedQuantity = reservedQuantity;
        HeldQuantity = heldQuantity;
        VisualKind = visualKind;
        IsActiveExpansion = isActiveExpansion;
    }

    public ResidentInventoryCompartment Compartment { get; }

    public int SlotIndex { get; }

    public string? StackId { get; }

    public string? ItemId { get; }

    public string DisplayName { get; }

    public int Quantity { get; }

    public int ReservedQuantity { get; }

    public int HeldQuantity { get; }

    public int AvailableQuantity => Quantity - ReservedQuantity - HeldQuantity;

    public ResidentInventorySlotVisualKind VisualKind { get; }

    public bool IsActiveExpansion { get; }

    public bool IsEmpty => StackId is null;

    public bool IsHeld => HeldQuantity > 0;

    public bool CanDrop => !IsEmpty
        && ReservedQuantity == 0
        && HeldQuantity == 0;

    public bool CanUse => VisualKind == ResidentInventorySlotVisualKind.Tool
        && Quantity == 1
        && AvailableQuantity == 1;

    public bool CanStartPlacement =>
        VisualKind == ResidentInventorySlotVisualKind.BuildingBox
        && Quantity == 1
        && AvailableQuantity == 1;
}

public sealed class ResidentInventoryLayoutViewModel
{
    public ResidentInventoryLayoutViewModel(
        string residentId,
        long inventoryVersion,
        int mainCapacity,
        int cargoCapacity,
        int weaponCapacity,
        double moveSpeedMultiplier,
        IEnumerable<ResidentInventoryLayoutSlotViewModel> slots)
    {
        if (string.IsNullOrWhiteSpace(residentId))
        {
            throw new ArgumentException("Resident id is required.", nameof(residentId));
        }

        if (inventoryVersion < 0
            || mainCapacity != ResidentInventoryLayoutSnapshot.MainSlotCount
            || cargoCapacity < 0
            || weaponCapacity < 0
            || moveSpeedMultiplier <= 0d
            || moveSpeedMultiplier > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(inventoryVersion));
        }

        ResidentId = residentId.Trim();
        InventoryVersion = inventoryVersion;
        MainCapacity = mainCapacity;
        CargoCapacity = cargoCapacity;
        WeaponCapacity = weaponCapacity;
        MoveSpeedMultiplier = moveSpeedMultiplier;
        Slots = new ReadOnlyCollection<ResidentInventoryLayoutSlotViewModel>(
            (slots ?? throw new ArgumentNullException(nameof(slots)))
                .OrderBy(slot => CompartmentOrder(slot.Compartment))
                .ThenBy(slot => slot.SlotIndex)
                .ToArray());
    }

    public string ResidentId { get; }

    public long InventoryVersion { get; }

    public int MainCapacity { get; }

    public int CargoCapacity { get; }

    public int WeaponCapacity { get; }

    public double MoveSpeedMultiplier { get; }

    public IReadOnlyList<ResidentInventoryLayoutSlotViewModel> Slots { get; }

    public IReadOnlyList<ResidentInventoryLayoutSlotViewModel> GetCompartment(
        ResidentInventoryCompartment compartment)
    {
        return new ReadOnlyCollection<ResidentInventoryLayoutSlotViewModel>(
            Slots.Where(slot => slot.Compartment == compartment).ToArray());
    }

    private static int CompartmentOrder(ResidentInventoryCompartment compartment)
    {
        return compartment switch
        {
            ResidentInventoryCompartment.Weapon => 0,
            ResidentInventoryCompartment.Main => 1,
            ResidentInventoryCompartment.Cargo => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(compartment)),
        };
    }
}

public sealed class ResidentInventoryLayoutPresenter
{
    private readonly ItemId _buildingBoxItemId;

    public ResidentInventoryLayoutPresenter(ItemId buildingBoxItemId)
    {
        if (buildingBoxItemId.IsEmpty)
        {
            throw new ArgumentException("BuildingBox item id is required.", nameof(buildingBoxItemId));
        }

        _buildingBoxItemId = buildingBoxItemId;
    }

    public ResidentInventoryLayoutViewModel Present(
        InventoryState inventory,
        EntityId residentId)
    {
        if (inventory is null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        ResidentInventoryLayoutSnapshot layout =
            inventory.GetResidentInventoryLayout(residentId);
        HeldItemReferenceSnapshot? held = inventory.GetHeldItem(residentId);
        ResidentInventoryLayoutSlotViewModel[] slots = layout.Slots
            .Select(slot => PresentSlot(inventory.Catalog, slot, held))
            .ToArray();
        return new ResidentInventoryLayoutViewModel(
            residentId.ToString(),
            inventory.Version,
            layout.MainCapacity,
            layout.CargoCapacity,
            layout.WeaponCapacity,
            inventory.GetResidentMoveSpeedMultiplier(residentId),
            slots);
    }

    private ResidentInventoryLayoutSlotViewModel PresentSlot(
        ItemCatalog catalog,
        ResidentInventorySlotSnapshot slot,
        HeldItemReferenceSnapshot? held)
    {
        if (slot.IsEmpty)
        {
            return new ResidentInventoryLayoutSlotViewModel(
                slot.Slot.Compartment,
                slot.Slot.Index,
                stackId: null,
                itemId: null,
                displayName: string.Empty,
                quantity: 0,
                reservedQuantity: 0,
                heldQuantity: 0,
                ResidentInventorySlotVisualKind.Empty,
                isActiveExpansion: false);
        }

        EntityId stackId = slot.StackId.GetValueOrDefault();
        ItemId itemId = slot.ItemId.GetValueOrDefault();
        if (stackId.IsEmpty || itemId.IsEmpty)
        {
            throw new InvalidOperationException(
                "A nonempty resident inventory slot requires stack and item ids.");
        }

        ItemDefinition definition = catalog.Get(itemId);
        int heldQuantity = held.HasValue && held.Value.StackId == stackId
            ? held.Value.Quantity
            : 0;
        return new ResidentInventoryLayoutSlotViewModel(
            slot.Slot.Compartment,
            slot.Slot.Index,
            stackId.ToString(),
            definition.Id.ToString(),
            definition.DisplayName,
            slot.Quantity,
            slot.ReservedQuantity,
            heldQuantity,
            ResolveVisualKind(definition),
            slot.IsActiveExpansion);
    }

    private ResidentInventorySlotVisualKind ResolveVisualKind(ItemDefinition definition)
    {
        if (definition.Id == _buildingBoxItemId)
        {
            return ResidentInventorySlotVisualKind.BuildingBox;
        }

        if (definition.InventoryExpansion?.Group == InventoryExpansionGroup.Cargo)
        {
            return ResidentInventorySlotVisualKind.CargoExpansion;
        }

        if (definition.InventoryExpansion?.Group == InventoryExpansionGroup.Weapon)
        {
            return ResidentInventorySlotVisualKind.WeaponExpansion;
        }

        return definition.IsTool
            ? ResidentInventorySlotVisualKind.Tool
            : ResidentInventorySlotVisualKind.Generic;
    }
}

}
