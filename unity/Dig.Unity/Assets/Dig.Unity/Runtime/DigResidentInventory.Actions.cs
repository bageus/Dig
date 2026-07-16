using System;
using Dig.Application.Inventory;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private DropResidentInventoryStackHandler? _residentBuildingInventoryDrop;
        private DropResidentInventoryStackHandler? _residentTerrainInventoryDrop;
        private UseResidentInventoryItemHandler? _residentBuildingInventoryUse;
        private UseResidentInventoryItemHandler? _residentTerrainInventoryUse;

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
