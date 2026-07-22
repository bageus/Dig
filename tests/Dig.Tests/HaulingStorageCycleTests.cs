using System;
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

public sealed class HaulingStorageCycleTests
{
    private static readonly ItemId Rock = new ItemId("test.rock");
    private static readonly EntityId StackId = Id("50000000000000000000000000000001");
    private static readonly EntityId JobId = Id("90000000000000000000000000000001");
    private static readonly EntityId AgentId = Id("10000000000000000000000000000001");
    private static readonly EntityId StorageId = Id("60000000000000000000000000000001");

    [Fact]
    public void Planned_world_stack_reaches_storage_and_releases_all_reservations()
    {
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        InMemoryInventoryRepository inventory = CreateInventory();
        InMemoryStorageRepository storage = CreateStorage();
        InMemoryJobRepository jobs = new InMemoryJobRepository();
        PlanHaulingHandler planner = new PlanHaulingHandler(
            inventory,
            storage,
            jobs,
            new SingleHaulingJobIdSource(JobId),
            journal);

        HaulingPlanningReport planned = planner.Handle(
            new PlanHaulingCommand(maximumJobs: 1, priority: 600, tick: 1));

        PlannedHaulingJob created = Assert.Single(planned.Created);
        Assert.Equal(JobId, created.JobId);
        Assert.Equal(12, created.Quantity);
        Assert.Equal(12, inventory.Get().GetStack(StackId)!.ReservedQuantity);
        Assert.NotNull(storage.Get().GetReservation(JobId));

        InMemoryJobCandidateProvider candidates = new InMemoryJobCandidateProvider();
        candidates.SetCandidates(JobId, new[]
        {
            new JobCandidate(
                AgentId,
                skillLevel: 5_000,
                distanceCost: 3,
                isAvailable: true),
        });
        JobAssignmentReport assignment = new AssignAvailableJobsHandler(
            jobs,
            candidates,
            journal).Handle(new AssignAvailableJobsCommand(tick: 2));
        Assert.Single(assignment.Assignments);
        Assert.NotEmpty(jobs.Get().GetReservations());

        AdvanceJobHandler advance = new AdvanceJobHandler(jobs, journal);
        Assert.True(advance.Handle(new AdvanceJobCommand(JobId, tick: 3)).IsSuccess);
        Assert.True(advance.Handle(new AdvanceJobCommand(JobId, tick: 4)).IsSuccess);
        Assert.True(advance.Handle(new AdvanceJobCommand(JobId, tick: 5)).IsSuccess);
        JobSnapshot deposit = jobs.Get().Get(JobId)!;
        Assert.Equal(JobStatus.InProgress, deposit.Status);
        Assert.Equal(JobStageKind.DepositItem, deposit.Stage);

        Result completed = new CompleteHaulingJobHandler(
            inventory,
            storage,
            jobs,
            journal,
            AgentSkillGrantTestFactory.Create(AgentId, journal))
            .Handle(new CompleteHaulingJobCommand(
                JobId,
                Id("a0000000000000000000000000000001"),
                tick: 6));

        Assert.True(completed.IsSuccess);
        JobSnapshot final = jobs.Get().Get(JobId)!;
        Assert.Equal(JobStatus.Completed, final.Status);
        Assert.Empty(jobs.Get().GetReservations());
        Assert.Empty(storage.Get().GetReservations());
        ItemStackSnapshot moved = inventory.Get().GetStack(StackId)!;
        Assert.Equal(ItemLocation.InStorage(StorageId), moved.Location);
        Assert.Equal(12, moved.Quantity);
        Assert.Equal(0, moved.ReservedQuantity);
        Assert.Equal(12, inventory.Get().GetTotalQuantityAt(
            ItemLocation.InStorage(StorageId)));
        Assert.Empty(inventory.Get().GetAvailableWorldStacks());
    }

    private static InMemoryInventoryRepository CreateInventory()
    {
        InventoryState state = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(
                Rock,
                "Rock",
                maximumStackSize: 100,
                isTool: false,
                new[] { new ItemCategoryId("raw.stone") }),
        }));
        Assert.True(state.AddStack(
            StackId,
            Rock,
            quantity: 12,
            ItemLocation.InWorld(new CellId(4, 3)),
            tick: 0).IsSuccess);
        return new InMemoryInventoryRepository(state);
    }

    private static InMemoryStorageRepository CreateStorage()
    {
        StorageState state = new StorageState();
        Assert.True(state.AddZone(new StorageZoneDefinition(
            StorageId,
            "Stone stockpile",
            priority: 900,
            capacity: 100,
            new StorageFilter(
                acceptsAll: false,
                allowedItems: new[] { Rock }))).IsSuccess);
        return new InMemoryStorageRepository(state);
    }

    private static EntityId Id(string value)
    {
        return EntityId.Parse(value);
    }

    private sealed class SingleHaulingJobIdSource : IHaulingJobIdSource
    {
        private readonly EntityId _value;
        private bool _used;

        public SingleHaulingJobIdSource(EntityId value)
        {
            _value = value;
        }

        public EntityId Next()
        {
            if (_used)
            {
                throw new InvalidOperationException("The hauling id was already consumed.");
            }

            _used = true;
            return _value;
        }
    }
}
}
