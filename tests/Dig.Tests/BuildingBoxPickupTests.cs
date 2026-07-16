using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Saving;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingBoxPickupTests
{
    [Fact]
    public void Pickup_claims_selected_resident_and_reserves_exact_box()
    {
        BuildingBoxPickupHarness harness = new BuildingBoxPickupHarness();

        Result result = harness.Create();

        Assert.True(result.IsSuccess, result.Error?.ToString());
        JobSnapshot job = harness.Jobs.Get(harness.JobId)!;
        Assert.Equal(JobStatus.Claimed, job.Status);
        Assert.Equal(harness.ResidentId, job.AssignedAgentId);
        Assert.Equal(4, harness.Jobs.GetReservations().Count);
        ItemStackSnapshot stack = harness.Inventory.GetStack(harness.StackId)!;
        Assert.Equal(1, stack.ReservedQuantity);
        Assert.Equal(ItemLocation.InWorld(harness.SourceCell), stack.Location);
    }

    [Fact]
    public void Pickup_moves_one_box_to_resident_inventory_and_releases_reservations()
    {
        BuildingBoxPickupHarness harness = new BuildingBoxPickupHarness();
        Assert.True(harness.Create().IsSuccess);
        harness.AdvanceToAcquireItem();

        Result completed = harness.Complete();

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(harness.JobId)!.Status);
        Assert.Empty(harness.Jobs.GetReservations());
        ItemStackSnapshot stack = harness.Inventory.GetStack(harness.StackId)!;
        Assert.Equal(ItemLocation.InAgent(harness.ResidentId), stack.Location);
        Assert.Equal(0, stack.ReservedQuantity);
        Assert.Equal(1, harness.Inventory.GetTotal(harness.BoxItemId));
    }

    [Fact]
    public void Competing_pickup_cannot_claim_the_same_box()
    {
        BuildingBoxPickupHarness harness = new BuildingBoxPickupHarness();
        Assert.True(harness.Create().IsSuccess);

        Result competing = harness.Create(
            jobId: Id('4'),
            residentId: Id('5'));

        Assert.Equal(BuildingBoxPickupErrors.BoxUnavailable, competing.Error);
        Assert.Null(harness.Jobs.Get(Id('4')));
        Assert.Equal(1, harness.Inventory.GetStack(harness.StackId)!.ReservedQuantity);
    }

    [Fact]
    public void Cancel_releases_inventory_and_common_job_reservations()
    {
        BuildingBoxPickupHarness harness = new BuildingBoxPickupHarness();
        Assert.True(harness.Create().IsSuccess);

        Result cancelled = harness.Cancel();

        Assert.True(cancelled.IsSuccess, cancelled.Error?.ToString());
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(harness.JobId)!.Status);
        Assert.Empty(harness.Jobs.GetReservations());
        ItemStackSnapshot stack = harness.Inventory.GetStack(harness.StackId)!;
        Assert.Equal(0, stack.ReservedQuantity);
        Assert.Equal(ItemLocation.InWorld(harness.SourceCell), stack.Location);
    }

    [Fact]
    public void Pickup_job_definition_round_trips_through_stable_save_codec()
    {
        EntityId jobId = Id('3');
        EntityId stackId = Id('1');
        EntityId dependencyId = Id('9');
        BuildingBoxPickupJobDefinition definition = new BuildingBoxPickupJobDefinition(
            jobId,
            stackId,
            new CellId(4, 5),
            priority: 700,
            createdTick: 12,
            retryPolicy: new JobRetryPolicy(maximumRetries: 2, retryDelayTicks: 9),
            dependencies: new[] { dependencyId });
        BuildingBoxPickupJobSaveCodec codec = new BuildingBoxPickupJobSaveCodec();

        JobDefinitionSaveData encoded = codec.Encode(definition);
        encoded.TypeId = codec.TypeId;
        BuildingBoxPickupJobDefinition decoded =
            Assert.IsType<BuildingBoxPickupJobDefinition>(codec.Decode(encoded));

        Assert.Equal("job.building_box_pickup.v1", codec.TypeId);
        Assert.Equal(jobId, decoded.Id);
        Assert.Equal(stackId, decoded.StackId);
        Assert.Equal(new CellId(4, 5), decoded.SourceCell);
        Assert.Equal(700, decoded.Priority);
        Assert.Equal(12, decoded.CreatedTick);
        Assert.Equal(2, decoded.RetryPolicy.MaximumRetries);
        Assert.Equal(9, decoded.RetryPolicy.RetryDelayTicks);
        Assert.Single(decoded.Dependencies);
        Assert.Equal(dependencyId, decoded.Dependencies[0]);
    }

    internal static EntityId Id(char prefix)
    {
        return EntityId.Parse(prefix + new string('0', 30) + "1");
    }
}

internal sealed class BuildingBoxPickupHarness
{
    private long _tick = 100;

    public BuildingBoxPickupHarness()
    {
        BoxItemId = new ItemId("test.building_box.workshop");
        StackId = BuildingBoxPickupTests.Id('1');
        ResidentId = BuildingBoxPickupTests.Id('2');
        JobId = BuildingBoxPickupTests.Id('3');
        SourceCell = new CellId(4, 5);
        Inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(
                BoxItemId,
                "Workshop BuildingBox",
                maximumStackSize: 1,
                isTool: false),
        }));
        Assert.True(Inventory.AddStack(
            StackId,
            BoxItemId,
            quantity: 1,
            location: ItemLocation.InWorld(SourceCell),
            tick: 0).IsSuccess);
        InventoryRepository = new InMemoryInventoryRepository(Inventory);
        JobRepository = new InMemoryJobRepository();
        Journal = new InMemoryExecutionJournal();
    }

    public ItemId BoxItemId { get; }
    public EntityId StackId { get; }
    public EntityId ResidentId { get; }
    public EntityId JobId { get; }
    public CellId SourceCell { get; }
    public InventoryState Inventory { get; }
    public JobSystem Jobs => JobRepository.Get();
    public InMemoryInventoryRepository InventoryRepository { get; }
    public InMemoryJobRepository JobRepository { get; }
    public InMemoryExecutionJournal Journal { get; }

    public Result Create(EntityId? jobId = null, EntityId? residentId = null)
    {
        return new CreateBuildingBoxPickupHandler(
            InventoryRepository,
            JobRepository,
            Journal).Handle(new CreateBuildingBoxPickupCommand(
                jobId ?? JobId,
                StackId,
                residentId ?? ResidentId,
                BoxItemId,
                SourceCell,
                priority: 700,
                tick: _tick++));
    }

    public void AdvanceToAcquireItem()
    {
        Advance();
        Assert.Equal(JobStageKind.TravelToTarget, Jobs.Get(JobId)!.Stage);
        Advance();
        Assert.Equal(JobStageKind.AcquireItem, Jobs.Get(JobId)!.Stage);
    }

    public Result Complete()
    {
        return new CompleteBuildingBoxPickupHandler(
            InventoryRepository,
            JobRepository,
            Journal).Handle(new CompleteBuildingBoxPickupCommand(JobId, _tick++));
    }

    public Result Cancel()
    {
        return new CancelBuildingBoxPickupHandler(
            InventoryRepository,
            JobRepository,
            Journal).Handle(new CancelBuildingBoxPickupCommand(
                JobId,
                "player_cancelled",
                _tick++));
    }

    private void Advance()
    {
        Result result = new AdvanceJobHandler(JobRepository, Journal).Handle(
            new AdvanceJobCommand(JobId, _tick++));
        Assert.True(result.IsSuccess, result.Error?.ToString());
    }
}

}
