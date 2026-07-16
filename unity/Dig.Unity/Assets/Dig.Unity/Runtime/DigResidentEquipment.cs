using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Presentation.Inventory;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private readonly ResidentEquipmentPresenter _residentEquipmentPresenter =
            new ResidentEquipmentPresenter();
        private EquipmentRates? _residentEquipmentRates;

        internal EquipmentRates ResidentEquipmentRates =>
            _residentEquipmentRates ??= CreateDemoEquipmentRates();

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

        internal int ResolveMiningWorkInterval(
            string residentId,
            int baseIntervalTicks)
        {
            if (_buildingInventoryRepository == null)
            {
                throw new InvalidOperationException(
                    "Resident equipment requires building inventory state.");
            }

            if (string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Resident id is required.", nameof(residentId));
            }

            return ResidentEquipmentRates.ResolveIntervalTicks(
                EntityId.Parse(residentId),
                EquipmentWorkKind.Mining,
                baseIntervalTicks,
                _buildingInventoryRepository.Get().CreateSnapshot(),
                _inventoryRepository.Get().CreateSnapshot());
        }

        private static EquipmentRates CreateDemoEquipmentRates()
        {
            return new EquipmentRates(new[]
            {
                new EquipmentProfile(
                    new ItemId("demo.tool.pickaxe"),
                    EquipmentAppearanceKind.Mining,
                    EquipmentWorkKind.Mining,
                    workIntervalTicks: 1),
                new EquipmentProfile(
                    new ItemId("demo.tool.hammer"),
                    EquipmentAppearanceKind.Construction,
                    EquipmentWorkKind.Construction,
                    workIntervalTicks: 1),
            });
        }
    }
}
