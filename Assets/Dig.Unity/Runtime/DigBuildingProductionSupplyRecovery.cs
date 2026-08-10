using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Production;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private void SynchronizeRequiredProductionInputDelivery(long tick)
    {
        ProductionOrderSnapshot[] orders = _productionRepository!.Get().GetAll()
            .Where(value => !value.IsTerminal)
            .OrderBy(value => value.Sequence)
            .ToArray();
        foreach (IGrouping<EntityId, ProductionOrderSnapshot> group in orders
            .GroupBy(value => value.BuildingId)
            .OrderBy(value => value.Key.ToString(), StringComparer.Ordinal))
        {
            ItemConsumptionRequest[] inputs = group
                .SelectMany(value => value.Recipe.Inputs)
                .GroupBy(value => value.ItemId)
                .Select(value => new ItemConsumptionRequest(
                    value.Key,
                    value.Sum(item => item.Quantity)))
                .ToArray();
            Result enabled = _enableProductionInputDelivery!.Handle(
                new EnableProductionInputDeliveryCommand(
                    group.Key,
                    inputs,
                    tick));
            if (enabled.IsFailure
                && enabled.Error != BuildingSupplyErrors.WorkstationNotFound)
            {
                throw new InvalidOperationException(enabled.Error!.ToString());
            }
        }
    }

    private void RecoverBlockedBuildingSupplyJobs(
        long tick,
        IReadOnlyList<Dig.Presentation.Agents.AgentViewModel> agents)
    {
        BuildingSupplyState supply = _buildingSupplyRepository!.Get();
        InventorySnapshot inventory = _buildingInventoryRepository!.Get()
            .CreateSnapshot();
        Dictionary<EntityId, EntityId> activeByBuilding = supply.GetAll(inventory)
            .Where(value => value.ActiveSupplyJobId.HasValue)
            .ToDictionary(
                value => value.BuildingId,
                value => value.ActiveSupplyJobId!.Value);
        JobSnapshot[] blocked = _jobRepository.Get().GetAll()
            .Where(value => value.Definition is BuildingSupplyJobDefinition definition
                && activeByBuilding.TryGetValue(
                    definition.BuildingId,
                    out EntityId activeJobId)
                && activeJobId == value.Id
                && value.Status is JobStatus.Blocked or JobStatus.Failed)
            .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        foreach (JobSnapshot job in blocked)
        {
            CellId? recoveryCell = job.AssignedAgentId.HasValue
                ? agents
                    .Where(value => value.Id == job.AssignedAgentId.Value.ToString())
                    .Select(value => (CellId?)new CellId(
                        value.CellX,
                        value.CellY,
                        value.CellZ))
                    .FirstOrDefault()
                : null;
            _cancelBuildingSupply!.Handle(new CancelBuildingSupplyCommand(
                job.Id,
                "blocked_supply_replanned",
                tick,
                recoveryCell));
        }
    }


}

}
