using System;

namespace Dig.Presentation.Inventory
{
public sealed class WorldItemMemoryViewModel
{
    public WorldItemMemoryViewModel(
        string stackId, string itemId, int cellX, int cellY, int cellZ, long observedTick)
    {
        if (string.IsNullOrWhiteSpace(stackId) || string.IsNullOrWhiteSpace(itemId))
            throw new ArgumentException("World item memory identifiers are required.");
        if (observedTick < 0) throw new ArgumentOutOfRangeException(nameof(observedTick));
        StackId = stackId; ItemId = itemId; CellX = cellX; CellY = cellY;
        CellZ = cellZ; ObservedTick = observedTick;
    }
    public string StackId { get; }
    public string ItemId { get; }
    public int CellX { get; }
    public int CellY { get; }
    public int CellZ { get; }
    public long ObservedTick { get; }
}
}
