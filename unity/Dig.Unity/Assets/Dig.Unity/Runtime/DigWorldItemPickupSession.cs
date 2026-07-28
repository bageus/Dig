using System;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Inventory;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private CreateWorldItemPickupHandler? _terrainItemPickupCreate;
        private CreateWorldItemPickupHandler? _buildingItemPickupCreate;
        private CompleteWorldItemPickupHandler? _terrainItemPickupComplete;
        private CompleteWorldItemPickupHandler? _buildingItemPickupComplete;
        private StartResidentFoodMealHandler? _startResidentFoodMeal;
        private NavigationPathfinder? _worldItemPickupPathfinder;
        private long _nextWorldItemPickupSequence;

        internal Result CreateWorldItemPickup(
            string stackId,
            string residentId,
            CellId sourceCell,
            long tick,
            bool eatAfterPickup = false)
        {
            EnsureWorldItemPickupInitialized();
            if (string.IsNullOrWhiteSpace(stackId)
                || string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Stack and resident ids are required.");
            }

            EntityId stack = EntityId.Parse(stackId);
            EntityId resident = EntityId.Parse(residentId);
            InMemoryInventoryRepository? repository = ResolveWorldItemRepository(stack);
            if (repository == null)
            {
                return Result.Failure(WorldItemPickupErrors.StackMissing);
            }

            ItemStackSnapshot snapshot = repository.Get().GetStack(stack)!;
            bool internalStock = snapshot.Location.Kind
                == ItemLocationKind.BuildingInventory;
            int quantity = internalStock ? 1 : snapshot.Quantity;
            if (eatAfterPickup
                && snapshot.ItemId != CampfireProductionContent.GrilledMushroomItemId)
            {
                return Result.Failure(new DomainError(
                    "world_food.unsupported_item",
                    "Only grilled mushroom supports direct world eating."));
            }

            Result prepared = PrepareResidentsForDirectCommand(
                new[] { residentId },
                tick);
            if (prepared.IsFailure)
            {
                return prepared;
            }

            long sequence = checked(_nextWorldItemPickupSequence + 1);
            _nextWorldItemPickupSequence = sequence;
            EntityId destinationStackId = quantity < snapshot.Quantity
                ? DemoId('8', sequence)
                : default;
            EntityId jobId = DemoId('9', sequence);
            CreateWorldItemPickupHandler handler = ReferenceEquals(
                repository,
                _buildingInventoryRepository)
                    ? _buildingItemPickupCreate!
                    : _terrainItemPickupCreate!;
            return handler.Handle(new CreateWorldItemPickupCommand(
                jobId,
                stack,
                resident,
                sourceCell,
                snapshot.Location,
                quantity,
                destinationStackId,
                priority: 675,
                tick,
                eatAfterPickup
                    ? WorldItemPickupCompletionAction.UseConsumable
                    : WorldItemPickupCompletionAction.None));
        }

        internal bool TryResolveBuildingInternalStockPickup(
            string buildingId,
            string itemId,
            out string stackId,
            out CellId workPosition)
        {
            stackId = string.Empty;
            workPosition = default;
            if (_buildingsRepository == null || _buildingInventoryRepository == null)
            {
                return false;
            }

            EntityId building = EntityId.Parse(buildingId);
            ItemId item = new ItemId(itemId);
            ItemStackSnapshot? stack = _buildingInventoryRepository.Get()
                .CreateSnapshot().Stacks
                .Where(value => value.ItemId == item
                    && value.Location == ItemLocation.InBuilding(building)
                    && value.AvailableQuantity > 0)
                .OrderBy(value => value.StackId.ToString(), StringComparer.Ordinal)
                .FirstOrDefault();
            Dig.Domain.Buildings.BuildingSnapshot? buildingSnapshot =
                _buildingsRepository.Get().Get(building);
            if (stack == null || buildingSnapshot == null)
            {
                return false;
            }

            stackId = stack.StackId.ToString();
            workPosition = buildingSnapshot.WorkPosition;
            return true;
        }

        private void EnsureWorldItemPickupInitialized()
        {
            if (_terrainItemPickupCreate != null
                && _buildingItemPickupCreate != null
                && _terrainItemPickupComplete != null
                && _buildingItemPickupComplete != null
                && _startResidentFoodMeal != null
                && _worldItemPickupPathfinder != null)
            {
                return;
            }

            if (_buildingInventoryRepository == null || _productionAgents == null)
            {
                throw new InvalidOperationException(
                    "Building inventory and resident state must be initialized first.");
            }

            InMemoryExecutionJournal journal = _worldSession.Journal;
            _terrainItemPickupCreate = new CreateWorldItemPickupHandler(
                _inventoryRepository,
                _jobRepository,
                journal);
            _buildingItemPickupCreate = new CreateWorldItemPickupHandler(
                _buildingInventoryRepository,
                _jobRepository,
                journal);
            _terrainItemPickupComplete = new CompleteWorldItemPickupHandler(
                _inventoryRepository,
                _jobRepository,
                journal);
            _buildingItemPickupComplete = new CompleteWorldItemPickupHandler(
                _buildingInventoryRepository,
                _jobRepository,
                journal);
            _startResidentFoodMeal = new StartResidentFoodMealHandler(
                _productionAgents,
                _buildingInventoryRepository,
                journal);
            _worldItemPickupPathfinder = new NavigationPathfinder();
        }

        private InMemoryInventoryRepository? ResolveWorldItemRepository(EntityId stackId)
        {
            if (_buildingInventoryRepository?.Get().GetStack(stackId) != null)
            {
                return _buildingInventoryRepository;
            }

            return _inventoryRepository.Get().GetStack(stackId) != null
                ? _inventoryRepository
                : null;
        }
    }
}
