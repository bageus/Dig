using System;

namespace Dig.Presentation.Inventory
{

public enum WorldItemInteractionKind
{
    None = 0,
    BuildingBox = 1,
    Pickup = 2,
}

public sealed class WorldItemViewModel
{
    public WorldItemViewModel(
        string stackId,
        string itemId,
        int quantity,
        int reservedQuantity,
        int cellX,
        int cellY,
        WorldItemInteractionKind interactionKind = WorldItemInteractionKind.None)
        : this(
            stackId,
            itemId,
            quantity,
            reservedQuantity,
            cellX,
            cellY,
            cellZ: 0,
            interactionKind)
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
        WorldItemInteractionKind interactionKind = WorldItemInteractionKind.None)
    {
        if (!Enum.IsDefined(typeof(WorldItemInteractionKind), interactionKind))
        {
            throw new ArgumentOutOfRangeException(nameof(interactionKind));
        }

        if (cellZ < 0 || cellZ > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(cellZ));
        }

        StackId = stackId;
        ItemId = itemId;
        Quantity = quantity;
        ReservedQuantity = reservedQuantity;
        CellX = cellX;
        CellY = cellY;
        CellZ = cellZ;
        InteractionKind = interactionKind;
    }

    public string StackId { get; }
    public string ItemId { get; }
    public int Quantity { get; }
    public int ReservedQuantity { get; }
    public int AvailableQuantity => Quantity - ReservedQuantity;
    public int CellX { get; }
    public int CellY { get; }
    public int CellZ { get; }
    public WorldItemInteractionKind InteractionKind { get; }
    public bool IsBuildingBox => InteractionKind == WorldItemInteractionKind.BuildingBox;
    public bool CanPickup => InteractionKind == WorldItemInteractionKind.Pickup
        && Quantity > 0
        && ReservedQuantity == 0;
    public bool IsInteractive => IsBuildingBox || InteractionKind == WorldItemInteractionKind.Pickup;
}

}
