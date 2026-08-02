using Dig.Application.Production;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class ProductionWorldOutputAtomicityTests
{
    private static readonly RecipeId Recipe =
        new RecipeId("recipe.copper_plate");
    private static readonly EntityId OrderId =
        EntityId.Parse("94000000000000000000000000000001");
    private static readonly EntityId JobId =
        EntityId.Parse("95000000000000000000000000000001");
    private static readonly EntityId OutputId =
        EntityId.Parse("96000000000000000000000000000001");

    [Fact]
    public void World_output_order_and_job_commit_terminal_state_together()
    {
        ProductionTestHarness harness = new ProductionTestHarness(new[]
        {
            ProductionContentCatalogTests.CreateRecipe(requiredTechnology: null),
        });
        Assert.True(harness.Enqueue(OrderId, Recipe, tick: 1).IsSuccess);
        Assert.True(harness.Prepare(JobId, tick: 2).IsSuccess);
        harness.AssignAndBegin(OrderId, JobId, tick: 3);
        Assert.True(harness.ApplyWork(OrderId, JobId, tick: 6).IsSuccess);
        CellId outputCell = new CellId(6, 2, 0);
        CompleteProductionOrderHandler handler = new CompleteProductionOrderHandler(
            harness.ProductionRepository,
            harness.InventoryRepository,
            harness.JobRepository,
            harness.Journal,
            harness.SkillGrants);

        Result completed = handler.Handle(new CompleteProductionOrderCommand(
            OrderId,
            JobId,
            new[] { OutputId },
            tick: 7,
            ItemLocation.InWorld(outputCell)));

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        ItemStackSnapshot output = harness.Inventory.GetStack(OutputId)!;
        Assert.Equal(ItemLocationKind.World, output.Location.Kind);
        Assert.Equal(outputCell, output.Location.CellId);
        Assert.Equal(
            ProductionOrderStatus.Completed,
            harness.Production.Get(OrderId)!.Status);
        Assert.Equal(JobStatus.InProgress, harness.Jobs.Get(JobId)!.Status);
        Assert.Equal(
            JobStageKind.TravelToDestination,
            harness.Jobs.Get(JobId)!.Stage);
        Assert.Contains(
            harness.Jobs.GetReservations(),
            value => value.JobId == JobId
                && value.Key == ReservationKey.ForDestination(
                    ProductionTestHarness.BuildingId));
        Assert.True(harness.Jobs.AdvanceStage(JobId, tick: 8).IsSuccess);
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(JobId)!.Status);
        Assert.Empty(harness.Jobs.GetReservations());

        Result replay = handler.Handle(new CompleteProductionOrderCommand(
            OrderId,
            JobId,
            new[]
            {
                EntityId.Parse("96000000000000000000000000000002"),
            },
            tick: 9,
            ItemLocation.InWorld(outputCell)));
        Assert.True(replay.IsFailure);
        Assert.Equal(1, harness.Inventory.GetTotal(ProductionTestHarness.Plate));
    }
}

}
