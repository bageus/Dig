using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Saving;
using Dig.Domain.Core;
using Dig.Domain.Exploration;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentInventoryPlacementTests
{
    private static readonly ItemId ItemId = new ItemId("material.placement_test");
    private static readonly EntityId ResidentId = Id(1);
    private static readonly EntityId FirstStackId = Id(2);
    private static readonly EntityId SecondStackId = Id(3);
    private static readonly EntityId FirstJobId = Id(4);
    private static readonly EntityId SecondJobId = Id(5);
    private static readonly CellId FirstTarget = new CellId(4, 4, 0);
    private static readonly CellId SecondTarget = new CellId(5, 4, 0);

    [Fact]
    public void Create_reserves_exact_stack_and_claims_selected_resident()
    {
        Harness harness = new Harness();

        Result result = harness.Create(
            FirstJobId,
            FirstStackId,
            FirstTarget,
            tick: 10);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        JobSnapshot job = harness.Jobs.Get(FirstJobId)!;
        ResidentInventoryPlacementJobDefinition placement =
            Assert.IsType<ResidentInventoryPlacementJobDefinition>(job.Definition);
        Assert.Equal(JobStatus.Claimed, job.Status);
        Assert.Equal(ResidentId, job.AssignedAgentId);
        Assert.Equal(FirstStackId, placement.StackId);
        Assert.Equal(1, placement.Quantity);
        Assert.Equal(FirstTarget, placement.DestinationCell);
        Assert.Equal(1, harness.Inventory.GetStack(FirstStackId)!.ReservedQuantity);
        Assert.Equal(ItemLocation.InResidentSlot(
            ResidentId,
            ResidentInventoryCompartment.Main,
            0), harness.Inventory.GetStack(FirstStackId)!.Location);
    }

    [Fact]
    public void Explored_not_visible_target_can_create_placement_job()
    {
        Harness harness = new Harness();
        ExplorationState visibility = ExplorationState.Restore(
            new ExplorationSaveSnapshot(
                schemaVersion: 1,
                explored: new[] { FirstTarget },
                markers: System.Array.Empty<LastKnownWorldItemMarker>()));
        Assert.Equal(
            CellVisibility.ExploredNotVisible,
            visibility.GetVisibility(FirstTarget));

        Result result = harness.Create(
            FirstJobId,
            FirstStackId,
            FirstTarget,
            tick: 10);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(JobStatus.Claimed, harness.Jobs.Get(FirstJobId)!.Status);
    }

    [Fact]
    public void Later_job_waits_for_predecessor_then_claims_same_resident()
    {
        Harness harness = new Harness();
        Assert.True(harness.Create(
            FirstJobId,
            FirstStackId,
            FirstTarget,
            tick: 10).IsSuccess);
        Assert.True(harness.Create(
            SecondJobId,
            SecondStackId,
            SecondTarget,
            tick: 11).IsSuccess);

        JobSnapshot waiting = harness.Jobs.Get(SecondJobId)!;
        Assert.Equal(JobStatus.Created, waiting.Status);
        Assert.Equal(new[] { FirstJobId }, waiting.Definition.Dependencies);

        harness.AdvanceToDeposit(FirstJobId, tick: 12);
        Result completed = harness.Complete(FirstJobId, FirstTarget, tick: 14);
        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        Result synchronized = harness.Queue.Synchronize(tick: 15);

        Assert.True(synchronized.IsSuccess, synchronized.Error?.ToString());
        JobSnapshot activated = harness.Jobs.Get(SecondJobId)!;
        Assert.Equal(JobStatus.Claimed, activated.Status);
        Assert.Equal(ResidentId, activated.AssignedAgentId);
    }

    [Fact]
    public void Deposit_moves_reserved_quantity_without_duplication()
    {
        Harness harness = new Harness();
        Assert.True(harness.Create(
            FirstJobId,
            FirstStackId,
            FirstTarget,
            tick: 10).IsSuccess);
        harness.AdvanceToDeposit(FirstJobId, tick: 11);

        Result completed = harness.Complete(
            FirstJobId,
            FirstTarget,
            tick: 13);

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(FirstJobId)!.Status);
        ItemStackSnapshot stack = harness.Inventory.GetStack(FirstStackId)!;
        Assert.Equal(ItemLocation.InWorld(FirstTarget), stack.Location);
        Assert.Equal(1, stack.Quantity);
        Assert.Equal(0, stack.ReservedQuantity);
        Assert.Equal(2, harness.Inventory.GetTotal(ItemId));
    }

    [Fact]
    public void Definition_codec_round_trips_binding_order_and_destination()
    {
        ResidentInventoryPlacementJobDefinition definition =
            new ResidentInventoryPlacementJobDefinition(
                SecondJobId,
                ResidentId,
                SecondStackId,
                quantity: 1,
                SecondTarget,
                priority: 700,
                createdTick: 20,
                new JobRetryPolicy(3, 4),
                new[] { FirstJobId });
        ResidentInventoryPlacementJobSaveCodec codec =
            new ResidentInventoryPlacementJobSaveCodec();

        JobDefinitionSaveData encoded = codec.Encode(definition);
        encoded.TypeId = codec.TypeId;
        ResidentInventoryPlacementJobDefinition decoded =
            Assert.IsType<ResidentInventoryPlacementJobDefinition>(codec.Decode(encoded));

        Assert.Equal(ResidentId, decoded.ResidentId);
        Assert.Equal(SecondStackId, decoded.StackId);
        Assert.Equal(1, decoded.Quantity);
        Assert.Equal(SecondTarget, decoded.DestinationCell);
        Assert.Equal(new[] { FirstJobId }, decoded.Dependencies);
        Assert.Equal(
            new[] { JobStageKind.TravelToDestination, JobStageKind.DepositItem },
            decoded.Stages);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }

    private sealed class Harness
    {
        public Harness()
        {
            Inventory = new InventoryState(new ItemCatalog(new[]
            {
                new ItemDefinition(
                    ItemId,
                    "Placement material",
                    maximumStackSize: 100,
                    isTool: false),
            }));
            Assert.True(Inventory.AddStack(
                FirstStackId,
                ItemId,
                quantity: 1,
                ItemLocation.InResidentSlot(
                    ResidentId,
                    ResidentInventoryCompartment.Main,
                    0),
                tick: 1).IsSuccess);
            Assert.True(Inventory.AddStack(
                SecondStackId,
                ItemId,
                quantity: 1,
                ItemLocation.InResidentSlot(
                    ResidentId,
                    ResidentInventoryCompartment.Main,
                    1),
                tick: 2).IsSuccess);
            InventoryRepository = new InMemoryInventoryRepository(Inventory);
            JobRepository = new InMemoryJobRepository();
            WorldRepository = new InMemoryWorldRepository(
                BuildingBoxPlacementTestWorld.SupportedState(new[]
                {
                    FirstTarget,
                    SecondTarget,
                }));
            Journal = new InMemoryExecutionJournal();
            CreateHandler = new CreateResidentInventoryPlacementHandler(
                WorldRepository,
                InventoryRepository,
                JobRepository,
                Journal);
            CompleteHandler = new CompleteResidentInventoryPlacementHandler(
                WorldRepository,
                InventoryRepository,
                JobRepository,
                Journal);
            Queue = new ResidentInventoryPlacementQueue(
                InventoryRepository,
                JobRepository,
                Journal);
        }

        public InventoryState Inventory { get; }
        public JobSystem Jobs => JobRepository.Get();
        public InMemoryInventoryRepository InventoryRepository { get; }
        public InMemoryJobRepository JobRepository { get; }
        public InMemoryWorldRepository WorldRepository { get; }
        public InMemoryExecutionJournal Journal { get; }
        public CreateResidentInventoryPlacementHandler CreateHandler { get; }
        public CompleteResidentInventoryPlacementHandler CompleteHandler { get; }
        public ResidentInventoryPlacementQueue Queue { get; }

        public Result Create(
            EntityId jobId,
            EntityId stackId,
            CellId destination,
            long tick)
        {
            return CreateHandler.Handle(new CreateResidentInventoryPlacementCommand(
                jobId,
                ResidentId,
                stackId,
                quantity: 1,
                destination,
                new[] { FirstTarget, SecondTarget },
                priority: 700,
                tick));
        }

        public void AdvanceToDeposit(EntityId jobId, long tick)
        {
            AdvanceJobHandler advance = new AdvanceJobHandler(JobRepository, Journal);
            Result started = advance.Handle(new AdvanceJobCommand(jobId, tick));
            Assert.True(started.IsSuccess, started.Error?.ToString());
            Result deposit = advance.Handle(new AdvanceJobCommand(jobId, tick + 1));
            Assert.True(deposit.IsSuccess, deposit.Error?.ToString());
            Assert.Equal(JobStageKind.DepositItem, Jobs.Get(jobId)!.Stage);
        }

        public Result Complete(EntityId jobId, CellId workerCell, long tick)
        {
            return CompleteHandler.Handle(new CompleteResidentInventoryPlacementCommand(
                jobId,
                workerCell,
                tick));
        }
    }
}

}
