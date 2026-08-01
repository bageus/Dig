using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Production;

namespace Dig.Application.Saving
{

public static partial class BuildingProductionSaveAdapter
{
    public static BuildingProductionSaveData Encode(
        ProductionState production,
        BuildingSupplyState supply,
        InventoryState inventory)
    {
        if (production is null || supply is null || inventory is null)
        {
            throw new ArgumentNullException(nameof(production));
        }

        BuildingProductionSaveData data = new BuildingProductionSaveData();
        foreach (ProductionOrderSnapshot order in production.GetAll())
        {
            data.Orders.Add(new ProductionOrderSaveData
            {
                OrderId = order.Id.ToString(),
                RecipeId = order.Recipe.Id.ToString(),
                BuildingId = order.BuildingId.ToString(),
                Sequence = order.Sequence,
                Status = (int)order.Status,
                CompletedWork = order.CompletedWork,
                Version = order.Version,
                Reason = order.Reason,
                InputAllocations = order.InputAllocations.Select(value =>
                    new ProductionInputAllocationSaveData
                    {
                        StackId = value.StackId.ToString(),
                        ItemId = value.ItemId.ToString(),
                        Quantity = value.Quantity,
                    }).ToList(),
                MaterialSteps = order.MaterialSteps.Select(value =>
                    new ProductionMaterialStepSaveData
                    {
                        Index = value.Index,
                        ItemId = value.ItemId.ToString(),
                        RequiredTicks = value.RequiredTicks,
                        CompletedTicks = value.CompletedTicks,
                        IsConsumed = value.Consumed,
                        Phase = (int)value.Phase,
                    }).ToList(),
            });
        }

        foreach (ProductionOutputPackageSnapshot package
            in production.GetOutputPackages())
        {
            data.Packages.Add(new ProductionOutputPackageSaveData
            {
                StackId = package.StackId.ToString(),
                OrderId = package.OrderId.ToString(),
                Kind = (int)package.Kind,
                Version = package.Version,
                Manifest = package.Manifest.Select(value =>
                    new ProductionPackageManifestItemSaveData
                    {
                        ItemId = value.ItemId.ToString(),
                        Quantity = value.Quantity,
                    }).ToList(),
            });
        }

        foreach (BuildingSupplySnapshot snapshot in supply.GetAll(
            inventory.CreateSnapshot()))
        {
            data.Supplies.Add(new BuildingSupplySaveData
            {
                BuildingId = snapshot.BuildingId.ToString(),
                WorkstationId = snapshot.Definition.BuildingId.ToString(),
                ActiveSupplyJobId = snapshot.ActiveSupplyJobId?.ToString(),
                Stocks = snapshot.Stocks.Select(value => new BuildingStockRuleSaveData
                {
                    ItemId = value.ItemId.ToString(),
                    Incoming = value.Incoming,
                    DeliveryEnabled = value.DeliveryEnabled,
                }).ToList(),
            });
        }

        return data;
    }

