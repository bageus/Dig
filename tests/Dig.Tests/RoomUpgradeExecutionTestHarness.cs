using System;
using Dig.Application.Agents;
using Dig.Application.Rooms;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Rooms;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

internal static class RoomUpgradeExecutionTestFixture
{
    internal static readonly EntityId RoomId = Id(1);
    internal static readonly EntityId FirstWorker = Id(2);
    internal static readonly EntityId SecondWorker = Id(3);
    internal static readonly CellId StockCell = new CellId(5, 5, 0);

    internal static RoomUpgradeExecutionHarness CreateHarness()
    {
        RoomInfrastructureState rooms = new RoomInfrastructureState();
        Assert.True(rooms.RegisterCompletedTemplateRoom(
            RoomId,
            "room.small.1",
            RoomTemplateKind.Small,
            tick: 0).IsSuccess);
        Assert.True(rooms.OrderUpgrade(
            RoomId,
            RoomPurposeKind.Workshop,
            tick: 1).IsSuccess);
        Assert.True(rooms.AssignTemporaryStockCell(RoomId, StockCell, tick: 2).IsSuccess);

        InventoryState inventory = new InventoryState(Items());
        Assert.True(inventory.AddStack(
            Id(10),
            RoomUpgradeMaterialIds.Stone,
            quantity: 4,
            ItemLocation.InWorld(new CellId(1, 1, 0)),
            tick: 1).IsSuccess);
        Assert.True(inventory.AddStack(
            Id(11),
            RoomUpgradeMaterialIds.MushroomLeg,
            quantity: 4,
            ItemLocation.InWorld(new CellId(2, 1, 0)),
            tick: 1).IsSuccess);

        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        InMemoryRoomInfrastructureRepository roomRepository =
            new InMemoryRoomInfrastructureRepository(rooms);
        InMemoryInventoryRepository inventoryRepository =
            new InMemoryInventoryRepository(inventory);
        InMemoryJobRepository jobRepository = new InMemoryJobRepository();
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        Assert.True(agents.Add(AgentTestFactory.CreateAgent(id: FirstWorker)).IsSuccess);
        Assert.True(agents.Add(AgentTestFactory.CreateAgent(id: SecondWorker)).IsSuccess);
        AgentSkillGrantService skills = new AgentSkillGrantService(agents, journal);
        return new RoomUpgradeExecutionHarness(
            roomRepository,
            inventoryRepository,
            jobRepository,
            agents,
            new SynchronizeRoomUpgradeJobsHandler(
                roomRepository,
                inventoryRepository,
                jobRepository,
                new SequentialIds(),
                journal),
            new CompleteRoomUpgradeDeliveryHandler(
                roomRepository,
                inventoryRepository,
                jobRepository,
                journal,
                skills),
            new CommitRoomUpgradeWorkIntervalHandler(
                roomRepository,
                inventoryRepository,
                jobRepository,
                journal,
                skills),
            new CompleteRoomUpgradeWorkHandler(roomRepository, jobRepository, journal),
            new CancelRoomUpgradeOperationHandler(
                roomRepository,
                inventoryRepository,
                jobRepository,
                journal));
    }

    internal static RoomUpgradeJobSynchronizationReport Synchronize(
        RoomUpgradeExecutionHarness harness)
    {
        Result<RoomUpgradeJobSynchronizationReport> result = harness.Sync.Handle(
            new SynchronizeRoomUpgradeJobsCommand(
                new[] { new CellId(1, 1, 0), new CellId(2, 1, 0), StockCell },
                new[] { new CellId(1, 1, 0), new CellId(2, 1, 0), StockCell },
                priority: 500,
                maximumDeliveryJobs: 8,
                tick: 3));
        Assert.True(result.IsSuccess, result.Error?.ToString());
        return result.Value;
    }

    internal static void CompleteDeliveries(
        RoomUpgradeExecutionHarness harness,
        RoomUpgradeJobSynchronizationReport report)
    {
        for (int index = 0; index < report.DeliveriesCreated.Count; index++)
        {
            CompleteDelivery(harness, report.DeliveriesCreated[index], 10 + index);
        }
    }

