using System.Linq;
using Dig.Application.Inventory;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Storage;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class RobustHaulingTests
{
    private static readonly ItemId Ore = new ItemId("ore.copper");
    private static readonly EntityId HighStorage =
        EntityId.Parse("81000000000000000000000000000001");
    private static readonly EntityId LowStorage =
        EntityId.Parse("81000000000000000000000000000002");
    private static readonly EntityId Worker =
        EntityId.Parse("82000000000000000000000000000001");

    [Fact]
    public void Planner_respects_storage_priority_capacity_and_pass_limit()
    {
        Harness harness = CreateHarness();
        harness.AddStack("83000000000000000000000000000001", quantity: 6, x: 1);
        harness.AddStack("83000000000000000000000000000002", quantity: 4, x: 2);
        harness.AddStorage(HighStorage, priority: 900, capacity: 5);
        harness.AddStorage(LowStorage, priority: 100, capacity: 20);
        EntityId firstJob = EntityId.Parse("84000000000000000000000000000001");
        EntityId secondJob = EntityId.Parse("84000000000000000000000000000002");
        EntityId thirdJob = EntityId.Parse("84000000000000000000000000000003");
        PlanHaulingHandler planner = harness.CreatePlanner(firstJob, secondJob, thirdJob);

        HaulingPlanningReport firstPass = planner.Handle(new PlanHaulingCommand(
            maximumJobs: 1,
            priority: 500,
            tick: 1));
        HaulingPlanningReport secondPass = planner.Handle(new PlanHaulingCommand(
            maximumJobs: 2,
            priority: 500,
            tick: 2));

        PlannedHaulingJob first = Assert.Single(firstPass.Created);
        Assert.Equal(HighStorage, first.StorageId);
        Assert.Equal(5, first.Quantity);
        Assert.True(secondPass.Created.Count <= 2);
        Assert.All(secondPass.Created, value => Assert.Equal(LowStorage, value.StorageId));
        Assert.Equal(
            10,
            harness.Inventory.CreateSnapshot().Stacks.Sum(value => value.ReservedQuantity));
        Assert.Equal(5, harness.Storage.GetReservation(firstJob)!.Value.Quantity);
    }

    [Fact]
    public void Block_and_retry_keep_external_reservations_until_retry_exhaustion()
    {
        Harness harness = CreateHarness();
        EntityId stackId = harness.AddStack(
            "83000000000000000000000000000003",
            quantity: 5,
            x: 3);
        harness.AddStorage(HighStorage, priority: 500, capacity: 10);
        EntityId jobId = EntityId.Parse("84000000000000000000000000000004");
        Assert.True(harness.Create(jobId, stackId, quantity: 5, tick: 1).IsSuccess);
        BlockHaulingJobHandler block = new BlockHaulingJobHandler(
            harness.InventoryRepository,
            harness.StorageRepository,
            harness.JobRepository,
            harness.Journal);
        RetryHaulingJobHandler retry = new RetryHaulingJobHandler(
            harness.JobRepository,
            harness.Journal);

        Assert.True(harness.Jobs.Claim(jobId, Worker, tick: 2).IsSuccess);
        Assert.True(block.Handle(new BlockHaulingJobCommand(
            jobId,
            "path_temporarily_blocked",
            "The route is temporarily unavailable.",
            tick: 3)).IsSuccess);
        Assert.Equal(5, harness.Inventory.GetStack(stackId)!.ReservedQuantity);
        Assert.NotNull(harness.Storage.GetReservation(jobId));

        Assert.True(retry.Handle(new RetryHaulingJobCommand(jobId, tick: 13)).IsSuccess);
        Assert.Equal(JobStatus.Available, harness.Jobs.Get(jobId)!.Status);
        Assert.Equal(5, harness.Inventory.GetStack(stackId)!.ReservedQuantity);

        ExhaustRetries(harness, block, retry, jobId, firstTick: 14);

        Assert.Equal(JobStatus.Failed, harness.Jobs.Get(jobId)!.Status);
        Assert.Equal(0, harness.Inventory.GetStack(stackId)!.ReservedQuantity);
        Assert.Null(harness.Storage.GetReservation(jobId));
    }

    [Fact]
    public void Reconciliation_fails_mismatched_job_and_releases_remaining_reservations()
    {
        Harness harness = CreateHarness();
        EntityId stackId = harness.AddStack(
            "83000000000000000000000000000004",
            quantity: 4,
            x: 4);
        harness.AddStorage(HighStorage, priority: 500, capacity: 10);
        EntityId jobId = EntityId.Parse("84000000000000000000000000000005");
        Assert.True(harness.Create(jobId, stackId, quantity: 4, tick: 1).IsSuccess);
        harness.Inventory.ReleaseReservations(jobId, tick: 2);
        ReconcileHaulingHandler reconcile = new ReconcileHaulingHandler(
            harness.InventoryRepository,
            harness.StorageRepository,
            harness.JobRepository,
            harness.Journal);

        HaulingReconciliationReport report = reconcile.Handle(
            new ReconcileHaulingCommand(tick: 3));

        Assert.Equal(JobStatus.Failed, harness.Jobs.Get(jobId)!.Status);
        Assert.Null(harness.Storage.GetReservation(jobId));
        Assert.Contains(
            report.Entries,
            value => value.JobId == jobId && value.ActionCode == "failed_mismatched");
    }

    private static void ExhaustRetries(
        Harness harness,
        BlockHaulingJobHandler block,
        RetryHaulingJobHandler retry,
        EntityId jobId,
        long firstTick)
    {
        long tick = firstTick;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            Assert.True(harness.Jobs.Claim(jobId, Worker, tick).IsSuccess);
            Result blocked = block.Handle(new BlockHaulingJobCommand(
                jobId,
                "path_temporarily_blocked",
                "The route is temporarily unavailable.",
                tick + 1));
            if (attempt == 2)
            {
                Assert.Equal(JobErrors.RetryLimitReached, blocked.Error);
                break;
            }

            Assert.True(blocked.IsSuccess);
            tick += 11;
            Assert.True(retry.Handle(new RetryHaulingJobCommand(jobId, tick)).IsSuccess);
            tick++;
        }
    }

    private static Harness CreateHarness()
    {
        ItemCatalog catalog = new ItemCatalog(new[]
        {
            new ItemDefinition(
                Ore,
                "Copper ore",
                maximumStackSize: 100,
                isTool: false,
                new[] { new ItemCategoryId("raw") }),
        });
        return new Harness(new InventoryState(catalog));
    }

    private sealed class Harness
    {
        public Harness(InventoryState inventory)
        {
            Inventory = inventory;
            Storage = new StorageState();
            Jobs = new JobSystem();
            InventoryRepository = new InMemoryInventoryRepository(Inventory);
            StorageRepository = new InMemoryStorageRepository(Storage);
            JobRepository = new InMemoryJobRepository(Jobs);
            Journal = new InMemoryExecutionJournal();
        }

        public InventoryState Inventory { get; }

        public StorageState Storage { get; }

        public JobSystem Jobs { get; }

        public InMemoryInventoryRepository InventoryRepository { get; }

        public InMemoryStorageRepository StorageRepository { get; }

        public InMemoryJobRepository JobRepository { get; }

        public InMemoryExecutionJournal Journal { get; }

        public EntityId AddStack(string value, int quantity, int x)
        {
            EntityId id = EntityId.Parse(value);
            Assert.True(Inventory.AddStack(
                id,
                Ore,
                quantity,
                ItemLocation.InWorld(new CellId(x, 1)),
                tick: 0).IsSuccess);
            return id;
        }

        public void AddStorage(EntityId id, int priority, int capacity)
        {
            Assert.True(Storage.AddZone(new StorageZoneDefinition(
                id,
                id.ToString(),
                priority,
                capacity,
                StorageFilter.All())).IsSuccess);
        }

        public PlanHaulingHandler CreatePlanner(params EntityId[] ids)
        {
            return new PlanHaulingHandler(
                InventoryRepository,
                StorageRepository,
                JobRepository,
                new InMemoryHaulingJobIdSource(ids),
                Journal);
        }

        public Result Create(EntityId jobId, EntityId stackId, int quantity, long tick)
        {
            return new CreateHaulingJobHandler(
                InventoryRepository,
                StorageRepository,
                JobRepository,
                Journal).Handle(new CreateHaulingJobCommand(
                    jobId,
                    stackId,
                    quantity,
                    HighStorage,
                    priority: 500,
                    tick));
        }
    }
}
}
