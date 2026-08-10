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
        private NavigationPathfinder? _worldItemPickupPathfinder;
        private long _nextWorldItemPickupSequence;

        internal Result CreateWorldItemPickup(
            string stackId,
            string residentId,
            CellId sourceCell,
            long tick,
            bool eatAfterPickup = false,
            bool automatic = false)
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
            ItemDefinition definition = repository.Get().Catalog.Get(snapshot.ItemId);
            if (eatAfterPickup
                && (definition.FoodUse == null
                    || !definition.Interactions.SupportsWorldAction(
                        ItemWorldInteractionAction.DirectUse)))
            {
                return Result.Failure(new DomainError(
                    "world_food.unsupported_item",
                    "The item does not expose a direct world-use food action."));
            }

            if (!automatic)
            {
                Result prepared = PrepareResidentsForDirectCommand(
                    new[] { residentId },
                    tick);
                if (prepared.IsFailure)
                {
                    return prepared;
                }
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
            string stackId,
            out CellId workPosition)
        {
            workPosition = default;
            if (_buildingsRepository == null || _buildingInventoryRepository == null
                || string.IsNullOrWhiteSpace(stackId))
            {
                return false;
            }

            ItemStackSnapshot? stack = _buildingInventoryRepository.Get()
                .GetStack(EntityId.Parse(stackId));
            if (stack == null
                || stack.Location.Kind != ItemLocationKind.BuildingInventory
                || !stack.Location.HasOwner
                || stack.AvailableQuantity <= 0)
            {
                return false;
            }

            Dig.Domain.Buildings.BuildingSnapshot? buildingSnapshot =
                _buildingsRepository.Get().Get(stack.Location.OwnerId);
            if (buildingSnapshot == null)
            {
                return false;
            }

            workPosition = ResolveBuildingInternalStockCell(buildingSnapshot);
            return true;
        }

        private void EnsureWorldItemPickupInitialized()
        {
            if (_terrainItemPickupCreate != null
                && _buildingItemPickupCreate != null
                && _terrainItemPickupComplete != null
                && _buildingItemPickupComplete != null
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
