using System;
using Dig.Application.Inventory;
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
        private long _nextWorldItemPickupSequence;

        internal Result CreateWorldItemPickup(
            string stackId,
            string residentId,
            CellId sourceCell,
            long tick)
        {
            EnsureWorldItemPickupInitialized();
            if (string.IsNullOrWhiteSpace(stackId)
                || string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Stack and resident ids are required.");
            }

            EntityId stack = EntityId.Parse(stackId);
            InMemoryInventoryRepository? repository = ResolveWorldItemRepository(stack);
            if (repository == null)
            {
                return Result.Failure(WorldItemPickupErrors.StackMissing);
            }

            long sequence = checked(_nextWorldItemPickupSequence + 1);
            _nextWorldItemPickupSequence = sequence;
            CreateWorldItemPickupHandler handler = ReferenceEquals(
                repository,
                _buildingInventoryRepository)
                    ? _buildingItemPickupCreate!
                    : _terrainItemPickupCreate!;
            return handler.Handle(new CreateWorldItemPickupCommand(
                DemoId('9', sequence),
                stack,
                EntityId.Parse(residentId),
                sourceCell,
                priority: 675,
                tick));
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
    }
}
