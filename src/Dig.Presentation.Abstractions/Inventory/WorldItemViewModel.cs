using System;
using Dig.Domain.Inventory;

namespace Dig.Presentation.Inventory
{

public sealed class WorldItemViewModel
{
    public WorldItemViewModel(
        string stackId,
        string itemId,
        int quantity,
        int reservedQuantity,
        int cellX,
        int cellY,
        ItemInteractionProfile interactionProfile,
        string? displayName = null)
        : this(
            stackId,
            itemId,
            quantity,
            reservedQuantity,
            cellX,
            cellY,
            cellZ: 0,
            interactionProfile: interactionProfile,
            displayName: displayName)
    {
    }

    public WorldItemViewModel(
        string stackId,
        string itemId,
        int quantity,
        int reservedQuantity,
        int cellX,
        int cellY,
        int cellZ,
        ItemInteractionProfile interactionProfile,
        string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(stackId)
            || string.IsNullOrWhiteSpace(itemId))
        {
            throw new ArgumentException("World item identifiers are required.");
        }

        if (quantity <= 0
            || reservedQuantity < 0
            || reservedQuantity > quantity)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (cellZ < 0 || cellZ > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(cellZ));
        }

        StackId = stackId.Trim();
        ItemId = itemId.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? ItemId
            : displayName.Trim();
        Quantity = quantity;
        ReservedQuantity = reservedQuantity;
        CellX = cellX;
        CellY = cellY;
        CellZ = cellZ;
        InteractionProfile = interactionProfile
            ?? throw new ArgumentNullException(nameof(interactionProfile));
    }

    public string StackId { get; }
    public string ItemId { get; }
    public string DisplayName { get; }
    public int Quantity { get; }
    public int ReservedQuantity { get; }
    public int AvailableQuantity => Quantity - ReservedQuantity;
    public int CellX { get; }
    public int CellY { get; }
    public int CellZ { get; }
    public ItemInteractionProfile InteractionProfile { get; }

    public bool IsBuildingBox =>
        InteractionProfile.WorldPrimaryAction
            == ItemWorldInteractionAction.SelectBuildingBox;

    public bool CanPickup => AvailableQuantity > 0
        && InteractionProfile.SupportsWorldAction(ItemWorldInteractionAction.Pickup);

    public bool CanUse => AvailableQuantity > 0
        && (InteractionProfile.SupportsWorldAction(
                ItemWorldInteractionAction.DirectUse)
            || InteractionProfile.SupportsWorldAction(
                ItemWorldInteractionAction.UseProductionPackage));

    public bool IsInteractive =>
        InteractionProfile.WorldPrimaryAction != ItemWorldInteractionAction.None
        || InteractionProfile.WorldAltAction != ItemWorldInteractionAction.None;

    public ItemWorldInteractionAction ResolveWorldAction(bool altPressed)
    {
        return InteractionProfile.ResolveWorldAction(altPressed);
    }

    public bool IsActionAvailable(ItemWorldInteractionAction action)
    {
        if (action == ItemWorldInteractionAction.SelectBuildingBox)
        {
            return Quantity == 1 && AvailableQuantity == 1;
        }

        return action != ItemWorldInteractionAction.None
            && AvailableQuantity > 0
            && InteractionProfile.SupportsWorldAction(action);
    }
}

}
