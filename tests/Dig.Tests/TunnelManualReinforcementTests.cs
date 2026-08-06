using System;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Inventory;
using Dig.Application.Saving;
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
public sealed class TunnelManualReinforcementTests
{
    private static readonly EntityId SegmentId = Id(1);
    private static readonly EntityId SecondSegmentId = Id(2);
    private static readonly EntityId ResidentId = Id(3);
    private static readonly EntityId StackId = Id(4);
    private static readonly EntityId JobId = Id(5);

    [Fact]
    public void Planner_distinguishes_support_floor_and_junction_targets()
    {
        TunnelInfrastructureState state = CreateTunnels(withJunction: true);
        TunnelInfrastructureSnapshot snapshot = state.CaptureSnapshot();

        var support = TunnelManualReinforcementPlanner.Resolve(
            snapshot,
            new ItemId("material.mushroom_leg"),
            new CellId(5, 0, 0));
        var floor = TunnelManualReinforcementPlanner.Resolve(
            snapshot,
            new ItemId("material.stone"),
            new CellId(6, 0, 0));
        var junction = TunnelManualReinforcementPlanner.Resolve(
            snapshot,
            new ItemId("material.stone"),
            new CellId(20, 0, 1));

        Assert.Equal(TunnelManualReinforcementKind.WoodenSupport, support.Value.Kind);
        Assert.Equal(TunnelManualReinforcementKind.StoneFloorTrim, floor.Value.Kind);
        Assert.Equal(TunnelManualReinforcementKind.JunctionStoneTrim, junction.Value.Kind);
    }