    public static Result<RestoredBuildingProductionState> Decode(
        BuildingProductionSaveData? data,
        ProductionContentCatalog content,
        InventoryState inventory)
    {
        if (content is null || inventory is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        try
        {
            BuildingProductionSaveData source = data ?? new BuildingProductionSaveData();
            ProductionState production = RestoreProduction(
                source,
                content,
                inventory);
            BuildingSupplyState supply = RestoreSupply(source, content, inventory);
            return Result<RestoredBuildingProductionState>.Success(
                new RestoredBuildingProductionState(production, supply));
        }
        catch (Exception)
        {
            return Result<RestoredBuildingProductionState>.Failure(
                SaveErrors.InvalidDocument);
        }
    }

    private static ProductionState RestoreProduction(
        BuildingProductionSaveData data,
        ProductionContentCatalog content,
        InventoryState inventory)
    {
        ProductionState production = new ProductionState();
        long tick = 0;
        foreach (ProductionOrderSaveData saved in data.Orders
            .OrderBy(value => value.Sequence))
        {
            if (!Enum.IsDefined(typeof(ProductionOrderStatus), saved.Status))
            {
                throw new InvalidOperationException("Invalid production status.");
            }

            EntityId orderId = EntityId.Parse(saved.OrderId);
            EntityId buildingId = EntityId.Parse(saved.BuildingId);
            RecipeDefinition recipe = content.GetRecipe(new RecipeId(saved.RecipeId));
            RequireSuccess(production.Enqueue(orderId, recipe, buildingId, tick++));
            ProductionOrderStatus status = (ProductionOrderStatus)saved.Status;
            if (NeedsReservedState(status, saved))
            {
                ItemReservationAllocation[] allocations = saved.InputAllocations
                    .Select(value => new ItemReservationAllocation(
                        EntityId.Parse(value.StackId),
                        new ItemId(value.ItemId),
                        value.Quantity))
                    .ToArray();
                RequireSuccess(production.ReserveInputs(orderId, allocations, tick++));
            }

            if (NeedsStartedState(status, saved))
            {
                long[] durations = recipe.UsesMaterialSteps
                    ? saved.MaterialSteps.OrderBy(value => value.Index)
                        .Select(value => value.RequiredTicks)
                        .ToArray()
                    : Array.Empty<long>();
                RequireSuccess(production.Start(
                    orderId,
                    tick++,
                    recipe.UsesMaterialSteps ? durations : null));
                RestoreProgress(
                    production,
                    inventory,
                    saved,
                    recipe,
                    orderId,
                    tick++);
            }

            if (status == ProductionOrderStatus.Completed)
            {
                RequireSuccess(production.Complete(orderId, tick++));
            }
            else if (status == ProductionOrderStatus.Cancelled)
            {
                RequireSuccess(production.Cancel(
                    orderId,
                    saved.Reason ?? "restored_cancelled",
                    tick++));
            }
            else if (status == ProductionOrderStatus.Failed)
            {
                RequireSuccess(production.Fail(
                    orderId,
                    saved.Reason ?? "restored_failed",
                    tick++));
            }
        }

        foreach (ProductionOutputPackageSaveData saved in data.Packages
            .OrderBy(value => value.StackId, StringComparer.Ordinal))
        {
            if (!Enum.IsDefined(typeof(ProductionOutputPackageKind), saved.Kind))
            {
                throw new InvalidOperationException("Invalid production package kind.");
            }

            ProductionOutputPackageKind kind =
                (ProductionOutputPackageKind)saved.Kind;
            if (kind == ProductionOutputPackageKind.Building)
            {
                throw new InvalidOperationException(
                    "Building packages are restored through BuildingBox state.");
            }

            EntityId stackId = EntityId.Parse(saved.StackId);
            ItemStackSnapshot? stack = inventory.GetStack(stackId);
            ItemId expectedItemId = kind == ProductionOutputPackageKind.Unfinished
                ? ProductionPackageContent.UnfinishedPackageItemId
                : ProductionPackageContent.GetClosedItemId(kind);
            if (stack == null
                || stack.ItemId != expectedItemId
                || stack.Quantity != 1)
            {
                throw new InvalidOperationException(
                    "Saved production package inventory identity is invalid.");
            }

            ContentItemQuantity[] manifest = saved.Manifest.Select(value =>
                new ContentItemQuantity(
                    new ItemId(value.ItemId),
                    value.Quantity)).ToArray();
            RequireSuccess(production.RestoreOutputPackage(
                stackId,
                EntityId.Parse(saved.OrderId),
                kind,
                saved.Version,
                manifest));
        }

        production.DequeueUncommittedEvents();
        return production;
    }

    private static BuildingSupplyState RestoreSupply(
        BuildingProductionSaveData data,
        ProductionContentCatalog content,
        InventoryState inventory)
    {
        BuildingSupplyState supply = new BuildingSupplyState();
        InventorySnapshot snapshot = inventory.CreateSnapshot();
        long tick = 0;
        foreach (BuildingSupplySaveData saved in data.Supplies
            .OrderBy(value => value.BuildingId, StringComparer.Ordinal))
        {
            EntityId buildingId = EntityId.Parse(saved.BuildingId);
            ProductionWorkstationDefinition definition = content.GetWorkstation(
                new Dig.Domain.Buildings.BuildingDefinitionId(saved.WorkstationId));
            RequireSuccess(supply.Register(buildingId, definition, tick++));
            foreach (BuildingStockRuleSaveData stock in saved.Stocks)
            {
                RequireSuccess(supply.SetDeliveryEnabled(
                    buildingId,
                    new ItemId(stock.ItemId),
                    stock.DeliveryEnabled,
                    tick++));
            }

            if (!string.IsNullOrWhiteSpace(saved.ActiveSupplyJobId))
            {
                ItemConsumptionRequest[] incoming = saved.Stocks
                    .Where(value => value.Incoming > 0)
                    .Select(value => new ItemConsumptionRequest(
                        new ItemId(value.ItemId),
                        value.Incoming))
                    .ToArray();
                Dictionary<ItemId, int> current = definition.StockRules.ToDictionary(
                    value => value.ItemId,
                    value => snapshot.GetQuantityAt(
                        value.ItemId,
                        ItemLocation.InBuilding(buildingId)));
                RequireSuccess(supply.ReserveIncoming(
                    buildingId,
                    EntityId.Parse(saved.ActiveSupplyJobId),
                    incoming,
                    current,
                    tick++));
            }
        }

        return supply;
    }

    private static bool NeedsReservedState(
        ProductionOrderStatus status,
        ProductionOrderSaveData saved)
    {
        return saved.InputAllocations.Count > 0
            || status is ProductionOrderStatus.InputsReserved
                or ProductionOrderStatus.InProgress
                or ProductionOrderStatus.ReadyToComplete
                or ProductionOrderStatus.Completed;
    }

    private static bool NeedsStartedState(
        ProductionOrderStatus status,
        ProductionOrderSaveData saved)
    {
        return saved.MaterialSteps.Any(value => value.RequiredTicks > 0)
            || saved.CompletedWork > 0
            || status is ProductionOrderStatus.InProgress
                or ProductionOrderStatus.ReadyToComplete
                or ProductionOrderStatus.Completed;
    }

    private static void RequireSuccess(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.ToString());
        }
    }
}

public sealed class RestoredBuildingProductionState
{
    public RestoredBuildingProductionState(
        ProductionState production,
        BuildingSupplyState supply)
    {
        Production = production ?? throw new ArgumentNullException(nameof(production));
        Supply = supply ?? throw new ArgumentNullException(nameof(supply));
    }

    public ProductionState Production { get; }
    public BuildingSupplyState Supply { get; }
}

}
