using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentInventoryPlacementFailureTests
{
    private static readonly ItemId ItemId = new ItemId("material.placement_failure_test");
    private static readonly EntityId ResidentId = Id(1);
    private static readonly EntityId FirstStackId = Id(2);
    private static readonly EntityId SecondStackId = Id(3);
    private static readonly EntityId FirstJobId = Id(4);
    private static readonly EntityId SecondJobId = Id(5);
    private static readonly CellId FirstTarget = new CellId(4, 4, 0);
    private static readonly CellId SecondTarget = new CellId(5, 4, 0);

    [Fact]
    public void Cancelled_predecessor_releases_reservations_and_cancels_dependents()
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

        JobSystem jobs = harness.JobRepository.Get();
        Assert.True(jobs.Cancel(
            FirstJobId,
            new JobBlockReason("test.cancelled", "Cancelled by regression test."),
            tick: 12).IsSuccess);
        harness.JobRepository.Save(jobs);

        Result synchronized = harness.Queue.Synchronize(tick: 13);

        Assert.True(synchronized.IsSuccess, synchronized.Error?.ToString());
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(FirstJobId)!.Status);
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(SecondJobId)!.Status);
        Assert.Equal(0, harness.Inventory.GetStack(FirstStackId)!.ReservedQuantity);
        Assert.Equal(0, harness.Inventory.GetStack(SecondStackId)!.ReservedQuantity);
    }

    [Fact]
    public void Invalid_target_does_not_create_job_or_reserve_stack()
    {
        Harness harness = new Harness();
        CellId unsupported = new CellId(6, 4, 0);

        Result result = harness.Create(
            FirstJobId,
            FirstStackId,
            unsupported,
            tick: 10,
            reachableCells: new[] { unsupported });

        Assert.True(result.IsFailure);
        Assert.Equal(
            ResidentInventoryPlacementErrors.TargetUnavailable,
            result.Error);
        Assert.Null(harness.Jobs.Get(FirstJobId));
        Assert.Equal(0, harness.Inventory.GetStack(FirstStackId)!.ReservedQuantity);
        Assert.Equal(
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                0),
            harness.Inventory.GetStack(FirstStackId)!.Location);
    }

    [Fact]
    public void Target_that_becomes_unavailable_at_arrival_cancels_and_keeps_item()
    {
        Harness harness = new Harness();
        Assert.True(harness.Create(
            FirstJobId,
            FirstStackId,
            FirstTarget,
            tick: 10).IsSuccess);
        harness.AdvanceToDeposit(FirstJobId, tick: 11);

        WorldState world = harness.WorldRepository.Get();
        CellState hidden = world.GetCell(FirstTarget).Value.State.WithExplored(false);
        Assert.True(world.ApplyTerrainChanges(
            new[] { new TerrainChange(FirstTarget, hidden) },
            tick: 13).IsSuccess);

        Result completed = harness.Complete(FirstJobId, FirstTarget, tick: 14);

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(FirstJobId)!.Status);
        ItemStackSnapshot stack = harness.Inventory.GetStack(FirstStackId)!;
        Assert.Equal(0, stack.ReservedQuantity);
        Assert.Equal(
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                0),
            stack.Location);
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
                    "Placement failure material",
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
        public ResidentInventoryPlacementQueue Queue { get; }

        public void AdvanceToDeposit(EntityId jobId, long tick)
        {
            AdvanceJobHandler advance = new AdvanceJobHandler(JobRepository, Journal);
            Assert.True(advance.Handle(new AdvanceJobCommand(jobId, tick)).IsSuccess);
            Assert.True(advance.Handle(new AdvanceJobCommand(jobId, tick + 1)).IsSuccess);
        }

        public Result Complete(EntityId jobId, CellId workerCell, long tick)
        {
            return new CompleteResidentInventoryPlacementHandler(
                WorldRepository,
                InventoryRepository,
                JobRepository,
                Journal).Handle(new CompleteResidentInventoryPlacementCommand(
                    jobId,
                    workerCell,
                    tick));
        }

        public Result Create(
            EntityId jobId,
            EntityId stackId,
            CellId destination,
            long tick,
            CellId[]? reachableCells = null)
        {
            return CreateHandler.Handle(new CreateResidentInventoryPlacementCommand(
                jobId,
                ResidentId,
                stackId,
                quantity: 1,
                destination,
                reachableCells ?? new[] { FirstTarget, SecondTarget },
                priority: 700,
                tick));
        }
    }
}

}
