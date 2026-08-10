using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
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

        internal EquipmentRates ResidentEquipmentRates =>
            _residentEquipmentRates ??= CreateDemoEquipmentRates();

        private ResidentWorkRatePresenter ResidentWorkRatePresenter =>
            _residentWorkRatePresenter ??= new ResidentWorkRatePresenter(
                ResidentEquipmentRates,
                ResidentMiningBaseIntervalTicks,
                ResidentConstructionBaseIntervalTicks);

        internal IReadOnlyList<ResidentEquipmentViewModel> LoadResidentEquipment()
        {
            InventorySnapshot[] snapshots = LoadResidentEquipmentSnapshots();
            List<ResidentEquipmentViewModel> equipment =
                _residentEquipmentPresenter.Present(snapshots).ToList();
            IReadOnlyList<ResidentEquipmentViewModel> productionCarries =
                LoadProductionMaterialCarries();
            if (productionCarries.Count == 0)
            {
                return equipment;
            }

            HashSet<string> overridden = productionCarries
                .Select(value => value.ResidentId)
                .ToHashSet(StringComparer.Ordinal);
            equipment.RemoveAll(value => overridden.Contains(value.ResidentId));
            equipment.AddRange(productionCarries);
            return equipment
                .OrderBy(value => value.ResidentId, StringComparer.Ordinal)
                .ToArray();
        }

        private IReadOnlyList<ResidentEquipmentViewModel> LoadProductionMaterialCarries()
        {
            if (_productionRepository == null || _jobRepository == null)
            {
                return Array.Empty<ResidentEquipmentViewModel>();
            }

            List<ResidentEquipmentViewModel> values =
                new List<ResidentEquipmentViewModel>();
            foreach (JobSnapshot job in _jobRepository.Get().GetAll()
                .Where(value => !value.IsTerminal
                    && value.AssignedAgentId.HasValue
                    && value.Definition is ProductionWorkJobDefinition)
                .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal))
            {
                ProductionWorkJobDefinition production =
                    (ProductionWorkJobDefinition)job.Definition;
                ProductionOrderSnapshot? order = _productionRepository.Get().Get(
                    production.OrderId);
                if (order == null
                    || !TryResolveCurrentProductionMaterialStep(
                        order,
                        out ProductionMaterialStepSnapshot step))
                {
                    continue;
                }

                EntityId residentId = job.AssignedAgentId!.Value;
                bool carriesRaw = step.Phase == ProductionMaterialStepPhase.AwaitingMaterial
                    && HasCarriedProductionMaterial(
                        production.OrderId,
                        residentId,
                        step.ItemId);
                bool carriesProcessed =
                    step.Phase == ProductionMaterialStepPhase.ProcessedAwaitingPackage;
                if (!carriesRaw && !carriesProcessed)
                {
                    continue;
                }

                values.Add(new ResidentEquipmentViewModel(
                    residentId.ToString(),
                    "production-carry:" + production.OrderId,
                    step.ItemId.ToString()));
            }

            return values;
        }

        internal IReadOnlyList<ResidentWorkRateViewModel> LoadResidentWorkRates(
            IReadOnlyList<AgentViewModel> agents)
        {
            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
            }

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

        private InventorySnapshot[] LoadResidentEquipmentSnapshots()
        {
            if (_buildingInventoryRepository == null)
            {
                throw new InvalidOperationException(
                    "Resident equipment requires building inventory state.");
            }

            if (ReferenceEquals(_buildingInventoryRepository, _inventoryRepository))
            {
                return new[]
                {
                    _inventoryRepository.Get().CreateSnapshot(),
                };
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
