using System.Linq;
using Dig.Application.Production;
using Dig.Domain.Agents;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Xunit;

namespace Dig.Tests
{

public sealed class ProductionMaterialTransitTests
{
    private static readonly RecipeId RecipeId =
        new RecipeId("recipe.production_transit");
    private static readonly EntityId OrderId =
        EntityId.Parse("8a000000000000000000000000000001");
    private static readonly EntityId JobId =
        EntityId.Parse("8a000000000000000000000000000002");
    private static readonly EntityId TransitStackId =
        EntityId.Parse("8a000000000000000000000000000003");


    [Fact]
    public void Internal_stock_direct_pickup_requests_one_available_unit()
    {
        ItemStackSnapshot internalStack = new ItemStackSnapshot(
            ProductionTestHarness.OreStackId,
            ProductionTestHarness.Ore,
            4,
            ItemLocation.InBuilding(ProductionTestHarness.BuildingId),
            System.Array.Empty<ItemQuantityReservationSnapshot>());
        ItemStackSnapshot worldStack = new ItemStackSnapshot(
            EntityId.Parse("8a000000000000000000000000000004"),
            ProductionTestHarness.Ore,
            4,
            ItemLocation.InWorld(new Dig.Domain.World.CellId(1, 1, 0)),
            System.Array.Empty<ItemQuantityReservationSnapshot>());

        Assert.Equal(1, ItemPickupQuantityPolicy.ResolveRequestedQuantity(internalStack));
        Assert.Equal(1, ItemPickupQuantityPolicy.ResolveRequestedQuantity(worldStack));
    }

    [Fact]
    public void Production_worker_stages_raw_before_processing_and_deposits_package()
    {
        RecipeDefinition recipe = new RecipeDefinition(
            RecipeId,
            "Production transit",
            ProductionTestHarness.Workshop,
            new[] { new ContentItemQuantity(ProductionTestHarness.Ore, 1) },
            new[] { new ContentItemQuantity(ProductionTestHarness.Plate, 1) },
            requiredWork: 1,
            energyPerWorkTick: 0,
            materialSteps: new[]
            {
                new RecipeMaterialStepDefinition(
                    ProductionTestHarness.Ore,
                    AgentSkillCatalog.Metallurgy,
                    baseDurationTicks: 3),
            });
        ProductionTestHarness harness = new ProductionTestHarness(new[] { recipe });
        Assert.True(harness.Enqueue(OrderId, RecipeId, tick: 1).IsSuccess);
        Assert.True(harness.Prepare(JobId, tick: 2).IsSuccess);
        harness.AssignAndBegin(OrderId, JobId, tick: 3);

        ApplyProductionWorkHandler work = new ApplyProductionWorkHandler(
            harness.ProductionRepository,
            harness.InventoryRepository,
            harness.JobRepository,
            harness.Agents,
            harness.Journal);
        Assert.Equal(
            ProductionErrors.InvalidStatus,
            work.Handle(new ApplyProductionWorkCommand(
                OrderId,
                JobId,
                baseWork: 1,
                conditionEfficiencyBasisPoints: 10_000,
                tick: 6)).Error);

        Result acquired = new AcquireProductionMaterialHandler(
            harness.ProductionRepository,
            harness.InventoryRepository,
            harness.JobRepository,
            harness.Journal).Handle(new AcquireProductionMaterialCommand(
                OrderId,
                JobId,
                TransitStackId,
                tick: 7));
        Assert.True(acquired.IsSuccess, acquired.Error?.ToString());

        ItemStackSnapshot carried = harness.Inventory.CreateSnapshot().Stacks.Single(value =>
            value.Location.Kind == ItemLocationKind.AgentInventory
            && value.Location.HasOwner
            && value.Location.OwnerId == ProductionTestHarness.WorkerId
            && value.ItemId == ProductionTestHarness.Ore);
        Assert.Equal(1, carried.Quantity);
        Assert.Contains(carried.Reservations, value =>
            value.JobId == OrderId && value.Quantity == 1);

        Result staged = new StageProductionMaterialHandler(
            harness.ProductionRepository,
            harness.InventoryRepository,
            harness.JobRepository,
            harness.Journal).Handle(new StageProductionMaterialCommand(
                OrderId,
                JobId,
                tick: 8));
        Assert.True(staged.IsSuccess, staged.Error?.ToString());
        Assert.DoesNotContain(harness.Inventory.CreateSnapshot().Stacks, value =>
            value.Location.Kind == ItemLocationKind.AgentInventory
            && value.Location.HasOwner
            && value.Location.OwnerId == ProductionTestHarness.WorkerId
            && value.ItemId == ProductionTestHarness.Ore);
        Assert.Equal(9, harness.Inventory.GetTotal(ProductionTestHarness.Ore));
        Assert.Equal(
            ProductionMaterialStepPhase.StagedOnWorkbench,
            harness.Production.Get(OrderId)!.MaterialSteps[0].Phase);

        Assert.True(work.Handle(new ApplyProductionWorkCommand(
            OrderId,
            JobId,
            baseWork: 1,
            conditionEfficiencyBasisPoints: 10_000,
            tick: 9)).IsSuccess);
        ProductionMaterialStepSnapshot processing =
            harness.Production.Get(OrderId)!.MaterialSteps[0];
        Assert.Equal(ProductionMaterialStepPhase.Processing, processing.Phase);
        Assert.Equal(1, processing.CompletedTicks);

        Assert.True(work.Handle(new ApplyProductionWorkCommand(
            OrderId,
            JobId,
            baseWork: 2,
            conditionEfficiencyBasisPoints: 10_000,
            tick: 10)).IsSuccess);
        ProductionOrderSnapshot processed = harness.Production.Get(OrderId)!;
        Assert.Equal(ProductionOrderStatus.InProgress, processed.Status);
        Assert.Equal(
            ProductionMaterialStepPhase.ProcessedAwaitingPackage,
            processed.MaterialSteps[0].Phase);
        Assert.Equal(JobStageKind.PerformWork, harness.Jobs.Get(JobId)!.Stage);

        EntityId packageId = EntityId.Parse("8a000000000000000000000000000005");
        Assert.True(harness.Production.CreateOutputPackage(
            OrderId,
            packageId,
            tick: 11).IsSuccess);
        Result deposited = new DepositProductionMaterialHandler(
            harness.ProductionRepository,
            harness.JobRepository,
            harness.Journal).Handle(new DepositProductionMaterialCommand(
                OrderId,
                JobId,
                packageId,
                tick: 12));
        Assert.True(deposited.IsSuccess, deposited.Error?.ToString());
        Assert.Equal(
            ProductionOrderStatus.ReadyToComplete,
            harness.Production.Get(OrderId)!.Status);
        Assert.Equal(
            ProductionMaterialStepPhase.Deposited,
            harness.Production.Get(OrderId)!.MaterialSteps[0].Phase);
        Assert.Equal(JobStageKind.Finalize, harness.Jobs.Get(JobId)!.Stage);
    }
}

}
