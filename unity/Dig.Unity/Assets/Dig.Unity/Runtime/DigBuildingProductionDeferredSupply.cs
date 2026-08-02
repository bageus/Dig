using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Production;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private void ResolveEligibleDeferredSupplyJobs(
        long tick,
        IReadOnlyList<AgentViewModel> agents,
        Dig.Domain.Navigation.NavigationSnapshot navigation)
    {
        CellId[] revealed = GetProductionRevealedCells();
        JobSystem jobs = _jobRepository.Get();
        foreach (JobSnapshot pending in jobs.GetAll()
            .Where(value => value.Status == JobStatus.Created
                && value.Definition is BuildingSupplyJobDefinition supply
                && !supply.IsSourceResolved)
            .OrderBy(value => value.Definition.CreatedTick)
            .ThenBy(value => value.Id.ToString(), StringComparer.Ordinal))
        {
            BuildingSupplyJobDefinition supply =
                (BuildingSupplyJobDefinition)pending.Definition;
            if (HasNonTerminalProductionWorkJob(supply.BuildingId))
            {
                continue;
            }

            JobSnapshot?[] dependencies = supply.Dependencies
                .Select(jobs.Get)
                .ToArray();
            if (dependencies.Any(value => value == null
                || (value.IsTerminal && value.Status != JobStatus.Completed)))
            {
                _cancelDeferredBuildingSupply!.Handle(
                    new CancelDeferredBuildingSupplyJobCommand(
                        pending.Id,
                        "The extraction dependency did not complete successfully.",
                        tick));
                jobs = _jobRepository.Get();
                continue;
            }

            if (!jobs.AreDependenciesCompleted(pending.Id))
            {
                continue;
            }

            BuildingSnapshot? building = _buildingsRepository!.Get().Get(
                supply.BuildingId);
            if (building == null || building.Status != BuildingStatus.Completed)
            {
                continue;
            }

            InventorySnapshot inventory = _buildingInventoryRepository!.Get()
                .CreateSnapshot();
            if (!BuildingSupplyDependencyPlanner.HasRequestedWorldQuantity(
                    supply.RequestedItems,
                    inventory.Stacks))
            {
                _cancelDeferredBuildingSupply!.Handle(
                    new CancelDeferredBuildingSupplyJobCommand(
                        pending.Id,
                        "The completed extraction dependency produced no remaining world source.",
                        tick));
                jobs = _jobRepository.Get();
                continue;
            }

            CellId[] reachable = GetProductionReachableCells(
                navigation,
                building.WorkPosition).ToArray();
            AgentViewModel[] candidates = agents
                .Where(value => IsAvailableForAutomaticWork(value)
                    && reachable.Contains(new CellId(
                        value.CellX,
                        value.CellY,
                        value.CellZ)))
                .OrderBy(value => Distance(value, building.WorkPosition))
                .ThenBy(value => value.Id, StringComparer.Ordinal)
                .ToArray();
            foreach (AgentViewModel resident in candidates)
            {
                Result resolved = _resolveDeferredBuildingSupply!.Handle(
                    new ResolveDeferredBuildingSupplyJobCommand(
                        pending.Id,
                        EntityId.Parse(resident.Id),
                        revealed,
                        reachable,
                        tick));
                if (resolved.IsSuccess)
                {
                    jobs = _jobRepository.Get();
                    break;
                }
            }
        }
    }

    private bool HasNonTerminalProductionWorkJob(EntityId buildingId)
    {
        return _jobRepository.Get().GetAll().Any(value =>
            !value.IsTerminal
            && value.Definition is ProductionWorkJobDefinition production
            && production.BuildingId == buildingId);
    }

    private bool HasNonTerminalBuildingSupplyJob(EntityId buildingId)
    {
        return _jobRepository.Get().GetAll().Any(value =>
            !value.IsTerminal
            && value.Definition is BuildingSupplyJobDefinition supply
            && supply.BuildingId == buildingId);
    }

}

}
