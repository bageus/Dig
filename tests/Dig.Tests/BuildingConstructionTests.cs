using System.Linq;
using System;
using Dig.Application.Buildings;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingConstructionTests
{
    private static readonly ItemId Rock = new ItemId("resource.rock");
    private static readonly EntityId BuildingId =
        EntityId.Parse("72000000000000000000000000000001");
    private static readonly EntityId SourceStackId =
        EntityId.Parse("73000000000000000000000000000001");
    private static readonly EntityId WorkerId =
        EntityId.Parse("74000000000000000000000000000001");

    [Fact]
    public void Placement_delivery_and_construction_complete_exactly_once()
    {
        BuildingConstructionHarness harness = CreateHarness();
        harness.Place();
        harness.Deliver(
            EntityId.Parse("75000000000000000000000000000001"),
            quantity: 4,
            EntityId.Parse("76000000000000000000000000000001"));
        Assert.True(new RefreshBuildingMaterialsHandler(
            harness.BuildingsRepository,
            harness.InventoryRepository,
            harness.Journal).Handle(new RefreshBuildingMaterialsCommand(BuildingId)).IsSuccess);
        EntityId constructionJobId =
            EntityId.Parse("77000000000000000000000000000001");
        CreateConstructionJobHandler createConstruction = new CreateConstructionJobHandler(
            harness.BuildingsRepository,
            harness.JobRepository,
            harness.Journal);
        Assert.True(createConstruction.Handle(new CreateConstructionJobCommand(
            constructionJobId,
            BuildingId,
            new[] { new CellId(3, 2) },
            priority: 700,
            tick: 10)).IsSuccess);
        harness.AssignAndAdvanceToPerformWork(constructionJobId, tick: 11);
        Assert.True(new AddConstructionWorkHandler(
            harness.BuildingsRepository,
            harness.JobRepository,
            harness.Journal).Handle(new AddConstructionWorkCommand(
                constructionJobId,
                BuildingId,
                workAmount: 10,
                tick: 13)).IsSuccess);
        Assert.Equal(JobStageKind.Finalize, harness.Jobs.Get(constructionJobId)!.Stage);
        CompleteConstructionHandler complete = new CompleteConstructionHandler(
            harness.BuildingsRepository,
            harness.InventoryRepository,
            harness.JobRepository,
            harness.Journal);

        Assert.True(complete.Handle(new CompleteConstructionCommand(
            constructionJobId,
            BuildingId,
            tick: 14)).IsSuccess);

        BuildingSnapshot building = harness.Buildings.Get(BuildingId)!;
        Assert.Equal(BuildingStatus.Completed, building.Status);
        Assert.Equal(100, building.Durability);
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(constructionJobId)!.Status);
        Assert.Equal(6, harness.Inventory.GetTotal(Rock));
        Assert.Equal(0, harness.Inventory.GetQuantityAt(
            Rock,
            ItemLocation.InBuilding(BuildingId)));
        Assert.Equal(
            1,
            harness.Journal.Events.OfType<BuildingConstructionProgressed>()
                .Count(value => value.BuildingId == BuildingId
                    && value.CompletedWork == value.RequiredWork));
        Assert.Equal(
            1,
            harness.Journal.Events.OfType<BuildingCompleted>()
                .Count(value => value.BuildingId == BuildingId));

        Assert.True(complete.Handle(new CompleteConstructionCommand(
            constructionJobId,
            BuildingId,
            tick: 15)).IsFailure);
        Assert.Equal(
            1,
            harness.Journal.Events.OfType<BuildingCompleted>()
                .Count(value => value.BuildingId == BuildingId));
    }

    [Fact]
    public void Cancellation_releases_jobs_and_returns_delivered_materials()
    {
        BuildingConstructionHarness harness = CreateHarness();
        harness.Place();
        EntityId completedDelivery =
            EntityId.Parse("75000000000000000000000000000002");
        harness.Deliver(
            completedDelivery,
            quantity: 2,
            EntityId.Parse("76000000000000000000000000000002"));
        EntityId activeDelivery =
            EntityId.Parse("75000000000000000000000000000003");
        Assert.True(new CreateBuildingDeliveryHandler(
            harness.BuildingsRepository,
            harness.InventoryRepository,
            harness.JobRepository,
            harness.Journal).Handle(new CreateBuildingDeliveryCommand(
                activeDelivery,
                BuildingId,
                SourceStackId,
                quantity: 2,
                priority: 500,
                tick: 8)).IsSuccess);
        Assert.Equal(2, harness.Inventory.GetStack(SourceStackId)!.ReservedQuantity);
        CancelBuildingHandler cancel = new CancelBuildingHandler(
            harness.BuildingsRepository,
            harness.InventoryRepository,
            harness.JobRepository,
            harness.Journal);

        Assert.True(cancel.Handle(new CancelBuildingCommand(
            BuildingId,
            "Player cancelled the blueprint.",
            tick: 9)).IsSuccess);

        Assert.Equal(BuildingStatus.Cancelled, harness.Buildings.Get(BuildingId)!.Status);
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(activeDelivery)!.Status);
        Assert.Equal(0, harness.Inventory.GetStack(SourceStackId)!.ReservedQuantity);
        Assert.Equal(0, harness.Inventory.GetQuantityAt(
            Rock,
            ItemLocation.InBuilding(BuildingId)));
        Assert.Equal(10, harness.Inventory.GetTotal(Rock));
        Assert.Equal(2, harness.Inventory.GetQuantityAt(
            Rock,
            ItemLocation.InWorld(new CellId(3, 3))));
    }

    [Fact]
    public void Construction_job_is_blocked_with_reachable_position_reason()
    {
        BuildingConstructionHarness harness = CreateHarness();
        harness.Place();
        harness.Deliver(
            EntityId.Parse("75000000000000000000000000000004"),
            quantity: 4,
            EntityId.Parse("76000000000000000000000000000004"));
        Assert.True(new RefreshBuildingMaterialsHandler(
            harness.BuildingsRepository,
            harness.InventoryRepository,
            harness.Journal).Handle(new RefreshBuildingMaterialsCommand(BuildingId)).IsSuccess);
        CreateConstructionJobHandler create = new CreateConstructionJobHandler(
            harness.BuildingsRepository,
            harness.JobRepository,
            harness.Journal);

        Result blocked = create.Handle(new CreateConstructionJobCommand(
            EntityId.Parse("77000000000000000000000000000004"),
            BuildingId,
            Array.Empty<CellId>(),
            priority: 500,
            tick: 10));

        Assert.Equal(BuildingErrors.NoReachableWorkPosition, blocked.Error);
        Assert.Contains("unreachable", harness.Buildings.Get(BuildingId)!.DiagnosticReason!);
    }

    [Fact]
    public void Completed_building_can_be_damaged_repaired_and_removed()
    {
        BuildingConstructionHarness harness = CreateCompletedHarness();
        DamageBuildingHandler damage = new DamageBuildingHandler(
            harness.BuildingsRepository,
            harness.Journal);
        RepairBuildingHandler repair = new RepairBuildingHandler(harness.BuildingsRepository);
        RemoveBuildingHandler remove = new RemoveBuildingHandler(
            harness.BuildingsRepository,
            harness.Journal);

        Assert.True(damage.Handle(new DamageBuildingCommand(
            BuildingId,
            amount: 30,
            tick: 20)).IsSuccess);
        Assert.Equal(BuildingStatus.Damaged, harness.Buildings.Get(BuildingId)!.Status);
        Assert.True(repair.Handle(new RepairBuildingCommand(BuildingId, amount: 30)).IsSuccess);
        Assert.Equal(BuildingStatus.Completed, harness.Buildings.Get(BuildingId)!.Status);
        Assert.True(remove.Handle(new RemoveBuildingCommand(
            BuildingId,
            "Demolished by player.",
            tick: 21)).IsSuccess);
        Assert.Equal(BuildingStatus.Removed, harness.Buildings.Get(BuildingId)!.Status);
        Assert.Contains(
            harness.Journal.Events,
            value => value is BuildingRemoved removed && removed.BuildingId == BuildingId);
    }

    private static BuildingConstructionHarness CreateCompletedHarness()
    {
        BuildingConstructionHarness harness = CreateHarness();
        harness.Place();
        harness.Deliver(
            EntityId.Parse("75000000000000000000000000000005"),
            quantity: 4,
            EntityId.Parse("76000000000000000000000000000005"));
        Assert.True(new RefreshBuildingMaterialsHandler(
            harness.BuildingsRepository,
            harness.InventoryRepository,
            harness.Journal).Handle(new RefreshBuildingMaterialsCommand(BuildingId)).IsSuccess);
        EntityId constructionJob =
            EntityId.Parse("77000000000000000000000000000005");
        Assert.True(new CreateConstructionJobHandler(
            harness.BuildingsRepository,
            harness.JobRepository,
            harness.Journal).Handle(new CreateConstructionJobCommand(
                constructionJob,
                BuildingId,
                new[] { new CellId(3, 2) },
                priority: 500,
                tick: 10)).IsSuccess);
        harness.AssignAndAdvanceToPerformWork(constructionJob, tick: 11);
        Assert.True(new AddConstructionWorkHandler(
            harness.BuildingsRepository,
            harness.JobRepository,
            harness.Journal).Handle(new AddConstructionWorkCommand(
                constructionJob,
                BuildingId,
                workAmount: 10,
                tick: 13)).IsSuccess);
        Assert.True(new CompleteConstructionHandler(
            harness.BuildingsRepository,
            harness.InventoryRepository,
            harness.JobRepository,
            harness.Journal).Handle(new CompleteConstructionCommand(
                constructionJob,
                BuildingId,
                tick: 14)).IsSuccess);
        return harness;
    }

    private static BuildingConstructionHarness CreateHarness()
    {
        ItemCatalog items = new ItemCatalog(new[]
        {
            new ItemDefinition(Rock, "Rock", 100, isTool: false),
        });
        InventoryState inventory = new InventoryState(items);
        Assert.True(inventory.AddStack(
            SourceStackId,
            Rock,
            quantity: 10,
            ItemLocation.InWorld(new CellId(1, 1)),
            tick: 0).IsSuccess);
        return new BuildingConstructionHarness(
            inventory,
            BuildingId,
            SourceStackId,
            WorkerId);
    }
}
}
