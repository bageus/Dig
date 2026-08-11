using System;

namespace Dig.Domain.Inventory
{

public static class ItemPickupQuantityPolicy
{
    public static int ResolveRequestedQuantity(ItemStackSnapshot stack)
    {
        if (stack is null)
        {
            throw new ArgumentNullException(nameof(stack));
        }

        return Math.Min(1, stack.AvailableQuantity);
    }
}

}
