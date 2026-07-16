using System;
using Dig.Application.Buildings;
using Dig.Application.Jobs;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingBoxPlanTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Confirm_reserves_one_box_and_creates_available_job(bool carriedByResident)
    {
        BuildingBoxHarness harness = new BuildingBoxHarness(carriedByResident);

        Result result = harness.Confirm(harness.BuildingId, harness.JobId, new CellId(3, 3));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        BuildingSnapshot building = harness.Buildings.Get(harness.BuildingId)!;
        Assert.Equal(BuildingStatus.AwaitingBox, building.Status);
        Assert.Equal(BuildingBoxCommitState.Reserved, building.BoxPlan!.CommitState);
        Assert.Equal(harness.SourceStackId, building.BoxPlan.SourceStackId);
        Assert.Equal(harness.JobId, building.BoxPlan.JobId);
        Assert.Equal(1, harness.Inventory.GetStack(harness.SourceStackId)!.ReservedQuantity);
        JobSnapshot job = harness.Jobs.Get(harness.JobId)!;
        Assert.Equal(JobStatus.Available, job.Status);
        Assert.IsType<BuildingBoxAssemblyJobDefinition>(job.Definition);
    }

    [Fact]
    public void Invalid_placement_leaves_box_plan_and_job_absent()
    {
        BuildingBoxHarness harness = new BuildingBoxHarness();

        Result result = harness.Confirm(
            harness.BuildingId,
            harness.JobId,
            new CellId(99, 99));

        Assert.True(result.IsFailure);
        Assert.Equal(BuildingErrors.PlacementOutOfBounds, result.Error);
        Assert.Null(harness.Buildings.Get(harness.BuildingId));
        Assert.Null(harness.Jobs.Get(harness.JobId));
        Assert.Equal(0, harness.Inventory.GetStack(harness.SourceStackId)!.ReservedQuantity);
    }

    [Fact]
    public void Competing_plans_cannot_reserve_the_same_box()
    {
        BuildingBoxHarness harness = new BuildingBoxHarness();
        Assert.True(harness.Confirm(
            harness.BuildingId,
            harness.JobId,
            new CellId(3, 3)).IsSuccess);
        EntityId secondBuilding = BuildingBoxHarness.Id(20);
        EntityId secondJob = BuildingBoxHarness.Id(21);

        Result second = harness.Confirm(
            secondBuilding,
            secondJob,
            new CellId(5, 5));

        Assert.True(second.IsFailure);
        Assert.Equal(InventoryErrors.InsufficientAvailableQuantity, second.Error);
        Assert.Null(harness.Buildings.Get(secondBuilding));
        Assert.Null(harness.Jobs.Get(secondJob));
        Assert.Equal(1, harness.Inventory.GetStack(harness.SourceStackId)!.ReservedQuantity);
    }

    [Fact]
    public void Full_job_lifecycle_consumes_exact_box_and_completes_building()
    {
        BuildingBoxHarness harness = new BuildingBoxHarness();
        Assert.True(harness.Confirm(
            harness.BuildingId,
            harness.JobId,
            new CellId(3, 3)).IsSuccess);
        harness.AssignAndAdvanceToDeposit();
        Assert.True(harness.CommitToSite().IsSuccess);
        harness.AdvanceToPerformWork();
        Assert.True(harness.AddWork(amount: 3).IsSuccess);
        harness.AdvanceToFinalize();

        Result completed = harness.Complete();

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        BuildingSnapshot building = harness.Buildings.Get(harness.BuildingId)!;
        Assert.Equal(BuildingStatus.Completed, building.Status);
        Assert.Equal(BuildingBoxCommitState.Consumed, building.BoxPlan!.CommitState);
        Assert.Null(harness.Inventory.GetStack(harness.SourceStackId));
        Assert.Equal(0, harness.Inventory.GetTotal(harness.BoxItemId));
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(harness.JobId)!.Status);
        Assert.Empty(harness.Jobs.GetReservations());
    }

    [Fact]
    public void Cancel_before_delivery_releases_box_and_job()
    {
        BuildingBoxHarness harness = new BuildingBoxHarness();
        Assert.True(harness.Confirm(
            harness.BuildingId,
            harness.JobId,
            new CellId(3, 3)).IsSuccess);

        Result cancelled = harness.Cancel("player_cancelled");

        Assert.True(cancelled.IsSuccess, cancelled.Error?.ToString());
        Assert.Equal(BuildingStatus.Cancelled, harness.Buildings.Get(harness.BuildingId)!.Status);
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(harness.JobId)!.Status);
        ItemStackSnapshot box = harness.Inventory.GetStack(harness.SourceStackId)!;
        Assert.Equal(1, box.Quantity);
        Assert.Equal(0, box.ReservedQuantity);
        Assert.Empty(harness.Jobs.GetReservations());
    }

    [Fact]
    public void Cancel_after_site_commit_returns_box_to_world()
    {
        BuildingBoxHarness harness = new BuildingBoxHarness(carriedByResident: true);
        CellId origin = new CellId(3, 3);
        Assert.True(harness.Confirm(
            harness.BuildingId,
            harness.JobId,
            origin).IsSuccess);
        harness.AssignAndAdvanceToDeposit();
        Assert.True(harness.CommitToSite().IsSuccess);

        Result cancelled = harness.Cancel("placement_changed");

        Assert.True(cancelled.IsSuccess, cancelled.Error?.ToString());
        ItemStackSnapshot box = harness.Inventory.GetStack(harness.SourceStackId)!;
        Assert.Equal(ItemLocation.InWorld(origin), box.Location);
        Assert.Equal(1, box.Quantity);
        Assert.Equal(0, box.ReservedQuantity);
        Assert.Equal(BuildingStatus.Cancelled, harness.Buildings.Get(harness.BuildingId)!.Status);
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(harness.JobId)!.Status);
        Assert.Empty(harness.Jobs.GetReservations());
    }
}

