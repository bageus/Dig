using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    public Result ReplaceProductionPackage(
        EntityId packageStackId,
        ItemId expectedPackageItemId,
        IReadOnlyCollection<ItemStackCreation> replacements,
        long tick)
    {
        ValidateTick(tick);
        if (packageStackId.IsEmpty || expectedPackageItemId.IsEmpty)
        {
            throw new ArgumentException("Package stack and item ids are required.");
        }

        if (replacements is null)
        {
            throw new ArgumentNullException(nameof(replacements));
        }

        ItemStackState? package = Find(packageStackId);
        if (package is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        ItemStackCreation[] outputs = replacements.ToArray();
        if (package.ItemId != expectedPackageItemId
            || package.Quantity != 1
            || package.AvailableQuantity != 1
            || outputs.Length == 0)
        {
            return Result.Failure(InventoryErrors.InsufficientAvailableQuantity);
        }

        EntityId[] outputIds = outputs.Select(value => value.StackId).ToArray();
        if (outputIds.Distinct().Count() != outputIds.Length
            || outputIds.Any(value => value != packageStackId && _stacks.ContainsKey(value)))
        {
            return Result.Failure(InventoryErrors.StackAlreadyExists);
        }

        foreach (ItemStackCreation output in outputs)
        {
            if (output.Quantity > Catalog.Get(output.ItemId).MaximumStackSize)
            {
                return Result.Failure(InventoryErrors.StackSizeExceeded);
            }
        }

        ItemLocation location = package.Location;
        _stacks.Remove(packageStackId);
        foreach (ItemStackCreation output in outputs)
        {
            _stacks.Add(
                output.StackId,
                new ItemStackState(
                    output.StackId,
                    output.ItemId,
                    output.Quantity,
                    location));
        }

        IncrementVersion();
        return Result.Success();
    }
}

}
