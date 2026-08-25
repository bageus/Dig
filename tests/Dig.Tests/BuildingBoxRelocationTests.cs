using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Saving;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingBoxRelocationTests
{
    private static readonly ItemId BoxItemId = new ItemId("building_box.relocation.test");
    private static readonly EntityId StackId = Id(1);
    private static readonly EntityId JobId = Id(2);
    private static readonly EntityId WorkerId = Id(3);
    private static readonly EntityId HolderId = Id(4);
    private static readonly CellId Source = new CellId(1, 1, 0);
    private static readonly CellId Destination = new CellId(5, 4, 0);

    [Fact]
    public void World_source_creates_available_relocation_and_reserves_exact_box()
    {
        RelocationHarness harness = new RelocationHarness(ItemLocation.InWorld(Source));

        Result created = harness.Create();

        Assert.True(created.IsSuccess, created.Error?.ToString());
        JobSnapshot job = harness.Jobs.Get(JobId)!;
        BuildingBoxPickupJobDefinition relocation =
            Assert.IsType<BuildingBoxPickupJobDefinition>(job.Definition);
        Assert.Equal(JobStatus.Available, job.Status);
        Assert.Null(job.AssignedAgentId);
        Assert.True(relocation.IsRelocation);
        Assert.False(relocation.StartsHeld);
        Assert.Equal(Source, relocation.SourceCell);
        Assert.Equal(Destination, relocation.DestinationCell);
        Assert.Equal(1, harness.Inventory.GetStack(StackId)!.ReservedQuantity);
    }

    [Fact]
    public void Unsupported_air_target_creates_no_job_or_reservation()
    {
        RelocationHarness harness = new RelocationHarness(
            ItemLocation.InWorld(Source),
            supportedTarget: false);

        Result created = harness.Create();

        Assert.Equal(BuildingBoxRelocationErrors.TargetUnavailable, created.Error);
        Assert.Null(harness.Jobs.Get(JobId));
        Assert.Equal(0, harness.Inventory.GetStack(StackId)!.ReservedQuantity);
    }

    [Fact]
    public void Inventory_source_is_claimed_only_by_holder_resident()
    {
        RelocationHarness harness = new RelocationHarness(ItemLocation.InAgent(HolderId));

        Result created = harness.Create();

        Assert.True(created.IsSuccess, created.Error?.ToString());
        JobSnapshot job = harness.Jobs.Get(JobId)!;
        BuildingBoxPickupJobDefinition relocation =
            Assert.IsType<BuildingBoxPickupJobDefinition>(job.Definition);
        Assert.Equal(JobStatus.Claimed, job.Status);
        Assert.Equal(HolderId, job.AssignedAgentId);
        Assert.True(relocation.StartsHeld);
        Assert.Equal(JobStageKind.None, job.Stage);
        Assert.Equal(JobStageKind.TravelToDestination, relocation.Stages[0]);
        Assert.Equal(1, harness.Inventory.GetStack(StackId)!.ReservedQuantity);
    }

    [Fact]
    public void World_source_pickup_and_delivery_preserve_entity_and_quantity()
    {
        RelocationHarness harness = new RelocationHarness(ItemLocation.InWorld(Source));
        Assert.True(harness.Create().IsSuccess);
        Assert.True(harness.Jobs.Claim(JobId, WorkerId, tick: 2).IsSuccess);
        harness.Advance(tick: 3);
        harness.Advance(tick: 4);
        Assert.Equal(JobStageKind.AcquireItem, harness.Jobs.Get(JobId)!.Stage);

        Result acquired = new AcquireBuildingBoxForRelocationHandler(
            harness.InventoryRepository,
            harness.JobRepository,
            harness.Journal).Handle(new AcquireBuildingBoxForRelocationCommand(
                JobId,
                Source,
                tick: 5));
        Assert.True(acquired.IsSuccess, acquired.Error?.ToString());
        Assert.Equal(ItemLocation.InAgent(WorkerId), harness.Inventory.GetStack(StackId)!.Location);
        Assert.Equal(1, harness.Inventory.GetStack(StackId)!.ReservedQuantity);

        harness.Advance(tick: 6);
        harness.Advance(tick: 7);
        Assert.Equal(JobStageKind.DepositItem, harness.Jobs.Get(JobId)!.Stage);
        Result completed = new CompleteBuildingBoxRelocationHandler(
            harness.WorldRepository,
            harness.BuildingsRepository,
            harness.InventoryRepository,
            harness.JobRepository,
            harness.Journal).Handle(new CompleteBuildingBoxRelocationCommand(
                JobId,
                tick: 8));

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(JobId)!.Status);
        ItemStackSnapshot box = harness.Inventory.GetStack(StackId)!;
        Assert.Equal(StackId, box.StackId);
        Assert.Equal(ItemLocation.InWorld(Destination), box.Location);
        Assert.Equal(1, box.Quantity);
        Assert.Equal(0, box.ReservedQuantity);
        Assert.Equal(1, harness.Inventory.GetTotal(BoxItemId));
    }

    [Fact]
    public void Target_that_becomes_invalid_at_arrival_cancels_and_keeps_box_with_worker()
    {
        RelocationHarness harness = new RelocationHarness(ItemLocation.InWorld(Source));
        Assert.True(harness.Create().IsSuccess);
        Assert.True(harness.Jobs.Claim(JobId, WorkerId, tick: 2).IsSuccess);
        harness.Advance(tick: 3);
        harness.Advance(tick: 4);
        Assert.True(new AcquireBuildingBoxForRelocationHandler(
            harness.InventoryRepository,
            harness.JobRepository,
            harness.Journal).Handle(new AcquireBuildingBoxForRelocationCommand(
                JobId,
                Source,
                tick: 5)).IsSuccess);
        harness.Advance(tick: 6);
        harness.Advance(tick: 7);

        WorldState world = harness.WorldRepository.Get();
        CellState hidden = world.GetCell(Destination).Value.State.WithExplored(false);
        Assert.True(world.ApplyTerrainChanges(
            new[] { new TerrainChange(Destination, hidden) },
            tick: 8).IsSuccess);

        Result completed = new CompleteBuildingBoxRelocationHandler(
            harness.WorldRepository,
            harness.BuildingsRepository,
            harness.InventoryRepository,
            harness.JobRepository,
            harness.Journal).Handle(new CompleteBuildingBoxRelocationCommand(
                JobId,
                tick: 9));

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(JobId)!.Status);
        ItemStackSnapshot box = harness.Inventory.GetStack(StackId)!;
        Assert.Equal(ItemLocation.InAgent(WorkerId), box.Location);
        Assert.Equal(0, box.ReservedQuantity);
    }

    [Fact]
    public void Held_relocation_starts_before_travel_and_completes_at_destination()
    {
        RelocationHarness harness = new RelocationHarness(ItemLocation.InAgent(HolderId));
        Assert.True(harness.Create().IsSuccess);

        Result<BuildingBoxRelocationExecutionStepKind> start =
            BuildingBoxRelocationExecutionPolicy.Evaluate(
                harness.Jobs.Get(JobId),
                harness.Inventory.GetStack(StackId),
                Source);
        Assert.True(start.IsSuccess);
        Assert.Equal(BuildingBoxRelocationExecutionStepKind.StartJob, start.Value);

        harness.Advance(tick: 2);
        Result<BuildingBoxRelocationExecutionStepKind> travel =
            BuildingBoxRelocationExecutionPolicy.Evaluate(
                harness.Jobs.Get(JobId),
                harness.Inventory.GetStack(StackId),
                Destination);
        Assert.Equal(BuildingBoxRelocationExecutionStepKind.AdvanceStage, travel.Value);

        harness.Advance(tick: 3);
        Result<BuildingBoxRelocationExecutionStepKind> deposit =
            BuildingBoxRelocationExecutionPolicy.Evaluate(
                harness.Jobs.Get(JobId),
                harness.Inventory.GetStack(StackId),
                Destination);
        Assert.Equal(
            BuildingBoxRelocationExecutionStepKind.CompleteRelocation,
            deposit.Value);
    }

    [Fact]
    public void Relocation_waits_until_carried_box_and_worker_reach_destination()
    {
        RelocationHarness harness = new RelocationHarness(ItemLocation.InWorld(Source));
        Assert.True(harness.Create().IsSuccess);
        Assert.True(harness.Jobs.Claim(JobId, WorkerId, tick: 2).IsSuccess);
        harness.Advance(tick: 3);
        harness.Advance(tick: 4);

        Result<BuildingBoxRelocationExecutionStepKind> acquire =
            BuildingBoxRelocationExecutionPolicy.Evaluate(
                harness.Jobs.Get(JobId),
                harness.Inventory.GetStack(StackId),
                Source);
        Assert.Equal(BuildingBoxRelocationExecutionStepKind.AcquireBox, acquire.Value);

        Result<BuildingBoxRelocationExecutionStepKind> away =
            BuildingBoxRelocationExecutionPolicy.Evaluate(
                harness.Jobs.Get(JobId),
                harness.Inventory.GetStack(StackId),
                Destination);
        Assert.Equal(BuildingBoxRelocationExecutionStepKind.None, away.Value);
    }

    [Fact]
    public void Relocation_definition_round_trips_destination_and_holder_start()
    {
        BuildingBoxPickupJobDefinition definition = new BuildingBoxPickupJobDefinition(
            JobId,
            StackId,
            Destination,
            Destination,
            startsHeld: true,
            priority: 625,
            createdTick: 12,
            retryPolicy: new JobRetryPolicy(2, 7));
        BuildingBoxPickupJobSaveCodec codec = new BuildingBoxPickupJobSaveCodec();

        JobDefinitionSaveData encoded = codec.Encode(definition);
        encoded.TypeId = codec.TypeId;
        BuildingBoxPickupJobDefinition decoded =
            Assert.IsType<BuildingBoxPickupJobDefinition>(codec.Decode(encoded));

        Assert.True(decoded.IsRelocation);
        Assert.True(decoded.StartsHeld);
        Assert.Equal(Destination, decoded.DestinationCell);
        Assert.Equal(
            new[] { JobStageKind.TravelToDestination, JobStageKind.DepositItem },
            decoded.Stages);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }

    private sealed class RelocationHarness
    {
        public RelocationHarness(
            ItemLocation sourceLocation,
            bool supportedTarget = true)
        {
            Inventory = new InventoryState(new ItemCatalog(new[]
            {
                new ItemDefinition(
                    BoxItemId,
                    "Relocation box",
                    maximumStackSize: 1,
                    isTool: false),
            }));
            Assert.True(Inventory.AddStack(
                StackId,
                BoxItemId,
                quantity: 1,
                sourceLocation,
                tick: 0).IsSuccess);
            InventoryRepository = new InMemoryInventoryRepository(Inventory);
            JobRepository = new InMemoryJobRepository();
            WorldState world = supportedTarget
                ? BuildingBoxPlacementTestWorld.SupportedState(new[]
                {
                    Source,
                    Destination,
                })
                : BuildingPlacementTests.CreateEmptyWorld();
            WorldRepository = new InMemoryWorldRepository(world);
            BuildingsRepository = new InMemoryBuildingsRepository();
            Journal = new InMemoryExecutionJournal();
        }

        public InventoryState Inventory { get; }
        public JobSystem Jobs => JobRepository.Get();
        public InMemoryInventoryRepository InventoryRepository { get; }
        public InMemoryJobRepository JobRepository { get; }
        public InMemoryWorldRepository WorldRepository { get; }
        public InMemoryBuildingsRepository BuildingsRepository { get; }
        public InMemoryExecutionJournal Journal { get; }

        public Result Create()
        {
            return new CreateBuildingBoxRelocationHandler(
                WorldRepository,
                BuildingsRepository,
                InventoryRepository,
                JobRepository,
                Journal).Handle(new CreateBuildingBoxRelocationCommand(
                    JobId,
                    StackId,
                    BoxItemId,
                    Destination,
                    new[] { Destination },
                    priority: 625,
                    tick: 1));
        }

        public void Advance(long tick)
        {
            Result result = new AdvanceJobHandler(JobRepository, Journal).Handle(
                new AdvanceJobCommand(JobId, tick));
            Assert.True(result.IsSuccess, result.Error?.ToString());
        }
    }
}

}
