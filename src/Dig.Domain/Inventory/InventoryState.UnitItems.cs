using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    public Result AddUnit(
        EntityId itemEntityId,
        ItemId itemId,
        ItemLocation location,
        long tick)
    {
        return AddStack(itemEntityId, itemId, quantity: 1, location, tick);
    }

    public Result AddUnits(
        IEnumerable<EntityId> itemEntityIds,
        ItemId itemId,
        ItemLocation location,
        long tick)
    {
        ValidateTick(tick);
        if (itemEntityIds is null)
        {
            throw new ArgumentNullException(nameof(itemEntityIds));
        }

        EntityId[] ids = itemEntityIds.ToArray();
        if (ids.Length == 0 || ids.Any(id => id.IsEmpty))
        {
            return Result.Failure(InventoryErrors.InvalidQuantity);
        }

        if (ids.Distinct().Count() != ids.Length
            || ids.Any(id => _stacks.ContainsKey(id)))
        {
            return Result.Failure(InventoryErrors.StackAlreadyExists);
        }

        ItemDefinition definition = Catalog.Get(itemId);
        Result locationValidation = ValidateResidentLocationForUnitBatch(
            definition,
            location,
            ids.Length);
        if (locationValidation.IsFailure)
        {
            return locationValidation;
        }

        for (int index = 0; index < ids.Length; index++)
        {
            _stacks.Add(
                ids[index],
                new ItemStackState(ids[index], itemId, quantity: 1, location));
        }

        IncrementVersion();
        return Result.Success();
    }

    private Result ValidateResidentLocationForUnitBatch(
        ItemDefinition definition,
        ItemLocation location,
        int unitCount)
    {
        if (location.Kind != ItemLocationKind.AgentInventory)
        {
            return Result.Success();
        }

        if (unitCount != 1)
        {
            return Result.Failure(InventoryErrors.ResidentInventoryCapacityExceeded);
        }

        return ValidateResidentLocationForNewStack(definition, location);
    }
}

}
