using System;
using System.Collections.Generic;
using Dig.Application.Inventory;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
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
        private readonly Dictionary<EntityId, DirectWorldFoodIntent>
            _directWorldFoodIntents = new Dictionary<EntityId, DirectWorldFoodIntent>();
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

            ItemStackSnapshot? source = repository.Get().GetStack(stack);
            if (eatAfterPickup
                && source?.ItemId != CampfireProductionContent.GrilledMushroomItemId)
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
            EntityId jobId = DemoId('9', sequence);
            CreateWorldItemPickupHandler handler = ReferenceEquals(
                repository,
                _buildingInventoryRepository)
                    ? _buildingItemPickupCreate!
                    : _terrainItemPickupCreate!;
            Result created = handler.Handle(new CreateWorldItemPickupCommand(
                jobId,
                stack,
                resident,
                sourceCell,
                priority: 675,
                tick));
            if (created.IsSuccess && eatAfterPickup)
            {
                _directWorldFoodIntents[jobId] = new DirectWorldFoodIntent(
                    resident,
                    stack);
            }

            return created;
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

            if (_buildingInventoryRepository == null)
            {
                throw new InvalidOperationException("Building inventory must be initialized first.");
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

        private readonly struct DirectWorldFoodIntent
        {
            internal DirectWorldFoodIntent(EntityId residentId, EntityId stackId)
            {
                ResidentId = residentId;
                StackId = stackId;
            }

            internal EntityId ResidentId { get; }
            internal EntityId StackId { get; }
        }
    }
}