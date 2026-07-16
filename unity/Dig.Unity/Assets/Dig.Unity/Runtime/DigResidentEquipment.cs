using System;
using System.Collections.Generic;
using Dig.Presentation.Inventory;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private readonly ResidentEquipmentPresenter _residentEquipmentPresenter =
            new ResidentEquipmentPresenter();

        internal IReadOnlyList<ResidentEquipmentViewModel> LoadResidentEquipment()
        {
            if (_buildingInventoryRepository == null)
            {
                throw new InvalidOperationException(
                    "Resident equipment requires building inventory state.");
            }

            return _residentEquipmentPresenter.Present(
                _buildingInventoryRepository.Get().CreateSnapshot(),
                _inventoryRepository.Get().CreateSnapshot());
        }
    }
}