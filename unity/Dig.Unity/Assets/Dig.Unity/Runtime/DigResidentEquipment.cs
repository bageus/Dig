using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private const int ResidentMiningBaseIntervalTicks = 3;
        private const int ResidentConstructionBaseIntervalTicks = 2;
        private readonly ResidentEquipmentPresenter _residentEquipmentPresenter =
            new ResidentEquipmentPresenter();
        private EquipmentRates? _residentEquipmentRates;
        private ResidentWorkRatePresenter? _residentWorkRatePresenter;
        private bool _constructionCadenceConfigured;

        internal EquipmentRates ResidentEquipmentRates =>
            _residentEquipmentRates ??= CreateDemoEquipmentRates();

        private ResidentWorkRatePresenter ResidentWorkRatePresenter =>
            _residentWorkRatePresenter ??= new ResidentWorkRatePresenter(
                ResidentEquipmentRates,
                ResidentMiningBaseIntervalTicks,
                ResidentConstructionBaseIntervalTicks);

        internal IReadOnlyList<ResidentEquipmentViewModel> LoadResidentEquipment()
        {
            ConfigureConstructionWorkCadence();
            InventorySnapshot[] snapshots = LoadResidentEquipmentSnapshots();
            return _residentEquipmentPresenter.Present(snapshots);
        }

        internal IReadOnlyList<ResidentWorkRateViewModel> LoadResidentWorkRates(
            IReadOnlyList<AgentViewModel> agents)
        {
            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
            }

            ConfigureConstructionWorkCadence();
            InventorySnapshot[] snapshots = LoadResidentEquipmentSnapshots();
            return ResidentWorkRatePresenter.Present(
                agents.Select(agent => agent.Id),
                snapshots);
        }

        internal int ResolveMiningWorkInterval(
            string residentId,
            int baseIntervalTicks)
        {
            return ResolveWorkInterval(
                residentId,
                EquipmentWorkKind.Mining,
                baseIntervalTicks);
        }

        internal int ResolveConstructionWorkInterval(
            string residentId,
            int baseIntervalTicks)
        {
            return ResolveWorkInterval(
                residentId,
                EquipmentWorkKind.Construction,
                baseIntervalTicks);
        }

        private int ResolveWorkInterval(
            string residentId,
            EquipmentWorkKind workKind,
            int baseIntervalTicks)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Resident id is required.", nameof(residentId));
            }

            InventorySnapshot[] snapshots = LoadResidentEquipmentSnapshots();
            return ResidentEquipmentRates.ResolveIntervalTicks(
                EntityId.Parse(residentId),
                workKind,
                baseIntervalTicks,
                snapshots);
        }

        private bool IsConstructionWorkDue(EntityId residentId, long tick)
        {
            int intervalTicks = ResolveConstructionWorkInterval(
                residentId.ToString(),
                ResidentConstructionBaseIntervalTicks);
            return EquipmentWorkCadence.IsDue(tick, intervalTicks);
        }

        private void ConfigureConstructionWorkCadence()
        {
            if (_constructionCadenceConfigured)
            {
                return;
            }

            if (_buildingBoxAssemblyWork == null || _buildingPackingWork == null)
            {
                return;
            }

            _buildingBoxAssemblyWork.SetWorkDuePolicy(IsConstructionWorkDue);
            _buildingPackingWork.SetWorkDuePolicy(IsConstructionWorkDue);
            _constructionCadenceConfigured = true;
        }

        private InventorySnapshot[] LoadResidentEquipmentSnapshots()
        {
            if (_buildingInventoryRepository == null)
            {
                throw new InvalidOperationException(
                    "Resident equipment requires building inventory state.");
            }

            return new[]
            {
                _buildingInventoryRepository.Get().CreateSnapshot(),
                _inventoryRepository.Get().CreateSnapshot(),
            };
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