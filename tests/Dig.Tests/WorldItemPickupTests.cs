using System.Linq;
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
    public void Repeated_completion_of_world_item_pickup_is_idempotent()
    {
        Harness harness = new Harness(quantity: 1);
        Assert.True(harness.Create().IsSuccess);
        harness.AdvanceToAcquireItem();

        Assert.True(harness.Complete().IsSuccess);
        int totalBefore = harness.Inventory.GetTotal(harness.ItemId);

        Result repeated = harness.Complete();

        Assert.True(repeated.IsSuccess, repeated.Error?.ToString());
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(harness.JobId)!.Status);
        Assert.Equal(totalBefore, harness.Inventory.GetTotal(harness.ItemId));
    }

    [Fact]
    public void Pickup_splits_full_world_stack_into_one_unit_per_resident_slot()
    {
        Harness harness = new Harness(quantity: 6);
        Assert.True(harness.Create().IsSuccess);
        ItemStackSnapshot reserved = harness.Inventory.GetStack(harness.StackId)!;
        Assert.Equal(6, reserved.ReservedQuantity);
        Assert.Equal(harness.ResidentId, harness.Jobs.Get(harness.JobId)!.AssignedAgentId);
        harness.AdvanceToAcquireItem();

        Result completed = harness.Complete();

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        ItemStackSnapshot[] carried = harness.Inventory.CreateSnapshot().Stacks
            .Where(stack => stack.ItemId == harness.ItemId)
            .Where(stack => stack.Location.Kind == ItemLocationKind.AgentInventory)
            .Where(stack => stack.Location.OwnerId == harness.ResidentId)
            .OrderBy(stack => stack.Location.ResidentSlotIndex)
            .ToArray();
        Assert.Equal(6, carried.Length);
        Assert.All(carried, stack =>
        {
            Assert.Equal(1, stack.Quantity);
            Assert.Equal(0, stack.ReservedQuantity);
        });
        Assert.Contains(carried, stack => stack.StackId == harness.StackId);
        Assert.Equal(6, carried.Select(stack => stack.Location.ResidentSlotIndex).Distinct().Count());
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(harness.JobId)!.Status);
        Assert.Empty(harness.Jobs.GetReservations());
    }

    [Fact]
    public void Hauling_reconcile_preserves_manual_pickup_slot_claim_until_arrival()
    {
        Harness harness = new Harness(quantity: 1);
        Assert.True(harness.Create().IsSuccess);
        Assert.Single(harness.Inventory.GetResidentSlotClaims(harness.JobId));

        int released = harness.ReconcileHaulingClaims();

        Assert.Equal(0, released);
        Assert.Single(harness.Inventory.GetResidentSlotClaims(harness.JobId));
        harness.AdvanceToAcquireItem();
        Result completed = harness.Complete();
        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        ItemStackSnapshot carried = harness.Inventory.GetStack(harness.StackId)!;
        Assert.Equal(ItemLocationKind.AgentInventory, carried.Location.Kind);
        Assert.Equal(harness.ResidentId, carried.Location.OwnerId);
    }

    [Fact]
    public void Pickup_repairs_missing_slot_claim_before_acquisition()
    {
        Harness harness = new Harness(quantity: 1);
        Assert.True(harness.Create().IsSuccess);
        Assert.Equal(1, harness.DropSlotClaims());
        Assert.Empty(harness.Inventory.GetResidentSlotClaims(harness.JobId));
        harness.AdvanceToAcquireItem();

        Result completed = harness.Complete();

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(harness.JobId)!.Status);
        ItemStackSnapshot carried = harness.Inventory.GetStack(harness.StackId)!;
        Assert.Equal(ItemLocationKind.AgentInventory, carried.Location.Kind);
        Assert.Equal(harness.ResidentId, carried.Location.OwnerId);
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
        Harness harness = new Harness(quantity: 6);
        Assert.True(harness.Create().IsSuccess);

        Result cancelled = harness.Cancel();

        Assert.True(cancelled.IsSuccess, cancelled.Error?.ToString());
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(harness.JobId)!.Status);
        Assert.Equal(0, harness.Inventory.GetStack(harness.StackId)!.ReservedQuantity);
        Assert.Empty(harness.Jobs.GetReservations());
        Assert.Empty(harness.Inventory.GetResidentSlotClaims(harness.JobId));
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
        Assert.Equal(ItemLocation.InWorld(new CellId(4, 5)), decoded.SourceLocation);
        Assert.True(decoded.DestinationStackId.IsEmpty);
        Assert.Equal(dependency, Assert.Single(decoded.Dependencies));
    }


    [Fact]
    public void Building_stock_moves_through_resident_to_world_and_definition_round_trips()
    {
        EntityId buildingId = Id('6');
        EntityId destinationId = Id('7');
        Harness harness = new Harness(
            quantity: 4,
            location: ItemLocation.InBuilding(buildingId));
        Result created = harness.CreateInternal(
            buildingId,
            destinationId,
            quantity: 1);
        Assert.True(created.IsSuccess, created.Error?.ToString());
        harness.AdvanceToAcquireItem();

        Result completed = harness.Complete();

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        Assert.Equal(3, harness.Inventory.GetStack(harness.StackId)!.Quantity);
        ItemStackSnapshot carried = harness.Inventory.GetStack(destinationId)!;
        Assert.Equal(1, carried.Quantity);
        Assert.Equal(harness.ResidentId, carried.Location.OwnerId);

        WorldItemPickupJobDefinition definition = Assert.IsType<WorldItemPickupJobDefinition>(
            harness.Jobs.Get(harness.JobId)!.Definition);
        WorldItemPickupJobSaveCodec codec = new WorldItemPickupJobSaveCodec();
        WorldItemPickupJobDefinition decoded = Assert.IsType<WorldItemPickupJobDefinition>(
            codec.Decode(codec.Encode(definition)));
        Assert.Equal(ItemLocation.InBuilding(buildingId), decoded.SourceLocation);
        Assert.Equal(destinationId, decoded.DestinationStackId);

        CellId dropCell = new CellId(8, 5);
        Result dropped = harness.Drop(destinationId, dropCell);

        Assert.True(dropped.IsSuccess, dropped.Error?.ToString());
        Assert.Equal(ItemLocation.InWorld(dropCell),
            harness.Inventory.GetStack(destinationId)!.Location);
        Assert.Equal(4, harness.Inventory.GetTotal(harness.ItemId));
    }

    private static EntityId Id(char prefix)
    {
        return EntityId.Parse(prefix + new string('0', 30) + "1");
    }

    private sealed class Harness
    {
        private long _tick = 100;

        public Harness(int quantity, ItemLocation? location = null)
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
                location ?? ItemLocation.InWorld(SourceCell),
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


        public Result CreateInternal(
            EntityId buildingId,
            EntityId destinationStackId,
            int quantity)
        {
            return new CreateWorldItemPickupHandler(
                InventoryRepository,
                JobRepository,
                Journal).Handle(new CreateWorldItemPickupCommand(
                    JobId,
                    StackId,
                    ResidentId,
                    SourceCell,
                    ItemLocation.InBuilding(buildingId),
                    quantity,
                    destinationStackId,
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

        public Result Drop(EntityId stackId, CellId destination)
        {
            return new DropResidentInventoryStackHandler(
                InventoryRepository,
                Journal).Handle(new DropResidentInventoryStackCommand(
                    ResidentId,
                    stackId,
                    destination,
                    _tick++));
        }

        public int ReconcileHaulingClaims()
        {
            return new HaulingResidentSlotClaimService(
                InventoryRepository,
                Journal).Reconcile(Jobs, _tick++);
        }

        public int DropSlotClaims()
        {
            return Inventory.ReleaseResidentSlotClaims(JobId, _tick++);
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
