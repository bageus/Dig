using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Presentation.Inventory
{

public sealed class ResidentInventorySlotDiagnosticViewModel
{
    public ResidentInventorySlotDiagnosticViewModel(
        ResidentInventoryCompartment compartment,
        int slotIndex,
        string? stackId,
        string? itemId,
        int quantity,
        int reservedQuantity,
        int heldQuantity,
        int incomingClaimQuantity,
        IReadOnlyCollection<string> claimJobIds)
    {
        if (slotIndex < 0
            || quantity < 0
            || reservedQuantity < 0
            || heldQuantity < 0
            || incomingClaimQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex));
        }

        Compartment = compartment;
        SlotIndex = slotIndex;
        StackId = Normalize(stackId);
        ItemId = Normalize(itemId);
        Quantity = quantity;
        ReservedQuantity = reservedQuantity;
        HeldQuantity = heldQuantity;
        IncomingClaimQuantity = incomingClaimQuantity;
        ClaimJobIds = new ReadOnlyCollection<string>(
            (claimJobIds ?? throw new ArgumentNullException(nameof(claimJobIds)))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray());
    }

    public ResidentInventoryCompartment Compartment { get; }
    public int SlotIndex { get; }
    public string? StackId { get; }
    public string? ItemId { get; }
    public int Quantity { get; }
    public int ReservedQuantity { get; }
    public int HeldQuantity { get; }
    public int IncomingClaimQuantity { get; }
    public IReadOnlyList<string> ClaimJobIds { get; }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed class ResidentInventoryDiagnosticViewModel
{
    public ResidentInventoryDiagnosticViewModel(
        string residentId,
        long inventoryVersion,
        int mainCapacity,
        int cargoCapacity,
        int weaponCapacity,
        string? activeCargoExpansionItemId,
        string? activeWeaponExpansionItemId,
        string? heldStackId,
        double moveSpeedMultiplier,
        IReadOnlyCollection<ResidentInventorySlotDiagnosticViewModel> slots)
    {
        ResidentId = string.IsNullOrWhiteSpace(residentId)
            ? throw new ArgumentException("Resident id is required.", nameof(residentId))
            : residentId.Trim();
        if (inventoryVersion < 0
            || mainCapacity != ResidentInventoryLayoutSnapshot.MainSlotCount
            || cargoCapacity < 0
            || weaponCapacity < 0
            || moveSpeedMultiplier <= 0d
            || moveSpeedMultiplier > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(inventoryVersion));
        }

        InventoryVersion = inventoryVersion;
        MainCapacity = mainCapacity;
        CargoCapacity = cargoCapacity;
        WeaponCapacity = weaponCapacity;
        ActiveCargoExpansionItemId = Normalize(activeCargoExpansionItemId);
        ActiveWeaponExpansionItemId = Normalize(activeWeaponExpansionItemId);
        HeldStackId = Normalize(heldStackId);
        MoveSpeedMultiplier = moveSpeedMultiplier;
        Slots = new ReadOnlyCollection<ResidentInventorySlotDiagnosticViewModel>(
            (slots ?? throw new ArgumentNullException(nameof(slots)))
                .OrderBy(slot => slot.Compartment)
                .ThenBy(slot => slot.SlotIndex)
                .ToArray());
    }

    public string ResidentId { get; }
    public long InventoryVersion { get; }
    public int MainCapacity { get; }
    public int CargoCapacity { get; }
    public int WeaponCapacity { get; }
    public string? ActiveCargoExpansionItemId { get; }
    public string? ActiveWeaponExpansionItemId { get; }
    public string? HeldStackId { get; }
    public double MoveSpeedMultiplier { get; }
    public IReadOnlyList<ResidentInventorySlotDiagnosticViewModel> Slots { get; }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed class ResidentInventoryDiagnosticsPresenter
{
    public ResidentInventoryDiagnosticViewModel Present(
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
        IReadOnlyList<ResidentInventorySlotClaimSnapshot> claims =
            inventory.GetResidentSlotClaims()
                .Where(claim => claim.ResidentId == residentId)
                .ToArray();
        ResidentInventorySlotDiagnosticViewModel[] slots = layout.Slots
            .Select(slot => PresentSlot(slot, held, claims))
            .ToArray();
        return new ResidentInventoryDiagnosticViewModel(
            residentId.ToString(),
            inventory.Version,
            layout.MainCapacity,
            layout.CargoCapacity,
            layout.WeaponCapacity,
            layout.ActiveCargoExpansion?.ItemId.ToString(),
            layout.ActiveWeaponExpansion?.ItemId.ToString(),
            held?.StackId.ToString(),
            inventory.GetResidentMoveSpeedMultiplier(residentId),
            slots);
    }

    private static ResidentInventorySlotDiagnosticViewModel PresentSlot(
        ResidentInventorySlotSnapshot slot,
        HeldItemReferenceSnapshot? held,
        IReadOnlyCollection<ResidentInventorySlotClaimSnapshot> claims)
    {
        ResidentInventorySlotClaimSnapshot[] slotClaims = claims
            .Where(claim => claim.Slot == slot.Slot)
            .ToArray();
        int heldQuantity = held.HasValue && slot.StackId == held.Value.StackId
            ? held.Value.Quantity
            : 0;
        return new ResidentInventorySlotDiagnosticViewModel(
            slot.Slot.Compartment,
            slot.Slot.Index,
            slot.StackId?.ToString(),
            slot.ItemId?.ToString(),
            slot.Quantity,
            slot.ReservedQuantity,
            heldQuantity,
            slotClaims.Sum(claim => claim.Quantity),
            slotClaims.Select(claim => claim.JobId.ToString()).ToArray());
    }
}

}