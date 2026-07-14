using System;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    public bool HasAvailableCategory(
        ItemCategoryId categoryId,
        EntityId reservationOwnerId)
    {
        ValidateCategoryId(categoryId);
        if (reservationOwnerId.IsEmpty)
        {
            throw new ArgumentException(
                "Reservation owner id cannot be empty.",
                nameof(reservationOwnerId));
        }

        foreach (ItemStackState stack in _stacks.Values)
        {
            if (!Catalog.Get(stack.ItemId).HasCategory(categoryId))
            {
                continue;
            }

            if (stack.AvailableQuantity > 0
                || stack.GetReservedQuantity(reservationOwnerId) > 0)
            {
                return true;
            }
        }

        return false;
    }

    public EntityId? FindFirstAvailableStackId(ItemCategoryId categoryId)
    {
        ValidateCategoryId(categoryId);
        ItemStackState? selected = null;
        foreach (ItemStackState stack in _stacks.Values)
        {
            if (stack.AvailableQuantity <= 0
                || !Catalog.Get(stack.ItemId).HasCategory(categoryId))
            {
                continue;
            }

            if (selected is null || CompareStacks(stack, selected) < 0)
            {
                selected = stack;
            }
        }

        return selected?.Id;
    }

    public bool HasReservation(
        EntityId stackId,
        EntityId reservationOwnerId,
        int minimumQuantity = 1)
    {
        if (minimumQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumQuantity));
        }

        ItemStackState? stack = Find(stackId);
        return stack is not null
            && stack.GetReservedQuantity(reservationOwnerId) >= minimumQuantity;
    }

    private static int CompareStacks(ItemStackState left, ItemStackState right)
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

    private static void ValidateCategoryId(ItemCategoryId categoryId)
    {
        if (categoryId.Equals(default(ItemCategoryId)))
        {
            throw new ArgumentException("Category id cannot be empty.", nameof(categoryId));
        }
    }
}
}
