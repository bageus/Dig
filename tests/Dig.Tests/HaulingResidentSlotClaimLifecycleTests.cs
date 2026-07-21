using System.Collections.Generic;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Storage;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class HaulingResidentSlotClaimLifecycleTests
{
    private static readonly ItemId OreId = new ItemId("ore.iron");
    private static readonly EntityId SourceStackId = Id(1);
    private static readonly EntityId StorageId = Id(2);
    private static readonly EntityId JobId = Id(3);
    private static readonly EntityId FirstResidentId = Id(4);
    private static readonly EntityId SecondResidentId = Id(5);

    [Fact]
    public void Assignment_reserves_exact_capacity_for_selected_worker()
    {
        Harness harness = new Harness();
        Assert.True(harness.CreateHaul().IsSuccess);

        JobAssignment assignment = Assert.Single(
            harness.Assign(FirstResidentId, tick: 2).Assignments);

        Assert.Equal(FirstResidentId, assignment.AgentId);
        ResidentInventorySlotClaimSnapshot claim = Assert.Single(
            harness.Inventory.GetResidentSlotClaims(JobId));
        Assert.Equal(FirstResidentId, claim.ResidentId);
        Assert.Equal(OreId, claim.ItemId);
        Assert.Equal(6, claim.Quantity);
        Assert.Equal(JobStatus.Claimed, harness.Jobs.Get(JobId)!.Status);
    }

    [Fact]
    public void Assignment_without_capacity_leaves_job_available()
    {
        Harness harness = new Harness(fillFirstResident: true);
        Assert.True(harness.CreateHaul().IsSuccess);

        JobAssignmentReport report = harness.Assign(FirstResidentId, tick: 2);

        Assert.Empty(report.Assignments);
        Assert.Equal(
            InventoryErrors.ResidentInventoryCapacityExceeded,
            Assert.Single(report.Failures).Error);
        Assert.Equal(JobStatus.Available, harness.Jobs.Get(JobId)!.Status);
        Assert.Empty(harness.Inventory.GetResidentSlotClaims(JobId));
    }

    [Fact]
    public void Block_releases_claim_and_retry_can_choose_another_worker()
    {
        Harness harness = new Harness();
        Assert.True(harness.CreateHaul().IsSuccess);
        Assert.Single(harness.Assign(FirstResidentId, tick: 2).Assignments);
        BlockHaulingJobHandler block = new BlockHaulingJobHandler(
            harness.InventoryRepository,
            harness.StorageRepository,
            harness.JobRepository,
            harness.Journal);

        Assert.True(block.Handle(new BlockHaulingJobCommand(
            JobId,
            "route_blocked",
            "The route is temporarily blocked.",
            tick: 3)).IsSuccess);
        Assert.Empty(harness.Inventory.GetResidentSlotClaims(JobId));
        Assert.True(new RetryHaulingJobHandler(
            harness.JobRepository,
            harness.Journal).Handle(new RetryHaulingJobCommand(
                JobId,
                tick: 13)).IsSuccess);

        Assert.Single(harness.Assign(SecondResidentId, tick: 14).Assignments);
        Assert.All(
            harness.Inventory.GetResidentSlotClaims(JobId),
            claim => Assert.Equal(SecondResidentId, claim.ResidentId));
    }

    [Fact]
    public void Cancellation_releases_resident_capacity_claim()
    {
        Harness harness = new Harness();
        Assert.True(harness.CreateHaul().IsSuccess);
        Assert.Single(harness.Assign(FirstResidentId, tick: 2).Assignments);

        Result cancelled = new CancelHaulingJobHandler(
            harness.InventoryRepository,
            harness.StorageRepository,
            harness.JobRepository,
            harness.Journal).Handle(new CancelHaulingJobCommand(
                JobId,
                "Cancelled by player.",
                tick: 3));

        Assert.True(cancelled.IsSuccess, cancelled.Error?.ToString());
        Assert.Empty(harness.Inventory.GetResidentSlotClaims(JobId));
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(JobId)!.Status);
    }

    [Fact]
    public void Completion_releases_resident_capacity_claim()
    {
        Harness harness = new Harness();
        Assert.True(harness.CreateHaul().IsSuccess);
        Assert.Single(harness.Assign(FirstResidentId, tick: 2).Assignments);
        Assert.True(harness.Jobs.Start(JobId, tick: 3).IsSuccess);
        Assert.True(harness.Jobs.AdvanceStage(JobId, tick: 4).IsSuccess);
        Assert.True(harness.Jobs.AdvanceStage(JobId, tick: 5).IsSuccess);

        Result completed = new CompleteHaulingJobHandler(
            harness.InventoryRepository,
            harness.StorageRepository,
            harness.JobRepository,
            harness.Journal,
            AgentSkillGrantTestFactory.Create(FirstResidentId, harness.Journal))
            .Handle(new CompleteHaulingJobCommand(
                JobId,
                Id(20),
                tick: 6));

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        Assert.Empty(harness.Inventory.GetResidentSlotClaims(JobId));
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(JobId)!.Status);
    }

    private sealed class Harness
    {
        public Harness(bool fillFirstResident = false)
        {
            List<ItemDefinition> definitions = new List<ItemDefinition>
            {
                new ItemDefinition(
                    OreId,
                    "Iron ore",
                    maximumStackSize: 100,
                    isTool: false,
                    new[] { new ItemCategoryId("raw") }),
            };
            for (int index = 0; index < 6; index++)
            {
                definitions.Add(new ItemDefinition(
                    new ItemId($"junk.{index}"),
                    $"Junk {index}",
                    maximumStackSize: 1,
                    isTool: false));
            }

            Inventory = new InventoryState(new ItemCatalog(definitions));
            Assert.True(Inventory.AddStack(
                SourceStackId,
                OreId,
                quantity: 10,
                ItemLocation.InWorld(new CellId(1, 1)),
                tick: 0).IsSuccess);
            if (fillFirstResident)
            {
                for (int index = 0; index < 6; index++)
                {
                    Assert.True(Inventory.AddStack(
                        Id(30 + index),
                        new ItemId($"junk.{index}"),
                        quantity: 1,
                        ItemLocation.InResidentSlot(
                            FirstResidentId,
                            ResidentInventoryCompartment.Main,
                            index),
                        tick: 0).IsSuccess);
                }
            }

            Storage = new StorageState();
            Assert.True(Storage.AddZone(new StorageZoneDefinition(
                StorageId,
                "Ore storage",
                priority: 500,
                capacity: 100,
                StorageFilter.All())).IsSuccess);
            Jobs = new JobSystem();
            InventoryRepository = new InMemoryInventoryRepository(Inventory);
            StorageRepository = new InMemoryStorageRepository(Storage);
            JobRepository = new InMemoryJobRepository(Jobs);
            Candidates = new InMemoryJobCandidateProvider();
            Journal = new InMemoryExecutionJournal();
            SlotClaims = new HaulingResidentSlotClaimService(
                InventoryRepository,
                Journal);
        }

        public InventoryState Inventory { get; }
        public StorageState Storage { get; }
        public JobSystem Jobs { get; }
        public InMemoryInventoryRepository InventoryRepository { get; }
        public InMemoryStorageRepository StorageRepository { get; }
        public InMemoryJobRepository JobRepository { get; }
        public InMemoryJobCandidateProvider Candidates { get; }
        public InMemoryExecutionJournal Journal { get; }
        public HaulingResidentSlotClaimService SlotClaims { get; }

        public Result CreateHaul()
        {
            return new CreateHaulingJobHandler(
                InventoryRepository,
                StorageRepository,
                JobRepository,
                Journal).Handle(new CreateHaulingJobCommand(
                    JobId,
                    SourceStackId,
                    quantity: 6,
                    StorageId,
                    priority: 500,
                    tick: 1));
        }

        public JobAssignmentReport Assign(EntityId residentId, long tick)
        {
            Candidates.SetCandidates(JobId, new[]
            {
                new JobCandidate(
                    residentId,
                    skillLevel: 5_000,
                    distanceCost: 1,
                    isAvailable: true),
            });
            return new AssignAvailableJobsHandler(
                JobRepository,
                Candidates,
                Journal,
                haulingResidentSlotClaims: SlotClaims).Handle(
                    new AssignAvailableJobsCommand(tick));
        }
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
