using System;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Inventory;
using Dig.Application.Tunnels;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class TunnelManualWorkExecutionTests
{
    private static readonly EntityId SegmentId = Id(1);
    private static readonly EntityId ManualJobId = EntityId.Parse(
        "m000000000000000000000000000001");
    private static readonly EntityId AutomaticJobId = EntityId.Parse(
        "a000000000000000000000000000001");
    private static readonly EntityId ResidentId = Id(4);
    private static readonly EntityId ResidentStackId = Id(5);
    private static readonly EntityId AutomaticStackId = Id(6);

    [Fact]
    public void Manual_support_at_cell_five_shifts_target_and_cancels_old_job()
    {
        Harness harness = CreateHarness("material.mushroom_leg");
        AddAutomaticSupportAtCellTen(harness);
        RequireSuccess(harness.Create.Handle(new CreateTunnelManualWorkCommand(
            ManualJobId,
            ResidentId,
            ResidentStackId,
            new CellId(5, 0, 0),
            tick: 2)).ToResult());
        AdvanceToFinalize(harness.Jobs, ManualJobId);

        Result result = harness.Complete.Handle(
            new CompleteTunnelManualWorkCommand(ManualJobId, tick: 5));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        HorizontalTunnelSegmentSnapshot segment = harness.Tunnels.Get()
            .GetSegment(SegmentId)!;
        Assert.Contains(segment.StructuralAnchors, anchor =>
            anchor.Cell == new CellId(5, 0, 0)
            && anchor.Kind == TunnelStructuralAnchorKind.WoodenSupport);
        Assert.Equal(
            new CellId(15, 0, 0),
            segment.NextAutomaticSupportTarget!.Value.TargetCell);
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(AutomaticJobId)!.Status);
        Assert.Equal(0, harness.Inventory.GetReservedQuantity(
            AutomaticStackId,
            AutomaticJobId));
        Assert.Null(harness.Inventory.GetStack(ResidentStackId));
        Assert.Equal(70, Skill(harness, AgentSkillCatalog.Woodworking));
    }

    [Fact]
    public void Direct_interruption_cancels_owner_job_and_keeps_exact_stack()
    {
        Harness harness = CreateHarness("material.mushroom_leg");
        Result<EntityId> created = harness.Create.Handle(
            new CreateTunnelManualWorkCommand(
                ManualJobId,
                ResidentId,
                ResidentStackId,
                new CellId(5, 0, 0),
                tick: 2));
        Assert.True(created.IsSuccess, created.Error?.ToString());
        Assert.Equal(ResidentId, harness.Jobs.Get(ManualJobId)!.AssignedAgentId);
        Assert.Equal(1, harness.Inventory.GetReservedQuantity(
            ResidentStackId,
            ManualJobId));

        Result cancelled = harness.Cancel.Handle(
            new CancelTunnelManualWorkCommand(ManualJobId, tick: 3));

        Assert.True(cancelled.IsSuccess, cancelled.Error?.ToString());
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(ManualJobId)!.Status);
        ItemStackSnapshot stack = harness.Inventory.GetStack(ResidentStackId)!;
        Assert.Equal(1, stack.Quantity);
        Assert.Equal(ItemLocationKind.AgentInventory, stack.Location.Kind);
        Assert.Equal(ResidentId, stack.Location.OwnerId);
        Assert.Equal(0, stack.ReservedQuantity);
        Assert.DoesNotContain(
            harness.Tunnels.Get().GetSegment(SegmentId)!.StructuralAnchors,
            anchor => anchor.Kind == TunnelStructuralAnchorKind.WoodenSupport);
    }

    [Fact]
    public void Manual_stone_floor_is_decorative_and_grants_stonework()
    {
        Harness harness = CreateHarness("material.stone");
        RequireSuccess(harness.Create.Handle(new CreateTunnelManualWorkCommand(
            ManualJobId,
            ResidentId,
            ResidentStackId,
            new CellId(7, 0, 0),
            tick: 2)).ToResult());
        AdvanceToFinalize(harness.Jobs, ManualJobId);

        Result result = harness.Complete.Handle(
            new CompleteTunnelManualWorkCommand(ManualJobId, tick: 5));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        TunnelInfrastructureSnapshot snapshot =
            harness.Tunnels.Get().CaptureSnapshot();
        Assert.Contains(new CellId(7, 0, 0), snapshot.CompletedStoneFloorTrimCells);
        Assert.DoesNotContain(
            snapshot.Segments.Single().StructuralAnchors,
            anchor => anchor.Cell == new CellId(7, 0, 0));
        Assert.Equal(new CellId(10, 0, 0),
            snapshot.Segments.Single().NextAutomaticSupportTarget!.Value.TargetCell);
        Assert.Equal(70, Skill(harness, AgentSkillCatalog.Stonework));
    }

    [Fact]
    public void Invalid_target_rejects_without_job_or_reservation()
    {
        Harness harness = CreateHarness("material.mushroom_leg");

        Result<EntityId> result = harness.Create.Handle(
            new CreateTunnelManualWorkCommand(
                ManualJobId,
                ResidentId,
                ResidentStackId,
                new CellId(5, 1, 0),
                tick: 2));

        Assert.True(result.IsFailure);
        Assert.Equal(TunnelManualPlacementErrors.TargetUnavailable, result.Error);
        Assert.Null(harness.Jobs.Get(ManualJobId));
        Assert.Equal(0, harness.Inventory.GetReservedQuantity(
            ResidentStackId,
            ManualJobId));
    }

    private static Harness CreateHarness(string itemIdValue)
    {
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        InMemoryTunnelInfrastructureRepository tunnels =
            new InMemoryTunnelInfrastructureRepository();
        RequireSuccess(tunnels.Get().RegisterSegment(
            SegmentId,
            TunnelSegmentOriginKind.RoomExit,
            new CellId(0, 0, 0),
            Enumerable.Range(1, 30).Select(x => new CellId(x, 0, 0)),
            tick: 1));

        ItemId leg = new ItemId("material.mushroom_leg");
        ItemId stone = new ItemId("material.stone");
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(leg, "Leg", 100, isTool: false),
            new ItemDefinition(stone, "Stone", 100, isTool: false),
        }));
        ItemId sourceItem = new ItemId(itemIdValue);
        RequireSuccess(inventory.AddStack(
            ResidentStackId,
            sourceItem,
            quantity: 1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                slotIndex: 0),
            tick: 1));
        TestInventoryRepository inventoryRepository =
            new TestInventoryRepository(inventory);
        InMemoryJobRepository jobs = new InMemoryJobRepository();
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        RequireSuccess(agents.Add(AgentTestFactory.CreateAgent(id: ResidentId)));
        AgentSkillGrantService skills = new AgentSkillGrantService(agents, journal);
        return new Harness(
            tunnels,
            inventoryRepository,
            jobs,
            agents,
            new CreateTunnelManualWorkHandler(
                tunnels,
                inventoryRepository,
                jobs,
                journal),
            new CancelTunnelManualWorkHandler(
                inventoryRepository,
                jobs,
                journal),
            new CompleteTunnelManualWorkHandler(
                tunnels,
                inventoryRepository,
                jobs,
                journal,
                skills));
    }

    private static void AddAutomaticSupportAtCellTen(Harness harness)
    {
        ItemId leg = new ItemId("material.mushroom_leg");
        RequireSuccess(harness.Inventory.AddStack(
            AutomaticStackId,
            leg,
            quantity: 1,
            ItemLocation.InWorld(new CellId(2, 0, 0)),
            tick: 1));
        RequireSuccess(harness.Inventory.ReserveQuantity(
            AutomaticStackId,
            AutomaticJobId,
            quantity: 1,
            tick: 1));
        RequireSuccess(harness.Jobs.Add(new TunnelAutomaticWorkJobDefinition(
            AutomaticJobId,
            SegmentId,
            TunnelAutomaticWorkKind.WoodenSupport,
            new CellId(10, 0, 0),
            createdTick: 1,
            JobRetryPolicy.Default,
            AutomaticStackId,
            new CellId(2, 0, 0))));
        RequireSuccess(harness.Jobs.MakeAvailable(AutomaticJobId, tick: 1));
    }

    private static void AdvanceToFinalize(JobSystem jobs, EntityId jobId)
    {
        RequireSuccess(jobs.Start(jobId, tick: 3));
        while (jobs.Get(jobId)!.Stage != JobStageKind.Finalize)
        {
            RequireSuccess(jobs.AdvanceStage(jobId, tick: 4));
        }
    }

    private static int Skill(Harness harness, AgentSkillId skill)
    {
        return harness.Agents.Get(ResidentId)!
            .CreateSkillProgressionSnapshot()
            .GetLevel(skill);
    }

    private static EntityId Id(int value) =>
        EntityId.Parse(value.ToString("x32"));

    private static void RequireSuccess(Result result) =>
        Assert.True(result.IsSuccess, result.Error?.ToString());

    private sealed class TestInventoryRepository : IInventoryRepository
    {
        private InventoryState _inventory;
        public TestInventoryRepository(InventoryState inventory) =>
            _inventory = inventory;
        public InventoryState Get() => _inventory;
        public void Save(InventoryState inventory) => _inventory = inventory;
    }

    private sealed class Harness
    {
        public Harness(
            InMemoryTunnelInfrastructureRepository tunnels,
            TestInventoryRepository inventory,
            InMemoryJobRepository jobs,
            InMemoryAgentRepository agents,
            CreateTunnelManualWorkHandler create,
            CancelTunnelManualWorkHandler cancel,
            CompleteTunnelManualWorkHandler complete)
        {
            Tunnels = tunnels;
            InventoryRepository = inventory;
            JobRepository = jobs;
            Agents = agents;
            Create = create;
            Cancel = cancel;
            Complete = complete;
        }

        public InMemoryTunnelInfrastructureRepository Tunnels { get; }
        public TestInventoryRepository InventoryRepository { get; }
        public InMemoryJobRepository JobRepository { get; }
        public InMemoryAgentRepository Agents { get; }
        public CreateTunnelManualWorkHandler Create { get; }
        public CancelTunnelManualWorkHandler Cancel { get; }
        public CompleteTunnelManualWorkHandler Complete { get; }
        public InventoryState Inventory => InventoryRepository.Get();
        public JobSystem Jobs => JobRepository.Get();
    }
}

internal static class ResultTestExtensions
{
    public static Result ToResult<T>(this Result<T> result)
    {
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error!);
    }
}

}
