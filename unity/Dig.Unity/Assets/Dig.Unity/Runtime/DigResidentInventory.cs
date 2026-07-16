using System;
using Dig.Application.Inventory;
using Dig.Domain.Core;
using Dig.Presentation.Inventory;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private ResidentInventoryPresenter? _residentInventoryPresenter;

        private void InitializeResidentInventoryPresentation()
        {
            if (_buildingInventoryRepository == null)
            {
                throw new InvalidOperationException(
                    "Building inventory must be initialized first.");
            }

            _residentInventoryPresenter = new ResidentInventoryPresenter(
                DemoBuildingBoxItemId,
                _buildingInventoryRepository.Get().Catalog);
            _buildingInventoryPresenter = new InventoryWorldPresenter(
                new GetInventorySnapshotQueryHandler(_buildingInventoryRepository),
                WorldItemInteractionKind.BuildingBox,
                DemoBuildingBoxItemId);
        }

        internal ResidentInventoryViewModel LoadResidentInventory(string residentId)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Resident id is required.", nameof(residentId));
            }

            if (_buildingInventoryRepository == null || _residentInventoryPresenter == null)
            {
                throw new InvalidOperationException(
                    "Resident inventory presentation is not initialized.");
            }

            return _residentInventoryPresenter.Present(
                _buildingInventoryRepository.Get().CreateSnapshot(),
                EntityId.Parse(residentId));
        }
    }
}
