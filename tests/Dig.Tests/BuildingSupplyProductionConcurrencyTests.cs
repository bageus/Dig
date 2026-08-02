using System.Linq;
using Dig.Application.Production;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingSupplyProductionConcurrencyTests
{
    [Fact]
    public void Production_and_supply_claim_different_residents_at_the_same_workstation()
    {
        JobSystem jobs = new JobSystem();
        EntityId buildingId = Id(1);
        EntityId productionJobId = Id(2);
        EntityId supplyJobId = Id(3);
        EntityId productionWorkerId = Id(4);
        EntityId supplyWorkerId = Id(5);
        EntityId sourceStackId = Id(6);
        CellId workPosition = new CellId(4, 3, 0);

        ProductionWorkJobDefinition production = new ProductionWorkJobDefinition(
            productionJobId,
            Id(7),
            buildingId,
            CampfireProductionContent.GrilledMushroomRecipeId,
            workPosition,
            700,
            0,
            JobRetryPolicy.Default);
        BuildingSupplyJobDefinition supply = new BuildingSupplyJobDefinition(
            supplyJobId,
            buildingId,
            workPosition,
            new[]
            {
                new ItemReservationAllocation(
                    sourceStackId,
                    CampfireProductionContent.MushroomCapItemId,
                    quantity: 1),
            },
            new[] { Id(8) },
            new[] { Id(9) },
            650,
            0,
            JobRetryPolicy.Default);

        Assert.True(jobs.Add(production).IsSuccess);
        Assert.True(jobs.MakeAvailable(productionJobId, 0).IsSuccess);
        Assert.True(jobs.Claim(productionJobId, productionWorkerId, 0).IsSuccess);
        Assert.True(jobs.Add(supply).IsSuccess);
        Assert.True(jobs.MakeAvailable(supplyJobId, 0).IsSuccess);

        Result claimed = jobs.Claim(supplyJobId, supplyWorkerId, 0);

        Assert.True(claimed.IsSuccess, claimed.Error?.ToString());
        Assert.Contains(
            jobs.GetReservations(),
            value => value.JobId == productionJobId
                && value.Key == ReservationKey.ForPosition(workPosition));
        Assert.DoesNotContain(
            jobs.GetReservations(),
            value => value.JobId == supplyJobId
                && value.Key == ReservationKey.ForPosition(workPosition));
        Assert.Contains(
            jobs.GetReservations(),
            value => value.JobId == supplyJobId
                && value.Key == ReservationKey.ForDestination(buildingId));
    }

    [Fact]
    public void Create_supply_succeeds_while_production_owns_the_work_position()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness();
        EntityId orderId = Id(20);
        EntityId productionJobId = Id(21);
        EntityId supplyJobId = Id(22);
        EntityId supplyWorkerId = Id(23);
        EntityId sourceStackId = Id(24);
        CellId sourceCell = new CellId(1, 3, 0);
        CellId workPosition = harness.Buildings.Get(
            CampfireProductionTestHarness.BuildingId)!.WorkPosition;
        harness.AddBuildingStock(
            CampfireProductionContent.MushroomCapItemId,
            quantity: 1,
            id: 25);
        Assert.True(harness.Agents.Add(
            AgentTestFactory.CreateAgent(id: supplyWorkerId)).IsSuccess);
        Assert.True(harness.Enqueue(
            orderId,
            CampfireProductionContent.GrilledMushroomRecipeId,
            tick: 1).IsSuccess);
        Assert.True(harness.Prepare(productionJobId, tick: 2).IsSuccess);
        Assert.True(harness.Jobs.Claim(
            productionJobId,
            CampfireProductionTestHarness.WorkerId,
            tick: 3).IsSuccess);
        Assert.True(harness.Inventory.AddStack(
            sourceStackId,
            CampfireProductionContent.MushroomCapItemId,
            quantity: 1,
            ItemLocation.InWorld(sourceCell),
            tick: 3).IsSuccess);

        CreateBuildingSupplyJobHandler handler = new CreateBuildingSupplyJobHandler(
            harness.Content,
            harness.SupplyRepository,
            harness.BuildingsRepository,
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal);
        Result created = handler.Handle(new CreateBuildingSupplyJobCommand(
            supplyJobId,
            CampfireProductionTestHarness.BuildingId,
            supplyWorkerId,
            new[] { sourceCell, workPosition },
            new[] { sourceCell, workPosition },
            new[] { Id(30) },
            new[] { Id(33) },
            priority: 650,
            tick: 4));

        Assert.True(created.IsSuccess, created.Error?.ToString());
        Assert.Equal(JobStatus.Claimed, harness.Jobs.Get(productionJobId)!.Status);
        Assert.Equal(JobStatus.Claimed, harness.Jobs.Get(supplyJobId)!.Status);
        Assert.Equal(
            supplyJobId,
            harness.Supply.Get(
                CampfireProductionTestHarness.BuildingId,
                harness.Inventory.CreateSnapshot())!.ActiveSupplyJobId);
        Assert.Equal(
            1,
            harness.Inventory.GetStack(sourceStackId)!.Reservations
                .Single(value => value.JobId == supplyJobId).Quantity);
    }

    private static EntityId Id(int value) =>
        EntityId.Parse(value.ToString("x32"));
}

}
