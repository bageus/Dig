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

public sealed class WorldItemPickupTests
{
    [Fact]
    public void Pickup_reserves_and_moves_full_world_stack_to_selected_resident()
    {
        Harness harness = new Harness(quantity: 12);
        Assert.True(harness.Create().IsSuccess);
        ItemStackSnapshot reserved = harness.Inventory.GetStack(harness.StackId)!;
        Assert.Equal(12, reserved.ReservedQuantity);
        Assert.Equal(harness.ResidentId, harness.Jobs.Get(harness.JobId)!.AssignedAgentId);
        harness.AdvanceToAcquireItem();

        Result completed = harness.Complete();

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        ItemStackSnapshot carried = harness.Inventory.GetStack(harness.StackId)!;
        Assert.Equal(ItemLocation.InAgent(harness.ResidentId), carried.Location);
        Assert.Equal(12, carried.Quantity);
        Assert.Equal(0, carried.ReservedQuantity);
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(harness.JobId)!.Status);
        Assert.Empty(harness.Jobs.GetReservations());
    }

    [Fact]
    public void Competing_pickup_is_rejected_without_duplicate_job()
    {
        Harness harness = new Harness(quantity: 4);
        Assert.True(harness.Create().IsSuccess);

        Result competing = harness.Create(Id('4'), Id('5'));

        Assert.Equal(WorldItemPickupErrors.StackUnavailable, competing.Error);
        Assert.Null(harness.Jobs.Get(Id('4')));
        Assert.Equal(4, harness.Inventory.GetStack(harness.StackId)!.ReservedQuantity);
    }

    [Fact]
    public void Cancel_releases_inventory_and_common_reservations()
    {
        Harness harness = new Harness(quantity: 7);
        Assert.True(harness.Create().IsSuccess);

        Result cancelled = harness.Cancel();

        Assert.True(cancelled.IsSuccess, cancelled.Error?.ToString());
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(harness.JobId)!.Status);
        Assert.Equal(0, harness.Inventory.GetStack(harness.StackId)!.ReservedQuantity);
        Assert.Empty(harness.Jobs.GetReservations());
    }

    [Fact]
    public void Pickup_definition_round_trips_through_stable_save_codec()
    {
        EntityId dependency = Id('9');
        WorldItemPickupJobDefinition definition = new WorldItemPickupJobDefinition(
            Id('3'),
            Id('1'),
            quantity: 8,
            new CellId(4, 5),
            priority: 675,
            createdTick: 12,
            new JobRetryPolicy(maximumRetries: 2, retryDelayTicks: 9),
            new[] { dependency });
        WorldItemPickupJobSaveCodec codec = new WorldItemPickupJobSaveCodec();

        JobDefinitionSaveData encoded = codec.Encode(definition);
        encoded.TypeId = codec.TypeId;
        WorldItemPickupJobDefinition decoded =
            Assert.IsType<WorldItemPickupJobDefinition>(codec.Decode(encoded));

        Assert.Equal("job.world_item_pickup.v1", codec.TypeId);
        Assert.Equal(definition.Id, decoded.Id);
        Assert.Equal(definition.StackId, decoded.StackId);
        Assert.Equal(8, decoded.Quantity);
        Assert.Equal(new CellId(4, 5), decoded.SourceCell);
        Assert.Equal(dependency, Assert.Single(decoded.Dependencies));
    }

    private static EntityId Id(char prefix)
    {
        return EntityId.Parse(prefix + new string('0', 30) + "1");
    }

    private sealed class Harness
    {
        private long _tick = 100;

        public Harness(int quantity)
        {
            ItemId = new ItemId("test.rock.chunk");
            StackId = Id('1');
            ResidentId = Id('2');
            JobId = Id('3');
            SourceCell = new CellId(4, 5);
            Inventory = new InventoryState(new ItemCatalog(new[]
            {
                new ItemDefinition(ItemId, "Rock", 100, isTool: false),
            }));
            Assert.True(Inventory.AddStack(
                StackId,
                ItemId,
                quantity,
                ItemLocation.InWorld(SourceCell),
                tick: 0).IsSuccess);
            InventoryRepository = new InMemoryInventoryRepository(Inventory);
            JobRepository = new InMemoryJobRepository();
            Journal = new InMemoryExecutionJournal();
        }

        public ItemId ItemId { get; }
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
            return new CreateWorldItemPickupHandler(
                InventoryRepository,
                JobRepository,
                Journal).Handle(new CreateWorldItemPickupCommand(
                    jobId ?? JobId,
                    StackId,
                    residentId ?? ResidentId,
                    SourceCell,
                    priority: 675,
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
            return new CompleteWorldItemPickupHandler(
                InventoryRepository,
                JobRepository,
                Journal).Handle(new CompleteWorldItemPickupCommand(JobId, _tick++));
        }

        public Result Cancel()
        {
            return new CancelWorldItemPickupHandler(
                InventoryRepository,
                JobRepository,
                Journal).Handle(new CancelWorldItemPickupCommand(
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
}
