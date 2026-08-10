using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Ecology;
using Dig.Application.Jobs;
using Dig.Application.Production;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Dig.Presentation.Production;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    internal BuildingProductionViewModel? LoadBuildingProduction(string buildingId)
    {
        EnsureBuildingProductionInitialized();
        if (string.IsNullOrWhiteSpace(buildingId))
        {
            throw new ArgumentException("Building id is required.", nameof(buildingId));
        }

        EntityId id = EntityId.Parse(buildingId);
        BuildingSnapshot? building = _buildingsRepository!.Get().Get(id);
        if (building == null
            || building.Status != BuildingStatus.Completed
            || !_productionContent!.ContainsWorkstation(building.Definition.Id))
        {
            return null;
        }

        BuildingSupplySnapshot? supply = _buildingSupplyRepository!.Get().Get(
            id,
            _buildingInventoryRepository!.Get().CreateSnapshot());
        return supply == null
            ? null
            : _buildingProductionPresenter!.Present(
                id,
                _productionRepository!.Get(),
                supply);
    }

    internal IReadOnlyList<BuildingProductionViewModel> LoadAllBuildingProduction()
    {
        EnsureBuildingProductionInitialized();
        InventorySnapshot inventory = _buildingInventoryRepository!.Get().CreateSnapshot();
        return _buildingSupplyRepository!.Get().GetAll(inventory)
            .Where(value => _buildingsRepository!.Get().Get(value.BuildingId)?.Status
                == BuildingStatus.Completed)
            .Select(value => _buildingProductionPresenter!.Present(
                value.BuildingId,
                _productionRepository!.Get(),
                value))
            .ToArray();
    }

    internal IReadOnlyList<BuildingInternalStockUnitViewModel>
        LoadAllBuildingInternalStockUnits()
    {
        EnsureBuildingProductionInitialized();
        return _buildingInventoryRepository!.Get().CreateSnapshot().Stacks
            .Where(stack => stack.Location.Kind == ItemLocationKind.BuildingInventory
                && stack.Location.HasOwner)
            .OrderBy(stack => stack.Location.OwnerId.ToString(), StringComparer.Ordinal)
            .ThenBy(stack => stack.ItemId)
            .ThenBy(stack => stack.StackId.ToString(), StringComparer.Ordinal)
            .SelectMany(stack => Enumerable.Range(0, stack.Quantity)
                .Select(unitIndex => new BuildingInternalStockUnitViewModel(
                    stack.StackId.ToString(),
                    stack.Location.OwnerId,
                    stack.ItemId,
                    unitIndex,
                    isAvailable: unitIndex < stack.AvailableQuantity,
                    _buildingInventoryRepository.Get().Catalog
                        .Get(stack.ItemId).Interactions)))
            .ToArray();
    }

    internal Result EnqueueBuildingProduction(
        string buildingId,
        string recipeId,
        long tick)
    {
        EnsureBuildingProductionInitialized();
        EntityId id = EntityId.Parse(buildingId);
        RecipeId recipe = new RecipeId(recipeId);
        BuildingSnapshot? building = _buildingsRepository!.Get().Get(id);
        if (building == null
            || building.Status != BuildingStatus.Completed
            || !_productionContent!.ContainsWorkstation(building.Definition.Id)
            || !_productionContent.GetWorkstation(building.Definition.Id)
                .RecipeIds.Contains(recipe))
        {
            return Result.Failure(ProductionErrors.WorkstationMismatch);
        }

        EntityId orderId = NextProductionEntityId(
            'e',
            ref _nextProductionOrderSequence);
        return _enqueueProduction!.Handle(new EnqueueProductionOrderCommand(
            orderId,
            recipe,
            id,
            tick));
    }

    internal Result CancelOneBuildingProduction(
        string buildingId,
        string recipeId,
        long tick)
    {
        EnsureBuildingProductionInitialized();
        EntityId building = EntityId.Parse(buildingId);
        RecipeId recipe = new RecipeId(recipeId);
        ProductionOrderSnapshot? order = _productionRepository!.Get().GetAll()
            .Where(value => value.BuildingId == building
                && value.Recipe.Id == recipe
                && !value.IsTerminal)
            .OrderBy(value => value.Status == ProductionOrderStatus.Queued ? 0 : 1)
            .ThenByDescending(value => value.Sequence)
            .FirstOrDefault();
        if (order == null)
        {
            return Result.Failure(ProductionErrors.OrderNotFound);
        }

        EntityId jobId = _jobRepository.Get().GetAll()
            .Where(value => value.Definition is ProductionWorkJobDefinition work
                && work.OrderId == order.Id
                && !value.IsTerminal)
            .Select(value => value.Id)
            .FirstOrDefault();
        Result result = _cancelProduction!.Handle(new CancelProductionOrderCommand(
            order.Id,
            jobId,
            "player_cancelled",
            tick));
        if (result.IsSuccess
            && !jobId.IsEmpty
            && (_jobRepository.Get().Get(jobId)?.IsTerminal ?? true))
        {
            _buildingProductionRoutes.Remove(jobId);
        }

        if (result.IsSuccess)
        {
            CancelDeferredSupplyForCancelledOrder(building, tick);
        }

        return result;
    }

    private void CancelDeferredSupplyForCancelledOrder(
        EntityId buildingId,
        long tick)
    {
        JobSnapshot[] pending = _jobRepository!.Get().GetAll()
            .Where(value => !value.IsTerminal
                && value.Definition is BuildingSupplyJobDefinition supply
                && supply.BuildingId == buildingId
                && !supply.IsSourceResolved)
            .ToArray();
        foreach (JobSnapshot delivery in pending)
        {
            BuildingSupplyJobDefinition supply =
                (BuildingSupplyJobDefinition)delivery.Definition;
            _cancelDeferredBuildingSupply!.Handle(
                new CancelDeferredBuildingSupplyJobCommand(
                    delivery.Id,
                    "The owning production order was cancelled.",
                    tick));
            foreach (EntityId dependencyId in supply.Dependencies)
            {
                JobSnapshot? dependency = _jobRepository.Get().Get(dependencyId);
                if (dependency?.Definition is MushroomChopJobDefinition
                    && !dependency.IsTerminal)
                {
                    _cancelMushroomChop!.Handle(new CancelMushroomChopCommand(
                        dependencyId,
                        "production_order_cancelled",
                        tick));
                }
            }
        }
    }

    internal Result SetBuildingStockDelivery(
        string buildingId,
        string itemId,
        bool enabled,
        long tick)
    {
        EnsureBuildingProductionInitialized();
        return _setBuildingStockDelivery!.Handle(
            new SetBuildingStockDeliveryCommand(
                EntityId.Parse(buildingId),
                new ItemId(itemId),
                enabled,
                tick));
    }

}

}
