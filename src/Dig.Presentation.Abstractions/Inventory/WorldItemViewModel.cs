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
        int cellY)
    {
        StackId = stackId;
        ItemId = itemId;
        Quantity = quantity;
        ReservedQuantity = reservedQuantity;
        CellX = cellX;
        CellY = cellY;
    }

    public string StackId { get; }
    public string ItemId { get; }
    public int Quantity { get; }
    public int ReservedQuantity { get; }
    public int AvailableQuantity => Quantity - ReservedQuantity;
    public int CellX { get; }
    public int CellY { get; }
}

}