internal sealed class BuildingBoxHarness
{
    private static readonly MaterialId Air = new MaterialId("air");
    private readonly InMemoryJobCandidateProvider _candidates =
        new InMemoryJobCandidateProvider();
    private long _tick = 1;

    public BuildingBoxHarness(bool carriedByResident = false)
    {
        BuildingId = Id(10);
        JobId = Id(11);
        SourceStackId = Id(12);
        WorkerId = Id(13);
        SourceCell = new CellId(1, 1);
        BoxItemId = new ItemId("building_box.workshop");
        ItemCatalog items = new ItemCatalog(new[]
        {
            new ItemDefinition(
                BoxItemId,
                "Workshop Box",
                maximumStackSize: 1,
                isTool: false),
        });
        Inventory = new InventoryState(items);
        Inventory.AddStack(
            SourceStackId,
            BoxItemId,
            quantity: 1,
            carriedByResident
                ? ItemLocation.InAgent(WorkerId)
                : ItemLocation.InWorld(SourceCell),
            tick: 0);
        Buildings = new BuildingsState();
        Jobs = new JobSystem();
        InventoryRepository = new InMemoryInventoryRepository(Inventory);
        BuildingsRepository = new InMemoryBuildingsRepository(Buildings);
        JobRepository = new InMemoryJobRepository(Jobs);
        WorldRepository = new InMemoryWorldRepository(CreateWorld());
        Journal = new InMemoryExecutionJournal();
        Catalog = new BuildingCatalog(new[] { CreateDefinition(BoxItemId) });
    }

    public EntityId BuildingId { get; }
    public EntityId JobId { get; }
    public EntityId SourceStackId { get; }
    public EntityId WorkerId { get; }
    public CellId SourceCell { get; }
    public ItemId BoxItemId { get; }
    public InventoryState Inventory { get; }
    public BuildingsState Buildings { get; }
    public JobSystem Jobs { get; }
    public InMemoryInventoryRepository InventoryRepository { get; }
    public InMemoryBuildingsRepository BuildingsRepository { get; }
    public InMemoryJobRepository JobRepository { get; }
    public InMemoryWorldRepository WorldRepository { get; }
    public InMemoryExecutionJournal Journal { get; }
    public BuildingCatalog Catalog { get; }

