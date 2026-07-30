using System;
using Dig.Application.Agents;
using Dig.Application.Inventory;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private static readonly ItemCategoryId PotionCategoryId =
            new ItemCategoryId("potion");
        private static readonly ItemCategoryId DrinkCategoryId =
            new ItemCategoryId("drink");
        private static readonly ItemCategoryId BeverageCategoryId =
            new ItemCategoryId("beverage");

        internal Result UseResidentInventoryActionWithSlotGuard(
            string residentId,
            string stackId,
            long tick)
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
            if (definition.HasCategory(CampfireProductionContent.FoodCategoryId))
            {
                if (_productionAgents == null)
                {
                    return Result.Failure(ResidentFoodMealErrors.ResidentNotFound);
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

            if (IsPotionOrDrink(definition))
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
            if (definition.HasCategory(CampfireProductionContent.FoodCategoryId))
            {
                return Result.Success();
            }

            return IsPotionOrDrink(definition)
                ? Result.Failure(
                    ResidentInventoryConsumableErrors.EffectOwnerUnavailable)
                : Result.Failure(
                    ResidentInventoryConsumableErrors.EffectOwnerUnavailable);
        }

        private static bool IsPotionOrDrink(ItemDefinition definition)
        {
            return definition.HasCategory(PotionCategoryId)
                || definition.HasCategory(DrinkCategoryId)
                || definition.HasCategory(BeverageCategoryId);
        }
    }
}