    internal static void CompleteDelivery(
        RoomUpgradeExecutionHarness harness,
        RoomUpgradeDeliveryJobPlan plan,
        long tick)
    {
        JobSystem jobs = harness.Jobs.Get();
        Assert.True(jobs.Claim(plan.JobId, FirstWorker, tick).IsSuccess);
        Assert.True(jobs.Start(plan.JobId, tick).IsSuccess);
        Assert.True(jobs.AdvanceStage(plan.JobId, tick).IsSuccess);
        Assert.True(jobs.AdvanceStage(plan.JobId, tick).IsSuccess);
        harness.Jobs.Save(jobs);
        Result completed = harness.Deliveries.Handle(
            new CompleteRoomUpgradeDeliveryCommand(
                plan.JobId,
                Id(100 + (int)tick),
                tick));
        Assert.True(completed.IsSuccess, completed.Error?.ToString());
    }

    internal static void BeginWork(
        RoomUpgradeExecutionHarness harness,
        EntityId workJobId,
        EntityId workerId,
        long tick)
    {
        JobSystem jobs = harness.Jobs.Get();
        Assert.True(jobs.Claim(workJobId, workerId, tick).IsSuccess);
        Assert.True(jobs.Start(workJobId, tick).IsSuccess);
        Assert.True(jobs.AdvanceStage(workJobId, tick).IsSuccess);
        harness.Jobs.Save(jobs);
    }

    internal static int Reserved(
        RoomUpgradeExecutionHarness harness,
        EntityId jobId,
        ItemId itemId)
    {
        return harness.Inventory.Get().GetReservedQuantityAt(
            jobId,
            itemId,
            ItemLocation.InWorld(StockCell));
    }

    internal static int Skill(
        RoomUpgradeExecutionHarness harness,
        EntityId worker,
        AgentSkillId skill)
    {
        return harness.Agents.Get(worker)!
            .CreateSkillProgressionSnapshot()
            .GetLevel(skill);
    }

    internal static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }

    private static ItemCatalog Items()
    {
        return new ItemCatalog(new[]
        {
            Item(RoomUpgradeMaterialIds.Stone),
            Item(RoomUpgradeMaterialIds.MushroomLeg),
            Item(RoomUpgradeMaterialIds.Iron),
            Item(RoomUpgradeMaterialIds.Crystal),
        });
    }

    private static ItemDefinition Item(ItemId id)
    {
        return new ItemDefinition(id, id.ToString(), 100, isTool: false);
    }

    private sealed class SequentialIds : IRoomUpgradeJobIdSource
    {
        private int _next = 20;
        public EntityId NextJobId() => Id(_next++);
    }
}

internal sealed class RoomUpgradeExecutionHarness
{
    public RoomUpgradeExecutionHarness(
        InMemoryRoomInfrastructureRepository rooms,
        InMemoryInventoryRepository inventory,
        InMemoryJobRepository jobs,
        InMemoryAgentRepository agents,
        SynchronizeRoomUpgradeJobsHandler sync,
        CompleteRoomUpgradeDeliveryHandler deliveries,
        CommitRoomUpgradeWorkIntervalHandler workIntervals,
        CompleteRoomUpgradeWorkHandler completeWork,
        CancelRoomUpgradeOperationHandler cancel)
    {
        Rooms = rooms;
        Inventory = inventory;
        Jobs = jobs;
        Agents = agents;
        Sync = sync;
        Deliveries = deliveries;
        WorkIntervals = workIntervals;
        CompleteWork = completeWork;
        Cancel = cancel;
    }

    public InMemoryRoomInfrastructureRepository Rooms { get; }
    public InMemoryInventoryRepository Inventory { get; }
    public InMemoryJobRepository Jobs { get; }
    public InMemoryAgentRepository Agents { get; }
    public SynchronizeRoomUpgradeJobsHandler Sync { get; }
    public CompleteRoomUpgradeDeliveryHandler Deliveries { get; }
    public CommitRoomUpgradeWorkIntervalHandler WorkIntervals { get; }
    public CompleteRoomUpgradeWorkHandler CompleteWork { get; }
    public CancelRoomUpgradeOperationHandler Cancel { get; }
}

}
