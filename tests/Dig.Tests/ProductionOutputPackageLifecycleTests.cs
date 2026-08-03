using System;
using Dig.Application.Agents;
using Dig.Application.Production;
using Dig.Application.Saving;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class ProductionOutputPackageLifecycleTests
{
    private static readonly CellId OutputCell = new CellId(6, 4, 0);

    [Fact]
    public void Forced_move_discards_used_material_and_resets_same_order()
    {
        CampfireProductionTestHarness harness = ReadyFoodOrder(
            out EntityId orderId,
            out EntityId jobId,
            out EntityId packageId);
        Assert.True(harness.Work(orderId, jobId, 1, tick: 8).IsSuccess);
        Assert.Equal(0, harness.Inventory.GetTotal(
            CampfireProductionContent.MushroomCapItemId));

        Result interrupted = new InterruptProductionOrderHandler(
            harness.ProductionRepository,
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal).Handle(new InterruptProductionOrderCommand(
                orderId,
                jobId,
                "forced_move",
                tick: 9));

        Assert.True(interrupted.IsSuccess, interrupted.Error?.ToString());
        ProductionOrderSnapshot reset = harness.Production.Get(orderId)!;
        Assert.Equal(ProductionOrderStatus.Queued, reset.Status);
        Assert.Equal(0, reset.CompletedWork);
        Assert.All(reset.MaterialSteps, step =>
        {
            Assert.Equal(0, step.RequiredTicks);
            Assert.Equal(0, step.CompletedTicks);
            Assert.False(step.Consumed);
        });
        Assert.Equal(1, harness.Production.GetQueuedCount(
            CampfireProductionTestHarness.BuildingId,
            CampfireProductionContent.GrilledMushroomRecipeId));
        Assert.Null(harness.Production.GetOutputPackage(packageId));
        Assert.Null(harness.Inventory.GetStack(packageId));
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(jobId)!.Status);
        Assert.Equal(0, harness.Inventory.GetTotal(
            CampfireProductionContent.MushroomCapItemId));
    }

    [Fact]
    public void Explicit_cancel_keeps_active_unit_until_normal_close()
    {
        CampfireProductionTestHarness harness = ReadyFoodOrder(
            out EntityId orderId,
            out EntityId jobId,
            out EntityId packageId);
        Result cancelled = new CancelProductionOrderHandler(
            harness.ProductionRepository,
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal).Handle(new CancelProductionOrderCommand(
                orderId,
                jobId,
                "player_cancelled",
                tick: 8));

        Assert.True(cancelled.IsSuccess);
        Assert.Equal(ProductionOrderStatus.InProgress,
            harness.Production.Get(orderId)!.Status);
        Assert.False(harness.Jobs.Get(jobId)!.IsTerminal);
        Assert.NotNull(harness.Inventory.GetStack(packageId));

        Assert.True(harness.Work(orderId, jobId, 1, tick: 9).IsSuccess);
        Result completed = CompleteStagedFood(
            harness,
            orderId,
            jobId,
            packageId,
            tick: 10);

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        Assert.Equal(ProductionOrderStatus.Completed,
            harness.Production.Get(orderId)!.Status);
        Assert.Equal(0, harness.Production.GetQueuedCount(
            CampfireProductionTestHarness.BuildingId,
            CampfireProductionContent.GrilledMushroomRecipeId));
        Assert.Equal(ProductionPackageContent.FoodPackageItemId,
            harness.Inventory.GetStack(packageId)!.ItemId);
    }

    [Fact]
    public void Closed_food_package_breaks_into_separate_units_exactly_once()
    {
        CampfireProductionTestHarness harness = ReadyFoodOrder(
            out EntityId orderId,
            out EntityId productionJobId,
            out EntityId packageId);
        Assert.True(harness.Work(orderId, productionJobId, 1, tick: 8).IsSuccess);
        Assert.True(CompleteStagedFood(
            harness,
            orderId,
            productionJobId,
            packageId,
            tick: 9).IsSuccess);
        ProductionOutputPackageSnapshot package =
            harness.Production.GetOutputPackage(packageId)!;
        Assert.True(package.IsClosed);
        Assert.Equal(ProductionOutputPackageKind.Food, package.Kind);
        Assert.Single(package.Manifest);
        Assert.Equal(2, package.Manifest[0].Quantity);
        Assert.Equal(2,
            ProductionPackageMaterialization.RequiredOutputStackCount(package));
        Assert.Equal(
            JobStageKind.TravelToDestination,
            harness.Jobs.Get(productionJobId)!.Stage);
        Assert.True(harness.Jobs.AdvanceStage(
            productionJobId,
            tick: 10).IsSuccess);
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(productionJobId)!.Status);

        EntityId useJobId = CampfireProductionTestHarness.Id(204);
        EntityId firstOutputId = CampfireProductionTestHarness.Id(205);
        EntityId secondOutputId = CampfireProductionTestHarness.Id(206);
        Assert.True(new StartProductionPackageUseHandler(
            harness.ProductionRepository,
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal).Handle(new StartProductionPackageUseCommand(
                useJobId,
                packageId,
                CampfireProductionTestHarness.WorkerId,
                new CellId(5, 4, 0),
                priority: 800,
                tick: 11)).IsSuccess);
        AdvanceProductionPackageUseHandler advance =
            new AdvanceProductionPackageUseHandler(
                harness.JobsRepository,
                harness.Journal);
        Assert.True(advance.Handle(
            new AdvanceProductionPackageUseCommand(useJobId, 12)).IsSuccess);
        Assert.True(advance.Handle(
            new AdvanceProductionPackageUseCommand(useJobId, 13)).IsSuccess);
        CompleteProductionPackageUseHandler complete =
            new CompleteProductionPackageUseHandler(
                harness.ProductionRepository,
                harness.InventoryRepository,
                harness.JobsRepository,
                harness.Journal);

        Result opened = complete.Handle(new CompleteProductionPackageUseCommand(
            useJobId,
            new[] { firstOutputId, secondOutputId },
            tick: 14));

        Assert.True(opened.IsSuccess, opened.Error?.ToString());
        Assert.Null(harness.Production.GetOutputPackage(packageId));
        Assert.Null(harness.Inventory.GetStack(packageId));
        ItemStackSnapshot first = harness.Inventory.GetStack(firstOutputId)!;
        ItemStackSnapshot second = harness.Inventory.GetStack(secondOutputId)!;
        Assert.Equal(CampfireProductionContent.GrilledMushroomItemId, first.ItemId);
        Assert.Equal(CampfireProductionContent.GrilledMushroomItemId, second.ItemId);
        Assert.Equal(1, first.Quantity);
        Assert.Equal(1, second.Quantity);
        Assert.Equal(ItemLocation.InWorld(OutputCell), first.Location);
        Assert.Equal(ItemLocation.InWorld(OutputCell), second.Location);
        Assert.True(complete.Handle(new CompleteProductionPackageUseCommand(
            useJobId,
            new[] { CampfireProductionTestHarness.Id(207) },
            tick: 15)).IsFailure);
        Assert.Equal(2, harness.Inventory.GetTotal(
            CampfireProductionContent.GrilledMushroomItemId));
    }

    [Fact]
    public void Building_output_transforms_package_into_existing_building_box()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness(1);
        EntityId orderId = CampfireProductionTestHarness.Id(220);
        EntityId jobId = CampfireProductionTestHarness.Id(221);
        EntityId packageId = CampfireProductionTestHarness.Id(222);
        harness.AddBuildingStock(CampfireProductionContent.MushroomLegItemId, 2, 223);
        harness.AddBuildingStock(CampfireProductionContent.MushroomCapItemId, 1, 224);
        Assert.True(harness.Enqueue(
            orderId,
            CampfireProductionContent.TentRecipeId,
            tick: 1).IsSuccess);
        Assert.True(harness.Prepare(jobId, tick: 2).IsSuccess);
        Assert.True(harness.Jobs.Claim(
            jobId,
            CampfireProductionTestHarness.WorkerId,
            tick: 3).IsSuccess);
        Assert.True(CreatePackage(harness, orderId, jobId, packageId, tick: 4).IsSuccess);
        BeginAndReachWork(harness, orderId, jobId, tick: 5);
        Assert.True(harness.Work(orderId, jobId, 3, tick: 8).IsSuccess);

        Result completed = new CompleteProductionOrderHandler(
            harness.ProductionRepository,
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal,
            harness.SkillGrants).Handle(new CompleteProductionOrderCommand(
                orderId,
                jobId,
                new[] { packageId },
                tick: 9,
                ItemLocation.InWorld(OutputCell),
                packageId));

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        Assert.Null(harness.Production.GetOutputPackage(packageId));
        ItemStackSnapshot box = harness.Inventory.GetStack(packageId)!;
        Assert.Equal(CampfireProductionContent.TentBoxItemId, box.ItemId);
        Assert.Equal(1, box.Quantity);
        Assert.Equal(ItemLocation.InWorld(OutputCell), box.Location);
    }

    [Fact]
    public void Unfinished_package_round_trips_as_inventory_entity()
    {
        CampfireProductionTestHarness harness = ReadyFoodOrder(
            out EntityId orderId,
            out _,
            out EntityId packageId);
        BuildingProductionSaveData saved = BuildingProductionSaveAdapter.Encode(
            harness.Production,
            harness.Supply,
            harness.Inventory);

        Result<RestoredBuildingProductionState> restored =
            BuildingProductionSaveAdapter.Decode(
                saved,
                harness.Content,
                harness.Inventory);

        Assert.True(restored.IsSuccess, restored.Error?.ToString());
        ProductionOutputPackageSnapshot package = restored.Value.Production
            .GetOutputPackage(packageId)!;
        Assert.Equal(orderId, package.OrderId);
        Assert.Equal(ProductionOutputPackageKind.Unfinished, package.Kind);
        Assert.Empty(package.Manifest);
        Assert.Equal(ProductionPackageContent.UnfinishedPackageItemId,
            harness.Inventory.GetStack(packageId)!.ItemId);
    }

    private static CampfireProductionTestHarness ReadyFoodOrder(
        out EntityId orderId,
        out EntityId jobId,
        out EntityId packageId)
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness(1);
        orderId = CampfireProductionTestHarness.Id(200);
        jobId = CampfireProductionTestHarness.Id(201);
        packageId = CampfireProductionTestHarness.Id(202);
        harness.AddBuildingStock(CampfireProductionContent.MushroomCapItemId, 1, 203);
        Assert.True(harness.Enqueue(
            orderId,
            CampfireProductionContent.GrilledMushroomRecipeId,
            tick: 1).IsSuccess);
        Assert.True(harness.Prepare(jobId, tick: 2).IsSuccess);
        Assert.True(harness.Jobs.Claim(
            jobId,
            CampfireProductionTestHarness.WorkerId,
            tick: 3).IsSuccess);
        Assert.True(CreatePackage(harness, orderId, jobId, packageId, tick: 4).IsSuccess);
        BeginAndReachWork(harness, orderId, jobId, tick: 5);
        return harness;
    }

    private static Result CreatePackage(
        CampfireProductionTestHarness harness,
        EntityId orderId,
        EntityId jobId,
        EntityId packageId,
        long tick)
    {
        return new CreateProductionOutputPackageHandler(
            harness.ProductionRepository,
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal).Handle(new CreateProductionOutputPackageCommand(
                orderId,
                jobId,
                packageId,
                ItemLocation.InWorld(OutputCell),
                tick));
    }

    private static void BeginAndReachWork(
        CampfireProductionTestHarness harness,
        EntityId orderId,
        EntityId jobId,
        long tick)
    {
        Assert.True(new BeginProductionWorkHandler(
            harness.ProductionRepository,
            harness.JobsRepository,
            harness.Agents,
            harness.Journal).Handle(new BeginProductionWorkCommand(
                orderId,
                jobId,
                tick)).IsSuccess);
        Assert.True(harness.Jobs.AdvanceStage(jobId, tick + 1).IsSuccess);
    }

    private static Result CompleteStagedFood(
        CampfireProductionTestHarness harness,
        EntityId orderId,
        EntityId jobId,
        EntityId packageId,
        long tick)
    {
        return new CompleteProductionOrderHandler(
            harness.ProductionRepository,
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal,
            harness.SkillGrants).Handle(new CompleteProductionOrderCommand(
                orderId,
                jobId,
                Array.Empty<EntityId>(),
                tick,
                ItemLocation.InWorld(OutputCell),
                packageId));
    }
}

}
