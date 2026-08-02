using System;
using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Production;

namespace Dig.Application.Saving
{

public static partial class BuildingProductionSaveAdapter
{
    private static void RestoreProgress(
        ProductionState production,
        InventoryState inventory,
        ProductionOrderSaveData saved,
        RecipeDefinition recipe,
        EntityId orderId,
        long tick)
    {
        if (recipe.UsesMaterialSteps)
        {
            ProductionMaterialStepSaveData[] ordered = saved.MaterialSteps
                .OrderBy(value => value.Index)
                .ToArray();
            ProductionMaterialStepSnapshot[] restored = new
                ProductionMaterialStepSnapshot[ordered.Length];
            for (int index = 0; index < ordered.Length; index++)
            {
                ProductionMaterialStepSaveData step = ordered[index];
                ProductionMaterialStepPhase phase = ResolveSavedMaterialPhase(step);
                if (!step.Phase.HasValue
                    && phase is ProductionMaterialStepPhase.Processing
                        or ProductionMaterialStepPhase.ProcessedAwaitingPackage)
                {
                    MigrateLegacyRawToWorkbench(
                        inventory,
                        orderId,
                        new ItemId(step.ItemId),
                        tick);
                }

                restored[index] = new ProductionMaterialStepSnapshot(
                    step.Index,
                    new ItemId(step.ItemId),
                    step.RequiredTicks,
                    step.CompletedTicks,
                    phase);
            }

            RequireSuccess(production.RestoreMaterialProgress(
                orderId,
                restored,
                tick));
        }
        else if (saved.CompletedWork > 0)
        {
            RequireSuccess(production.AddWork(orderId, saved.CompletedWork, tick));
        }
    }

    private static ProductionMaterialStepPhase ResolveSavedMaterialPhase(
        ProductionMaterialStepSaveData step)
    {
        if (step.Phase.HasValue)
        {
            if (!Enum.IsDefined(
                typeof(ProductionMaterialStepPhase),
                step.Phase.Value))
            {
                throw new InvalidOperationException("Invalid production material phase.");
            }

            return (ProductionMaterialStepPhase)step.Phase.Value;
        }

        if (step.IsConsumed)
        {
            return ProductionMaterialStepPhase.Deposited;
        }

        if (step.CompletedTicks <= 0)
        {
            return ProductionMaterialStepPhase.AwaitingMaterial;
        }

        return step.CompletedTicks >= step.RequiredTicks
            ? ProductionMaterialStepPhase.ProcessedAwaitingPackage
            : ProductionMaterialStepPhase.Processing;
    }

    private static void MigrateLegacyRawToWorkbench(
        InventoryState inventory,
        EntityId orderId,
        ItemId itemId,
        long tick)
    {
        ItemStackSnapshot? carried = inventory.CreateSnapshot().Stacks
            .Where(stack => stack.ItemId == itemId
                && stack.Location.Kind == ItemLocationKind.AgentInventory
                && stack.Location.HasOwner
                && stack.Reservations.Any(value =>
                    value.JobId == orderId && value.Quantity > 0))
            .OrderBy(stack => stack.StackId.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();
        Result migrated = carried != null
            ? inventory.ConsumeReservedProductionUnit(
                orderId,
                carried.Location.OwnerId,
                itemId,
                tick)
            : inventory.ConsumeNextReserved(orderId, itemId, quantity: 1, tick);
        RequireSuccess(migrated);
    }


}

}
