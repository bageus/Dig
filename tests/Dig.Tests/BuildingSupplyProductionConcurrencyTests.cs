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
    public void Production_and_supply_share_one_building_operation_reservation()
    {
        JobSystem jobs = new JobSystem();
        EntityId buildingId = Id(1);
        EntityId productionJobId = Id(2);
        EntityId supplyJobId = Id(3);
        EntityId productionWorkerId = Id(4);
        EntityId supplyWorkerId = Id(5);
        CellId workPosition = new CellId(4, 3, 0);

        ProductionWorkJobDefinition production = CreateProduction(
            productionJobId,
            buildingId,
            workPosition);
        BuildingSupplyJobDefinition supply = CreateSupply(
            supplyJobId,
            buildingId,
            workPosition);

        Assert.True(jobs.Add(production).IsSuccess);
        Assert.True(jobs.MakeAvailable(productionJobId, 0).IsSuccess);
        Assert.True(jobs.Claim(productionJobId, productionWorkerId, 0).IsSuccess);
        Assert.True(jobs.Add(supply).IsSuccess);
        Assert.True(jobs.MakeAvailable(supplyJobId, 0).IsSuccess);

        Result blocked = jobs.Claim(supplyJobId, supplyWorkerId, 0);

        Assert.True(blocked.IsFailure);
        Assert.Equal(JobErrors.ReservationConflict, blocked.Error);
        Assert.Contains(
            jobs.GetReservations(),
            value => value.JobId == productionJobId
                && value.Key == ReservationKey.ForDestination(buildingId));
        Assert.Contains(
            jobs.GetReservations(),
            value => value.JobId == productionJobId
                && value.Key == ReservationKey.ForPosition(workPosition));
        Assert.DoesNotContain(
            jobs.GetReservations(),
            value => value.JobId == supplyJobId);

        Assert.True(jobs.Cancel(
            productionJobId,
            new JobBlockReason("test.completed", "Production released building."),
            tick: 1).IsSuccess);
        Assert.True(jobs.Claim(supplyJobId, supplyWorkerId, tick: 2).IsSuccess);
        Assert.Contains(
            jobs.GetReservations(),
            value => value.JobId == supplyJobId
                && value.Key == ReservationKey.ForDestination(buildingId));
    }

    [Fact]
    public void Supply_creation_rolls_back_until_production_releases_building()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness();
        EntityId orderId = Id(20);
        EntityId productionJobId = Id(21);
        EntityId blockedSupplyJobId = Id(22);
        EntityId successfulSupplyJobId = Id(23);
        EntityId supplyWorkerId = Id(24);
        EntityId sourceStackId = Id(25);
        CellId sourceCell = new CellId(1, 3, 0);
        CellId workPosition = harness.Buildings.Get(
            CampfireProductionTestHarness.BuildingId)!.WorkPosition;
        harness.AddBuildingStock(
            CampfireProductionContent.MushroomCapItemId,
            quantity: 1,
            id: 26);
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
        Result blocked = handler.Handle(CreateSupplyCommand(
            blockedSupplyJobId,
            supplyWorkerId,
            sourceCell,
            workPosition,
            tick: 4));

        Assert.True(blocked.IsFailure);
        Assert.Equal(JobErrors.ReservationConflict, blocked.Error);
        Assert.Equal(
            JobStatus.Cancelled,
            harness.Jobs.Get(blockedSupplyJobId)!.Status);
        Assert.Empty(harness.Inventory.GetStack(sourceStackId)!.Reservations);
        Assert.False(harness.Supply.Get(
            CampfireProductionTestHarness.BuildingId,
            harness.Inventory.CreateSnapshot())!.HasActiveSupply);

        Assert.True(harness.Jobs.Cancel(
            productionJobId,
            new JobBlockReason("test.completed", "Production released building."),
            tick: 5).IsSuccess);
        Result created = handler.Handle(CreateSupplyCommand(
            successfulSupplyJobId,
            supplyWorkerId,
            sourceCell,
            workPosition,
            tick: 6));

        Assert.True(created.IsSuccess, created.Error?.ToString());
        Assert.Equal(JobStatus.Claimed, harness.Jobs.Get(successfulSupplyJobId)!.Status);
        Assert.Equal(
            successfulSupplyJobId,
            harness.Supply.Get(
                CampfireProductionTestHarness.BuildingId,
                harness.Inventory.CreateSnapshot())!.ActiveSupplyJobId);
        Assert.Equal(
            1,
            harness.Inventory.GetStack(sourceStackId)!.Reservations
                .Single(value => value.JobId == successfulSupplyJobId).Quantity);
    }

    private static ProductionWorkJobDefinition CreateProduction(
        EntityId jobId,
        EntityId buildingId,
        CellId workPosition)
    {
        return new ProductionWorkJobDefinition(
            jobId,
            Id(7),
            buildingId,
            CampfireProductionContent.GrilledMushroomRecipeId,
            workPosition,
            700,
            0,
            JobRetryPolicy.Default);
    }

    private static BuildingSupplyJobDefinition CreateSupply(
        EntityId jobId,
        EntityId buildingId,
        CellId workPosition)
    {
        return new BuildingSupplyJobDefinition(
            jobId,
            buildingId,
            workPosition,
            new[]
            {
                new ItemReservationAllocation(
                    Id(6),
                    CampfireProductionContent.MushroomCapItemId,
                    quantity: 1),
            },
            new[] { Id(8) },
            new[] { Id(9) },
            650,
            0,
            JobRetryPolicy.Default);
    }

    private static CreateBuildingSupplyJobCommand CreateSupplyCommand(
        EntityId jobId,
        EntityId workerId,
        CellId sourceCell,
        CellId workPosition,
        long tick)
    {
        return new CreateBuildingSupplyJobCommand(
            jobId,
            CampfireProductionTestHarness.BuildingId,
            workerId,
            new[] { sourceCell, workPosition },
            new[] { sourceCell, workPosition },
            new[] { Id(30 + (int)tick) },
            new[] { Id(40 + (int)tick) },
            priority: 650,
            tick);
    }

    private static EntityId Id(int value) =>
        EntityId.Parse(value.ToString("x32"));
}

}