    [Fact]
    public void Exact_resident_material_is_reserved_consumed_once_and_commits_support()
    {
        Harness harness = CreateHarness(new ItemId("material.mushroom_leg"));
        TunnelManualReinforcementPlan plan = RequirePlan(
            harness.Tunnels.Get().CaptureSnapshot(),
            new ItemId("material.mushroom_leg"),
            new CellId(5, 0, 0));

        Require(harness.Create.Handle(new CreateTunnelManualReinforcementCommand(
            JobId, ResidentId, StackId, plan, tick: 2)));
        Assert.Equal(1, harness.Inventory.GetReservedQuantity(StackId, JobId));
        AdvanceToFinalize(harness.Jobs, JobId);
        Require(harness.Complete.Handle(
            new CompleteTunnelManualReinforcementCommand(JobId, tick: 5)));

        Assert.Equal(0, harness.Inventory.CreateSnapshot().GetTotal(
            new ItemId("material.mushroom_leg")));
        Assert.Contains(harness.Tunnels.Get().GetSegment(SegmentId)!.StructuralAnchors,
            value => value.Cell == new CellId(5, 0, 0)
                && value.Kind == TunnelStructuralAnchorKind.WoodenSupport);
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(JobId)!.Status);
        Assert.True(harness.Complete.Handle(
            new CompleteTunnelManualReinforcementCommand(JobId, tick: 6)).IsFailure);
    }

    [Fact]
    public void Cancel_preserves_exact_material_and_releases_reservation()
    {
        Harness harness = CreateHarness(new ItemId("material.stone"));
        TunnelManualReinforcementPlan plan = RequirePlan(
            harness.Tunnels.Get().CaptureSnapshot(),
            new ItemId("material.stone"),
            new CellId(6, 0, 0));
        Require(harness.Create.Handle(new CreateTunnelManualReinforcementCommand(
            JobId, ResidentId, StackId, plan, tick: 2)));

        Require(harness.Cancel.Handle(new CancelTunnelManualReinforcementCommand(
            JobId, "test_cancel", tick: 3)));

        Assert.Equal(1, harness.Inventory.CreateSnapshot().GetTotal(
            new ItemId("material.stone")));
        Assert.Equal(0, harness.Inventory.GetReservedQuantity(StackId, JobId));
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(JobId)!.Status);
        Assert.DoesNotContain(new CellId(6, 0, 0),
            harness.Tunnels.Get().CaptureSnapshot().CompletedStoneFloorTrimCells);
    }

    [Fact]
    public void Manual_job_save_codec_round_trips_owner_source_kind_and_target()
    {
        TunnelManualReinforcementJobDefinition definition =
            new TunnelManualReinforcementJobDefinition(
                JobId,
                ResidentId,
                StackId,
                SegmentId,
                TunnelManualReinforcementKind.StoneFloorTrim,
                new CellId(7, 0, 0),
                createdTick: 9,
                JobRetryPolicy.Default);
        TunnelManualReinforcementJobSaveCodec codec =
            new TunnelManualReinforcementJobSaveCodec();

        TunnelManualReinforcementJobDefinition restored =
            Assert.IsType<TunnelManualReinforcementJobDefinition>(
                codec.Decode(codec.Encode(definition)));

        Assert.Equal(ResidentId, restored.ResidentId);
        Assert.Equal(StackId, restored.SourceStackId);
        Assert.Equal(SegmentId, restored.SegmentId);
        Assert.Equal(TunnelManualReinforcementKind.StoneFloorTrim, restored.Kind);
        Assert.Equal(new CellId(7, 0, 0), restored.TargetCell);
    }

    private static Harness CreateHarness(ItemId itemId)
    {
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        InMemoryTunnelInfrastructureRepository tunnels =
            new InMemoryTunnelInfrastructureRepository(CreateTunnels(withJunction: false));
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(itemId, itemId.ToString(), 10, isTool: false),
        }));
        Require(inventory.AddStack(
            StackId, itemId, 1, ItemLocation.InAgent(ResidentId), tick: 1));
        TestInventoryRepository inventoryRepository = new TestInventoryRepository(inventory);
        InMemoryJobRepository jobs = new InMemoryJobRepository();
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        Require(agents.Add(AgentTestFactory.CreateAgent(id: ResidentId)));
        AgentSkillGrantService skills = new AgentSkillGrantService(agents, journal);
        return new Harness(
            tunnels,
            inventoryRepository,
            jobs,
            new CreateTunnelManualReinforcementHandler(
                tunnels, inventoryRepository, jobs, journal),
            new CompleteTunnelManualReinforcementHandler(
                tunnels, inventoryRepository, jobs, journal, skills),
            new CancelTunnelManualReinforcementHandler(
                inventoryRepository, jobs, journal));
    }

    private static TunnelInfrastructureState CreateTunnels(bool withJunction)
    {
        TunnelInfrastructureState state = new TunnelInfrastructureState();
        Require(state.RegisterSegment(
            SegmentId,
            TunnelSegmentOriginKind.RoomExit,
            new CellId(0, 0, 0),
            Enumerable.Range(1, 20).Select(x => new CellId(x, 0, 0)),
            tick: 1));
        if (withJunction)
        {
            CellId junction = new CellId(20, 0, 1);
            Require(state.RegisterSegment(
                SecondSegmentId,
                TunnelSegmentOriginKind.VerticalJunction,
                junction,
                Enumerable.Range(1, 10).Select(x => new CellId(20 + x, 0, 1)),
                tick: 1));
            Require(state.RegisterSegment(
                Id(9),
                TunnelSegmentOriginKind.VerticalJunction,
                junction,
                Enumerable.Range(1, 10).Select(x => new CellId(20 - x, 0, 1)),
                tick: 1));
        }

        return state;
    }

    private static TunnelManualReinforcementPlan RequirePlan(
        TunnelInfrastructureSnapshot snapshot,
        ItemId itemId,
        CellId target)
    {
        var result = TunnelManualReinforcementPlanner.Resolve(snapshot, itemId, target);
        Assert.True(result.IsSuccess, result.Error?.ToString());
        return result.Value;
    }

    private static void AdvanceToFinalize(JobSystem jobs, EntityId jobId)
    {
        Require(jobs.Start(jobId, tick: 3));
        while (jobs.Get(jobId)!.Stage != JobStageKind.Finalize)
        {
            Require(jobs.AdvanceStage(jobId, tick: 4));
        }
    }

    private static EntityId Id(int value) => EntityId.Parse(value.ToString("x32"));

    private static void Require(Result result)
    {
        Assert.True(result.IsSuccess, result.Error?.ToString());
    }

    private sealed class TestInventoryRepository : IInventoryRepository
    {
        private InventoryState _state;
        internal TestInventoryRepository(InventoryState state) { _state = state; }
        public InventoryState Get() => _state;
        public void Save(InventoryState state) { _state = state; }
    }

    private sealed class Harness
    {
        internal Harness(
            InMemoryTunnelInfrastructureRepository tunnels,
            TestInventoryRepository inventory,
            InMemoryJobRepository jobRepository,
            CreateTunnelManualReinforcementHandler create,
            CompleteTunnelManualReinforcementHandler complete,
            CancelTunnelManualReinforcementHandler cancel)
        {
            Tunnels = tunnels;
            InventoryRepository = inventory;
            JobRepository = jobRepository;
            Create = create;
            Complete = complete;
            Cancel = cancel;
        }

        internal InMemoryTunnelInfrastructureRepository Tunnels { get; }
        internal TestInventoryRepository InventoryRepository { get; }
        internal InventoryState Inventory => InventoryRepository.Get();
        internal InMemoryJobRepository JobRepository { get; }
        internal JobSystem Jobs => JobRepository.Get();
        internal CreateTunnelManualReinforcementHandler Create { get; }
        internal CompleteTunnelManualReinforcementHandler Complete { get; }
        internal CancelTunnelManualReinforcementHandler Cancel { get; }
    }
}
}
