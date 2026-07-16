using System;
using Dig.Domain.Core;
using Dig.Presentation.Inventory;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private ResidentInventoryPresenter? _residentInventoryPresenter;

        private void InitializeResidentInventoryPresentation()
        {
            _residentInventoryPresenter = new ResidentInventoryPresenter(
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
