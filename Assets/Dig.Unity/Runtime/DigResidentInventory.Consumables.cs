using System;
using Dig.Application.Agents;
using Dig.Application.Inventory;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        internal Result UseResidentInventoryActionWithSlotGuard(
            string residentId,
            string stackId,
            long tick,
            bool directCommand = true)
        {
            if (string.IsNullOrWhiteSpace(residentId)
                || string.IsNullOrWhiteSpace(stackId))
            {
                throw new ArgumentException("Resident and stack ids are required.");
            }

            EntityId resident = EntityId.Parse(residentId);
            EntityId stack = EntityId.Parse(stackId);
            InMemoryInventoryRepository? repository = ResolveWorldItemRepository(stack);
            ItemStackSnapshot? snapshot = repository?.Get().GetStack(stack);
            if (repository == null || snapshot == null)
            {
                return Result.Failure(InventoryErrors.StackNotFound);
            }

            ItemDefinition definition = repository.Get().Catalog.Get(snapshot.ItemId);
            if (definition.FoodUse != null)
            {
                if (_productionAgents == null)
                {
                    return Result.Failure(ResidentFoodMealErrors.ResidentNotFound);
                }

                if (directCommand)
                {
                    Result prepared = PrepareResidentsForDirectCommand(
                        new[] { residentId },
                        tick);
                    if (prepared.IsFailure)
                    {
                        return prepared;
                    }
                }

                return new StartResidentFoodMealHandler(
                    _productionAgents,
                    repository,
                    new DigTerrainResidentStandingSupportQuery(this),
                    _worldSession.Journal).Handle(
                        new StartResidentFoodMealCommand(
                            resident,
                            stack,
                            tick));
            }

            if (definition.Interactions.SupportsInventoryAction(
                    ItemInventoryInteractionAction.DirectUse)
                && !definition.IsTool)
            {
                return Result.Failure(
                    ResidentInventoryConsumableErrors.EffectOwnerUnavailable);
            }

            return UseResidentInventoryItemWithSlotGuard(
                residentId,
                stackId,
                tick);
        }

        internal Result ValidateWorldConsumableAction(string stackId)
        {
            if (string.IsNullOrWhiteSpace(stackId))
            {
                throw new ArgumentException("Stack id is required.", nameof(stackId));
            }

            EntityId stack = EntityId.Parse(stackId);
            InMemoryInventoryRepository? repository = ResolveWorldItemRepository(stack);
            ItemStackSnapshot? snapshot = repository?.Get().GetStack(stack);
            if (repository == null || snapshot == null)
            {
                return Result.Failure(InventoryErrors.StackNotFound);
            }

            ItemDefinition definition = repository.Get().Catalog.Get(snapshot.ItemId);
            return definition.FoodUse != null
                && definition.Interactions.SupportsWorldAction(
                    ItemWorldInteractionAction.DirectUse)
                ? Result.Success()
                : Result.Failure(
                    ResidentInventoryConsumableErrors.EffectOwnerUnavailable);
        }
    }
}
