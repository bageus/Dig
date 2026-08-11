using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Production;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingSupplyApplicationTests
{
    [Fact]
    public void Resident_collects_mixed_partial_load_and_deposits_separate_stacks()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness();
        CellId capCell = new CellId(1, 1, 0);
        CellId legCell = new CellId(2, 1, 0);
        harness.Inventory.AddStack(
            CampfireProductionTestHarness.Id(200),
            Dig.Domain.Content.CampfireProductionContent.MushroomCapItemId,
            4,
            ItemLocation.InWorld(capCell),
            0);
        harness.Inventory.AddStack(
            CampfireProductionTestHarness.Id(201),
            Dig.Domain.Content.CampfireProductionContent.MushroomLegItemId,
            4,
            ItemLocation.InWorld(legCell),
            0);
        EntityId jobId = CampfireProductionTestHarness.Id(202);
        EntityId capDeposit = CampfireProductionTestHarness.Id(220);
        EntityId legDeposit = CampfireProductionTestHarness.Id(221);
        CreateBuildingSupplyJobHandler create = new CreateBuildingSupplyJobHandler(
            harness.Content,
            harness.SupplyRepository,
            harness.ProductionRepository,
            harness.BuildingsRepository,
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal);

        Assert.True(create.Handle(new CreateBuildingSupplyJobCommand(
            jobId,
            CampfireProductionTestHarness.BuildingId,
            CampfireProductionTestHarness.WorkerId,
            new[] { capCell, legCell },
            new[] { capCell, legCell, harness.Buildings.Get(
                CampfireProductionTestHarness.BuildingId)!.WorkPosition },
            Enumerable.Range(210, 6).Select(CampfireProductionTestHarness.Id).ToArray(),
            new[] { capDeposit, legDeposit },
            priority: 500,
            tick: 1)).IsSuccess);
        Assert.True(new AcquireBuildingSupplyHandler(
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal).Handle(new AcquireBuildingSupplyCommand(jobId, 2)).IsSuccess);
        Assert.Equal(JobStageKind.TravelToDestination, harness.Jobs.Get(jobId)!.Stage);
        Assert.True(harness.Jobs.AdvanceStage(jobId, 3).IsSuccess);
        Assert.True(new DepositBuildingSupplyHandler(
            harness.SupplyRepository,
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal).Handle(new DepositBuildingSupplyCommand(jobId, 4)).IsSuccess);

        Assert.Equal(4, harness.Inventory.GetAvailableQuantityAt(
            Dig.Domain.Content.CampfireProductionContent.MushroomCapItemId,
            ItemLocation.InBuilding(CampfireProductionTestHarness.BuildingId)));
        Assert.Equal(4, harness.Inventory.GetAvailableQuantityAt(
            Dig.Domain.Content.CampfireProductionContent.MushroomLegItemId,
            ItemLocation.InBuilding(CampfireProductionTestHarness.BuildingId)));
        Assert.Null(harness.Inventory.GetStack(
            CampfireProductionTestHarness.Id(201)));
        Assert.Equal(ItemLocation.InBuilding(CampfireProductionTestHarness.BuildingId),
            harness.Inventory.GetStack(capDeposit)!.Location);
        Assert.Equal(ItemLocation.InBuilding(CampfireProductionTestHarness.BuildingId),
            harness.Inventory.GetStack(legDeposit)!.Location);
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(jobId)!.Status);
        Assert.False(harness.Supply.Get(
            CampfireProductionTestHarness.BuildingId,
            harness.Inventory.CreateSnapshot())!.HasActiveSupply);
    }

    [Fact]
    public void Resident_acquires_mixed_supply_at_each_world_source_before_travel()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness();
        CellId capCell = new CellId(1, 1, 0);
        CellId legCell = new CellId(2, 1, 0);
        EntityId capSource = CampfireProductionTestHarness.Id(240);
        EntityId legSource = CampfireProductionTestHarness.Id(241);
        EntityId jobId = CampfireProductionTestHarness.Id(242);
        harness.Inventory.AddStack(
            capSource,
            Dig.Domain.Content.CampfireProductionContent.MushroomCapItemId,
            4,
            ItemLocation.InWorld(capCell),
            0);
        harness.Inventory.AddStack(
            legSource,
            Dig.Domain.Content.CampfireProductionContent.MushroomLegItemId,
            4,
            ItemLocation.InWorld(legCell),
            0);
        CreateBuildingSupplyJobHandler create = new CreateBuildingSupplyJobHandler(
            harness.Content,
            harness.SupplyRepository,
            harness.ProductionRepository,
            harness.BuildingsRepository,
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal);
        Assert.True(create.Handle(new CreateBuildingSupplyJobCommand(
            jobId,
            CampfireProductionTestHarness.BuildingId,
            CampfireProductionTestHarness.WorkerId,
            new[] { capCell, legCell },
            new[] { capCell, legCell, harness.Buildings.Get(
                CampfireProductionTestHarness.BuildingId)!.WorkPosition },
            Enumerable.Range(250, 6).Select(CampfireProductionTestHarness.Id).ToArray(),
            new[]
            {
                CampfireProductionTestHarness.Id(260),
                CampfireProductionTestHarness.Id(261),
            },
            priority: 500,
            tick: 1)).IsSuccess);
        AcquireBuildingSupplySourceHandler acquire = new AcquireBuildingSupplySourceHandler(
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal);
        Assert.Equal(JobStatus.Claimed, harness.Jobs.Get(jobId)!.Status);
        Assert.True(harness.Jobs.Start(jobId, 2).IsSuccess);
        Assert.Equal(JobStageKind.TravelToTarget, harness.Jobs.Get(jobId)!.Stage);
        Assert.True(harness.Jobs.AdvanceStage(jobId, 2).IsSuccess);
        Assert.Equal(JobStageKind.AcquireItem, harness.Jobs.Get(jobId)!.Stage);

        Result firstAcquire = acquire.Handle(new AcquireBuildingSupplySourceCommand(
            jobId,
            capSource,
            2));
        Assert.True(firstAcquire.IsSuccess, firstAcquire.Error?.ToString());
        Assert.Equal(JobStatus.InProgress, harness.Jobs.Get(jobId)!.Status);
        Assert.Equal(JobStageKind.AcquireItem, harness.Jobs.Get(jobId)!.Stage);
        Assert.Null(harness.Inventory.GetStack(capSource));
        Assert.NotNull(harness.Inventory.GetStack(legSource));
        Assert.NotEmpty(harness.Inventory.GetResidentSlotClaims(jobId));

        Result secondAcquire = acquire.Handle(new AcquireBuildingSupplySourceCommand(
            jobId,
            legSource,
            3));
        Assert.True(secondAcquire.IsSuccess, secondAcquire.Error?.ToString());
        Assert.Equal(JobStageKind.TravelToDestination, harness.Jobs.Get(jobId)!.Stage);
        Assert.Empty(harness.Inventory.GetResidentSlotClaims(jobId));
        Assert.Null(harness.Inventory.GetStack(legSource));
    }


    [Fact]
    public void Direct_internal_pickup_creates_replacement_supply_demand()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness();
        EntityId internalStack = CampfireProductionTestHarness.Id(300);
        EntityId pickupJob = CampfireProductionTestHarness.Id(301);
        EntityId carriedStack = CampfireProductionTestHarness.Id(302);
        ItemId cap = Dig.Domain.Content.CampfireProductionContent.MushroomCapItemId;
        Assert.True(harness.Inventory.AddStack(
            internalStack,
            cap,
            4,
            ItemLocation.InBuilding(CampfireProductionTestHarness.BuildingId),
            0).IsSuccess);
        CellId work = harness.Buildings.Get(
            CampfireProductionTestHarness.BuildingId)!.WorkPosition;
        CreateWorldItemPickupHandler createPickup = new CreateWorldItemPickupHandler(
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal);
        Assert.True(createPickup.Handle(new CreateWorldItemPickupCommand(
            pickupJob,
            internalStack,
            CampfireProductionTestHarness.WorkerId,
            work,
            ItemLocation.InBuilding(CampfireProductionTestHarness.BuildingId),
            quantity: 1,
            destinationStackId: carriedStack,
            priority: 675,
            tick: 1)).IsSuccess);
        Assert.True(harness.Jobs.Start(pickupJob, 2).IsSuccess);
        Assert.True(harness.Jobs.AdvanceStage(pickupJob, 3).IsSuccess);
        Assert.True(new CompleteWorldItemPickupHandler(
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal).Handle(new CompleteWorldItemPickupCommand(
                pickupJob,
                4)).IsSuccess);

        BuildingSupplySnapshot supply = harness.Supply.Get(
            CampfireProductionTestHarness.BuildingId,
            harness.Inventory.CreateSnapshot())!;
        Assert.Equal(1, supply.Stocks.Single(value => value.ItemId == cap).Missing);

        CellId sourceCell = new CellId(1, 1, 0);
        harness.Inventory.AddStack(
            CampfireProductionTestHarness.Id(303),
            cap,
            1,
            ItemLocation.InWorld(sourceCell),
            5);
        Result replacement = new CreateBuildingSupplyJobHandler(
            harness.Content,
            harness.SupplyRepository,
            harness.ProductionRepository,
            harness.BuildingsRepository,
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal).Handle(new CreateBuildingSupplyJobCommand(
                CampfireProductionTestHarness.Id(304),
                CampfireProductionTestHarness.BuildingId,
                CampfireProductionTestHarness.WorkerId,
                new[] { sourceCell },
                new[] { sourceCell, work },
                new[] { CampfireProductionTestHarness.Id(305) },
                new[] { CampfireProductionTestHarness.Id(306) },
                priority: 500,
                tick: 6));
        Assert.True(replacement.IsSuccess, replacement.Error?.ToString());
    }

    [Fact]
    public void Cancelling_blocked_supply_releases_external_reservations_for_replanning()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness();
        CellId cell = new CellId(1, 1, 0);
        EntityId source = CampfireProductionTestHarness.Id(270);
        EntityId jobId = CampfireProductionTestHarness.Id(271);
        Assert.True(harness.Inventory.AddStack(
            source,
            Dig.Domain.Content.CampfireProductionContent.MushroomCapItemId,
            4,
            ItemLocation.InWorld(cell),
            0).IsSuccess);
        CreateBuildingSupplyJobHandler create = new CreateBuildingSupplyJobHandler(
            harness.Content,
            harness.SupplyRepository,
            harness.ProductionRepository,
            harness.BuildingsRepository,
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal);
        Assert.True(create.Handle(new CreateBuildingSupplyJobCommand(
            jobId,
            CampfireProductionTestHarness.BuildingId,
            CampfireProductionTestHarness.WorkerId,
            new[] { cell },
            new[]
            {
                cell,
                harness.Buildings.Get(
                    CampfireProductionTestHarness.BuildingId)!.WorkPosition,
            },
            Enumerable.Range(272, 4)
                .Select(CampfireProductionTestHarness.Id)
                .ToArray(),
            new[] { CampfireProductionTestHarness.Id(276) },
            500,
            1)).IsSuccess);
        Assert.True(harness.Jobs.Block(
            jobId,
            new JobBlockReason("route_unavailable", "No connected route."),
            tick: 2).IsSuccess);
        harness.JobsRepository.Save(harness.Jobs);

        Result cancelled = new CancelBuildingSupplyHandler(
            harness.SupplyRepository,
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal).Handle(new CancelBuildingSupplyCommand(
                jobId,
                "blocked_supply_replanned",
                3));

        Assert.True(cancelled.IsSuccess, cancelled.Error?.ToString());
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(jobId)!.Status);
        Assert.Equal(4, harness.Inventory.GetStack(source)!.AvailableQuantity);
        Assert.Empty(harness.Inventory.GetResidentSlotClaims(jobId));
        Assert.False(harness.Supply.Get(
            CampfireProductionTestHarness.BuildingId,
            harness.Inventory.CreateSnapshot())!.HasActiveSupply);
    }

    [Fact]
    public void Cancelling_uncollected_supply_releases_all_reservations_and_incoming()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness();
        CellId cell = new CellId(1, 1, 0);
        EntityId source = CampfireProductionTestHarness.Id(230);
        EntityId jobId = CampfireProductionTestHarness.Id(231);
        harness.Inventory.AddStack(
            source,
            Dig.Domain.Content.CampfireProductionContent.MushroomCapItemId,
            4,
            ItemLocation.InWorld(cell),
            0);
        CreateBuildingSupplyJobHandler create = new CreateBuildingSupplyJobHandler(
            harness.Content,
            harness.SupplyRepository,
            harness.ProductionRepository,
            harness.BuildingsRepository,
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal);
        Assert.True(create.Handle(new CreateBuildingSupplyJobCommand(
            jobId,
            CampfireProductionTestHarness.BuildingId,
            CampfireProductionTestHarness.WorkerId,
            new[] { cell },
            new[] { cell, harness.Buildings.Get(
                CampfireProductionTestHarness.BuildingId)!.WorkPosition },
            Enumerable.Range(232, 4)
                .Select(CampfireProductionTestHarness.Id)
                .ToArray(),
            new[] { CampfireProductionTestHarness.Id(236) },
            500,
            1)).IsSuccess);

        Assert.True(new CancelBuildingSupplyHandler(
            harness.SupplyRepository,
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal).Handle(new CancelBuildingSupplyCommand(
                jobId,
                "player_cancelled",
                2)).IsSuccess);

        Assert.Equal(4, harness.Inventory.GetStack(source)!.AvailableQuantity);
        Assert.Empty(harness.Inventory.GetResidentSlotClaims(jobId));
        Assert.False(harness.Supply.Get(
            CampfireProductionTestHarness.BuildingId,
            harness.Inventory.CreateSnapshot())!.HasActiveSupply);
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(jobId)!.Status);
    }
}

}
