using System;
using Dig.Application.Inventory;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private DropResidentInventoryStackHandler? _residentInventoryDrop;
        private UseResidentInventoryItemHandler? _residentInventoryUse;

        internal Result DropResidentInventoryStack(
            string residentId,
            string stackId,
            CellId destination,
            long tick)
        {
            EnsureResidentInventoryActionsInitialized();
            return _residentInventoryDrop!.Handle(new DropResidentInventoryStackCommand(
                ParseInventoryEntityId(residentId, nameof(residentId)),
                ParseInventoryEntityId(stackId, nameof(stackId)),
                destination,
                tick));
        }

        internal Result UseResidentInventoryItem(
            string residentId,
            string stackId,
            long tick)
        {
            EnsureResidentInventoryActionsInitialized();
            return _residentInventoryUse!.Handle(new UseResidentInventoryItemCommand(
                ParseInventoryEntityId(residentId, nameof(residentId)),
                ParseInventoryEntityId(stackId, nameof(stackId)),
                tick));
        }

        private void EnsureResidentInventoryActionsInitialized()
        {
            if (_residentInventoryDrop != null && _residentInventoryUse != null)
            {
                return;
            }

            if (_buildingInventoryRepository == null)
            {
                throw new InvalidOperationException(
                    "Resident inventory actions require building inventory state.");
            }

            _residentInventoryDrop = new DropResidentInventoryStackHandler(
                _buildingInventoryRepository,
                _worldSession.Journal);
            _residentInventoryUse = new UseResidentInventoryItemHandler(
                _buildingInventoryRepository,
                _worldSession.Journal);
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
