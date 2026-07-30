using System;
using System.Collections.Generic;
using System.Linq;
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
        EntityId[] ids = new EntityId[command.TotalOutputQuantity];
        for (int index = 0; index < ids.Length; index++)
        {
            ids[index] = index == 0
                ? command.OutputStackId
                : EntityId.Parse(CreateDerivedEntityId(seed, index));
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
            return outputUnitIds.Count == 0
                ? Result.Success()
                : Result.Failure(InventoryErrors.InvalidQuantity);
        }

        if (outputUnitIds.Count != command.TotalOutputQuantity
            || outputUnitIds.Any(value => value.IsEmpty)
            || outputUnitIds.Distinct().Count() != outputUnitIds.Count)
        {
            return Result.Failure(InventoryErrors.InvalidQuantity);
        }

        foreach (TerrainWorkOutputSpec output in command.Outputs)
        {
            if (!inventory.Catalog.Contains(output.ItemId))
            {
                return Result.Failure(TerrainWorkCompletionErrors.UnknownOutputItem);
            }

            ItemDefinition definition = inventory.Catalog.Get(output.ItemId);
            if (output.Quantity > definition.MaximumStackSize)
            {
                return Result.Failure(InventoryErrors.StackSizeExceeded);
            }
        }

        foreach (EntityId outputUnitId in outputUnitIds)
        {
            if (inventory.GetStack(outputUnitId) is not null)
            {
                return Result.Failure(InventoryErrors.StackAlreadyExists);
            }
        }

        return Result.Success();
    }

    public static IReadOnlyList<TerrainWorkProducedOutput> AddToInventory(
        InventoryState inventory,
        CompleteTerrainWorkCommand command,
        IReadOnlyList<EntityId> outputUnitIds,
        ItemLocation location,
        long tick)
    {
        if (!command.ProducesOutput)
        {
            return Array.Empty<TerrainWorkProducedOutput>();
        }

        List<TerrainWorkProducedOutput> produced =
            new List<TerrainWorkProducedOutput>(command.Outputs.Count);
        int offset = 0;
        foreach (TerrainWorkOutputSpec output in command.Outputs)
        {
            EntityId[] ids = outputUnitIds
                .Skip(offset)
                .Take(output.Quantity)
                .ToArray();
            Result added = inventory.AddUnits(
                ids,
                output.ItemId,
                location,
                tick);
            if (added.IsFailure)
            {
                throw new InvalidOperationException(
                    $"A validated terrain output batch failed: {added.Error}");
            }

            produced.Add(new TerrainWorkProducedOutput(
                output.ItemId,
                output.Quantity,
                ids));
            offset += output.Quantity;
        }

        return produced;
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
