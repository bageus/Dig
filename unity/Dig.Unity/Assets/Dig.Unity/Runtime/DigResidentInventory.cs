using System;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Domain.Core;
using Dig.Presentation.Inventory;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private ResidentInventoryPresenter? _residentBuildingInventoryPresenter;
        private ResidentInventoryPresenter? _residentTerrainInventoryPresenter;

        private void InitializeResidentInventoryPresentation()
        {
            if (_buildingInventoryRepository == null)
            {
                throw new InvalidOperationException("Building inventory must be initialized first.");
            }

            _residentBuildingInventoryPresenter = new ResidentInventoryPresenter(
                DemoBuildingBoxItemId,
                _buildingInventoryRepository.Get().Catalog);
            _residentTerrainInventoryPresenter = new ResidentInventoryPresenter(
                DemoBuildingBoxItemId,
                _inventoryRepository.Get().Catalog);
            _buildingInventoryPresenter = new InventoryWorldPresenter(
                new GetInventorySnapshotQueryHandler(_buildingInventoryRepository),
                WorldItemInteractionKind.BuildingBox,
                DemoBuildingBoxItemId,
                WorldItemInteractionKind.Pickup);
        }

        internal ResidentInventoryViewModel LoadResidentInventory(string residentId)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Resident id is required.", nameof(residentId));
            }

            if (_buildingInventoryRepository == null
                || _residentBuildingInventoryPresenter == null
                || _residentTerrainInventoryPresenter == null)
            {
                throw new InvalidOperationException("Resident inventory presentation is not initialized.");
            }

            EntityId id = EntityId.Parse(residentId);
            ResidentInventoryViewModel building = _residentBuildingInventoryPresenter.Present(
                _buildingInventoryRepository.Get().CreateSnapshot(), id);
            ResidentInventoryViewModel terrain = _residentTerrainInventoryPresenter.Present(
                _inventoryRepository.Get().CreateSnapshot(), id);
            return new ResidentInventoryViewModel(
                id.ToString(),
                checked(building.InventoryVersion + terrain.InventoryVersion),
                building.Slots.Concat(terrain.Slots).ToArray());
        }
    }
}