    public Result Confirm(EntityId buildingId, EntityId jobId, CellId origin)
    {
        return new ConfirmBuildingBoxPlacementHandler(
            Catalog,
            WorldRepository,
            BuildingsRepository,
            InventoryRepository,
            JobRepository,
            new BuildingPlacementValidator(),
            Journal).Handle(new ConfirmBuildingBoxPlacementCommand(
                buildingId,
                jobId,
                SourceStackId,
                new BuildingDefinitionId("workshop.box"),
                origin,
                BuildingOrientation.North,
                reachableCells: new[] { new CellId(origin.X, origin.Y - 1) },
                priority: 600,
                tick: _tick++));
    }

    public void AssignAndAdvanceToDeposit()
    {
        _candidates.SetCandidates(JobId, new[]
        {
            new JobCandidate(WorkerId, 5000, distanceCost: 1, isAvailable: true),
        });
        Assert.Single(new AssignAvailableJobsHandler(
            JobRepository,
            _candidates,
            Journal).Handle(new AssignAvailableJobsCommand(_tick++)).Assignments);
        Advance();
        Assert.Equal(JobStageKind.AcquireItem, Jobs.Get(JobId)!.Stage);
        Result acquired = new AcquireBuildingBoxForAssemblyHandler(
            BuildingsRepository,
            InventoryRepository,
            JobRepository,
            Journal).Handle(new AcquireBuildingBoxForAssemblyCommand(
                BuildingId,
                JobId,
                SourceCell,
                _tick++));
        Assert.True(acquired.IsSuccess, acquired.Error?.ToString());
        ItemStackSnapshot carried = Inventory.GetStack(SourceStackId)!;
        Assert.Equal(ItemLocation.InAgent(WorkerId), carried.Location);
        Assert.Equal(1, carried.ReservedQuantity);
        Advance();
        Advance();
        Assert.Equal(JobStageKind.DepositItem, Jobs.Get(JobId)!.Stage);
    }

    public Result CommitToSite()
    {
        return new CommitBuildingBoxToSiteHandler(
            BuildingsRepository,
            InventoryRepository,
            JobRepository,
            Journal).Handle(new CommitBuildingBoxToSiteCommand(
                BuildingId,
                JobId,
                _tick++));
    }

    public void AdvanceToPerformWork()
    {
        Advance();
        Assert.Equal(JobStageKind.PerformWork, Jobs.Get(JobId)!.Stage);
    }

    public Result AddWork(int amount)
    {
        return new AddBuildingBoxAssemblyWorkHandler(
            BuildingsRepository,
            JobRepository).Handle(new AddBuildingBoxAssemblyWorkCommand(
                BuildingId,
                JobId,
                amount,
                _tick++));
    }

    public void AdvanceToFinalize()
    {
        Advance();
        Assert.Equal(JobStageKind.Finalize, Jobs.Get(JobId)!.Stage);
    }

    public Result Complete()
    {
        return new CompleteBuildingBoxAssemblyHandler(
            BuildingsRepository,
            InventoryRepository,
            JobRepository,
            Journal).Handle(new CompleteBuildingBoxAssemblyCommand(
                BuildingId,
                JobId,
                _tick++));
    }

    public Result Cancel(string reason)
    {
        return new CancelBuildingBoxPlanHandler(
            BuildingsRepository,
            InventoryRepository,
            JobRepository,
            Journal).Handle(new CancelBuildingBoxPlanCommand(
                BuildingId,
                reason,
                _tick++));
    }

    private void Advance()
    {
        Assert.True(new AdvanceJobHandler(
            JobRepository,
            Journal).Handle(new AdvanceJobCommand(JobId, _tick++)).IsSuccess);
    }

    private static BuildingDefinition CreateDefinition(ItemId boxItemId)
    {
        return new BuildingDefinition(
            new BuildingDefinitionId("workshop.box"),
            "Box Workshop",
            new[] { new CellOffset(0, 0) },
            new[] { new CellOffset(0, -1) },
            Array.Empty<BuildingMaterialRequirement>(),
            requiredWork: 3,
            maximumDurability: 100,
            new BuildingBoxPolicy(boxItemId, packingWork: 2));
    }

    private static WorldState CreateWorld()
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
        });
        return WorldState.CreateFilled(
            new WorldSize(8, 8),
            chunkSize: 4,
            materials,
            Air,
            explored: true).Value;
    }

    public static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}
