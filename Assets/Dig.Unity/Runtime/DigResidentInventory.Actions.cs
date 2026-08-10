using System;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private DropResidentInventoryStackHandler? _residentBuildingInventoryDrop;
        private DropResidentInventoryStackHandler? _residentTerrainInventoryDrop;
        private UseResidentInventoryItemHandler? _residentBuildingInventoryUse;
        private UseResidentInventoryItemHandler? _residentTerrainInventoryUse;

        internal Result ValidateResidentInventoryDrop(
            string residentId,
            string stackId,
            CellId destination)
        {
            EntityId actor = ParseInventoryEntityId(residentId, nameof(residentId));
            EntityId stack = ParseInventoryEntityId(stackId, nameof(stackId));
            InMemoryInventoryRepository? repository = ResolveWorldItemRepository(stack);
            if (repository == null)
            {
                return Result.Failure(InventoryErrors.StackNotFound);
            }

            InventoryState inventory = repository.Get();
            ItemStackSnapshot? snapshot = inventory.GetStack(stack);
            if (snapshot == null)
            {
                return Result.Failure(InventoryErrors.StackNotFound);
            }

            if (!snapshot.Location.HasOwner || snapshot.Location.OwnerId != actor)
            {
                return Result.Failure(new DomainError(
                    "inventory.drop.not_owned",
                    "The stack is not owned by the selected resident."));
            }

            CellSnapshot? target = _worldSession.LoadSnapshot().Chunks
                .SelectMany(chunk => chunk.Cells)
                .Where(cell => cell.Id == destination)
                .Select(cell => (CellSnapshot?)cell)
                .FirstOrDefault();
            if (!target.HasValue)
            {
                return Result.Failure(new DomainError(
                    "inventory.drop.out_of_bounds",
                    "The item drop target is outside the world."));
            }

            if (target.Value.IsSolid || !target.Value.State.IsExplored)
            {
                return Result.Failure(new DomainError(
                    "inventory.drop.target_blocked",
                    "The item drop target must be an explored open cell."));
            }

            return Result.Success();
        }

        internal Result ValidateResidentInventoryPlacement(
            string residentId,
            string stackId,
            CellId destination)
        {
            EntityId actor = ParseInventoryEntityId(residentId, nameof(residentId));
            EntityId stack = ParseInventoryEntityId(stackId, nameof(stackId));
            InMemoryInventoryRepository? repository = ResolveWorldItemRepository(stack);
            if (repository == null)
            {
                return Result.Failure(InventoryErrors.StackNotFound);
            }

            InventoryState inventory = repository.Get();
            ItemStackSnapshot? snapshot = inventory.GetStack(stack);
            if (snapshot == null)
            {
                return Result.Failure(InventoryErrors.StackNotFound);
            }

            if (snapshot.Location.Kind != ItemLocationKind.AgentInventory
                || !DropResidentInventoryStackHandler.IsOwnedByResident(
                    snapshot.Location,
                    actor)
                || snapshot.AvailableQuantity != snapshot.Quantity
                || snapshot.ReservedQuantity != 0
                || inventory.CreateSnapshot().HeldItems.Any(value => value.StackId == stack))
            {
                return Result.Failure(ResidentInventoryPlacementErrors.SourceUnavailable);
            }

            return CreateResidentInventoryPlacementHandler.ValidateTarget(
                _worldSession.LoadSnapshot(),
                destination,
                GetBuildingPlacementReachableCells());
        }

        internal Result DropResidentInventoryStack(
            string residentId,
            string stackId,
            CellId destination,
            long tick)
        {
            EnsureResidentInventoryActionsInitialized();
            EntityId actor = ParseInventoryEntityId(residentId, nameof(residentId));
            EntityId stack = ParseInventoryEntityId(stackId, nameof(stackId));
            DropResidentInventoryStackHandler? handler = ResolveResidentInventoryDrop(stack);
            return handler == null
                ? Result.Failure(InventoryErrors.StackNotFound)
                : handler.Handle(new DropResidentInventoryStackCommand(
                    actor,
                    stack,
                    destination,
                    tick));
        }

        internal Result UseResidentInventoryItem(
            string residentId,
            string stackId,
            long tick)
        {
            EnsureResidentInventoryActionsInitialized();
            EntityId actor = ParseInventoryEntityId(residentId, nameof(residentId));
            EntityId stack = ParseInventoryEntityId(stackId, nameof(stackId));
            UseResidentInventoryItemHandler? handler = ResolveResidentInventoryUse(stack);
            return handler == null
                ? Result.Failure(InventoryErrors.StackNotFound)
                : handler.Handle(new UseResidentInventoryItemCommand(
                    actor,
                    stack,
                    tick));
        }

        private void EnsureResidentInventoryActionsInitialized()
        {
            if (_residentBuildingInventoryDrop != null
                && _residentTerrainInventoryDrop != null
                && _residentBuildingInventoryUse != null
                && _residentTerrainInventoryUse != null)
            {
                return;
            }

            if (_buildingInventoryRepository == null)
            {
                throw new InvalidOperationException(
                    "Resident inventory actions require building inventory state.");
            }

            _residentBuildingInventoryDrop = new DropResidentInventoryStackHandler(
                _buildingInventoryRepository,
                _worldSession.Journal);
            _residentTerrainInventoryDrop = new DropResidentInventoryStackHandler(
                _inventoryRepository,
                _worldSession.Journal);
            _residentBuildingInventoryUse = new UseResidentInventoryItemHandler(
                _buildingInventoryRepository,
                _worldSession.Journal);
            _residentTerrainInventoryUse = new UseResidentInventoryItemHandler(
                _inventoryRepository,
                _worldSession.Journal);
        }

        private DropResidentInventoryStackHandler? ResolveResidentInventoryDrop(
            EntityId stackId)
        {
            if (_buildingInventoryRepository?.Get().GetStack(stackId) != null)
            {
                return _residentBuildingInventoryDrop;
            }

            return _inventoryRepository.Get().GetStack(stackId) != null
                ? _residentTerrainInventoryDrop
                : null;
        }

        private UseResidentInventoryItemHandler? ResolveResidentInventoryUse(
            EntityId stackId)
        {
            if (_buildingInventoryRepository?.Get().GetStack(stackId) != null)
            {
                return _residentBuildingInventoryUse;
            }

            return _inventoryRepository.Get().GetStack(stackId) != null
                ? _residentTerrainInventoryUse
                : null;
        }

        private static EntityId ParseInventoryEntityId(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Entity id is required.", parameterName);
            }

            return EntityId.Parse(value);
        }
    }
}
