using System;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Presentation.Inventory;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private ResidentInventoryPresenter? _residentBuildingInventoryPresenter;
    private ResidentInventoryPresenter? _residentTerrainInventoryPresenter;
    private ResidentInventoryLayoutPresenter? _residentInventoryLayoutPresenter;

    private void InitializeResidentInventoryPresentation()
    {
        if (_buildingInventoryRepository == null)
        {
            throw new InvalidOperationException(
                "Building inventory must be initialized first.");
        }

        _residentBuildingInventoryPresenter = new ResidentInventoryPresenter(
            DemoBuildingBoxItemId,
            _buildingInventoryRepository.Get().Catalog);
        _residentTerrainInventoryPresenter = new ResidentInventoryPresenter(
            DemoBuildingBoxItemId,
            _inventoryRepository.Get().Catalog);
        _residentInventoryLayoutPresenter = new ResidentInventoryLayoutPresenter(
            DemoBuildingBoxItemId);
        _buildingInventoryPresenter = new InventoryWorldPresenter(
            new GetInventorySnapshotQueryHandler(_buildingInventoryRepository),
            WorldItemInteractionKind.BuildingBox,
            DemoBuildingBoxItemId,
            WorldItemInteractionKind.Pickup);
    }

    internal ResidentInventoryViewModel LoadResidentInventory(string residentId)
    {
        EnsureResidentInventoryPresentation();
        EntityId id = ParseResidentId(residentId);
        ResidentInventoryViewModel building = _residentBuildingInventoryPresenter!.Present(
            _buildingInventoryRepository!.Get().CreateSnapshot(), id);
        ResidentInventoryViewModel terrain = _residentTerrainInventoryPresenter!.Present(
            _inventoryRepository.Get().CreateSnapshot(), id);
        return new ResidentInventoryViewModel(
            id.ToString(),
            checked(building.InventoryVersion + terrain.InventoryVersion),
            building.Slots.Concat(terrain.Slots).ToArray());
    }

    internal ResidentInventoryLayoutViewModel LoadResidentInventoryLayout(
        string residentId)
    {
        EnsureResidentInventoryPresentation();
        EntityId id = ParseResidentId(residentId);
        InventoryState inventory = _buildingInventoryRepository!.Get();
        Result normalized = inventory.NormalizeResidentInventory(id, tick: 0);
        if (normalized.IsFailure)
        {
            throw new InvalidOperationException(normalized.Error!.ToString());
        }

        if (normalized.IsSuccess)
        {
            _buildingInventoryRepository.Save(inventory);
        }

        return _residentInventoryLayoutPresenter!.Present(inventory, id);
    }

    private void EnsureResidentInventoryPresentation()
    {
        if (_buildingInventoryRepository == null
            || _residentBuildingInventoryPresenter == null
            || _residentTerrainInventoryPresenter == null
            || _residentInventoryLayoutPresenter == null)
        {
            throw new InvalidOperationException(
                "Resident inventory presentation is not initialized.");
        }
    }

    private static EntityId ParseResidentId(string residentId)
    {
        if (string.IsNullOrWhiteSpace(residentId))
        {
            throw new ArgumentException("Resident id is required.", nameof(residentId));
        }

        return EntityId.Parse(residentId);
    }
}

}
