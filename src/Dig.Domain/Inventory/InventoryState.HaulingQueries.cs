using Dig.Domain.Core;

namespace Dig.Domain.Inventory;

public sealed partial class InventoryState
{
    public IReadOnlyList<ItemStackSnapshot> GetAvailableWorldStacks()
    {
        List<ItemStackState>? selected = null;
        foreach (ItemStackState stack in _stacks.Values)
        {
            if (stack.Location.Kind != ItemLocationKind.World
                || stack.AvailableQuantity <= 0)
            {
                continue;
            }

            selected ??= new List<ItemStackState>();
            selected.Add(stack);
        }

        if (selected is null)
        {
            return Array.Empty<ItemStackSnapshot>();
        }

        selected.Sort(CompareWorldStacks);
        ItemStackSnapshot[] snapshots = new ItemStackSnapshot[selected.Count];
        for (int index = 0; index < selected.Count; index++)
        {
            snapshots[index] = selected[index].CreateSnapshot();
        }

        return snapshots;
    }

    public int GetTotalQuantityAt(ItemLocation location)
    {
        int quantity = 0;
        foreach (ItemStackState stack in _stacks.Values)
        {
            if (stack.Location == location)
            {
                quantity = checked(quantity + stack.Quantity);
            }
        }

        return quantity;
    }

    private static int CompareWorldStacks(ItemStackState left, ItemStackState right)
    {
        int locationComparison = left.Location.CompareTo(right.Location);
        if (locationComparison != 0)
        {
            return locationComparison;
        }

        return string.Compare(
            left.Id.ToString(),
            right.Id.ToString(),
            StringComparison.Ordinal);
    }
}
