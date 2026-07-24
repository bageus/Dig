using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Application.Jobs
{

internal static class TerrainWorkOutputUnits
{
    public static EntityId[] CreateIds(CompleteTerrainWorkCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (!command.ProducesOutput)
        {
            return Array.Empty<EntityId>();
        }

        string seed = command.OutputStackId.ToString();
        EntityId[] ids = new EntityId[command.OutputQuantity];
        ids[0] = command.OutputStackId;

        for (int index = 1; index < ids.Length; index++)
        {
            ids[index] = EntityId.Parse(CreateDerivedEntityId(seed, index));
        }

        return ids;
    }

    public static Result Validate(
        InventoryState inventory,
        CompleteTerrainWorkCommand command,
        IReadOnlyList<EntityId> outputUnitIds)
    {
        if (inventory is null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (outputUnitIds is null)
        {
            throw new ArgumentNullException(nameof(outputUnitIds));
        }

        if (!command.ProducesOutput)
        {
            return Result.Success();
        }

        if (!inventory.Catalog.Contains(command.OutputItemId))
        {
            return Result.Failure(TerrainWorkCompletionErrors.UnknownOutputItem);
        }

        if (command.OutputQuantity <= 0)
        {
            return Result.Failure(InventoryErrors.InvalidQuantity);
        }

        foreach (EntityId outputUnitId in outputUnitIds)
        {
            if (inventory.GetStack(outputUnitId) is not null)
            {
                return Result.Failure(InventoryErrors.StackAlreadyExists);
            }
        }

        ItemDefinition definition = inventory.Catalog.Get(command.OutputItemId);
        return command.OutputQuantity > definition.MaximumStackSize
            ? Result.Failure(InventoryErrors.StackSizeExceeded)
            : Result.Success();
    }

    private static string CreateDerivedEntityId(string seed, int index)
    {
        const int suffixLength = 8;
        string prefix = seed.Substring(0, seed.Length - suffixLength);
        uint seedSuffix = Convert.ToUInt32(seed.Substring(seed.Length - suffixLength), 16);
        uint derivedSuffix = checked(seedSuffix + (uint)index);
        return prefix + derivedSuffix.ToString("x8");
    }
}

}
