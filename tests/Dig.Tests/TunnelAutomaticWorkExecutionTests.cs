using System;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
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

public sealed class TunnelAutomaticWorkExecutionTests
{
    private static readonly EntityId SegmentId = Id(1);
    private static readonly EntityId SecondSegmentId = Id(2);
    private static readonly EntityId JobId = Id(3);
    private static readonly EntityId SourceStackId = Id(4);
    private static readonly EntityId WorkerId = Id(5);
    private static readonly CellId Origin = new CellId(0, 0, 0);
    private static readonly CellId Junction = new CellId(20, 8, 1);

    [Fact]
    public void Wooden_support_consumes_reserved_leg_completes_anchor_and_grants_once()
    {
        Harness harness = CreateHarness(TunnelAutomaticWorkKind.WoodenSupport);

        Result result = harness.Complete.Handle(
            new CompleteTunnelAutomaticWorkCommand(JobId, tick: 3));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(JobId)!.Status);
        ItemStackSnapshot remaining = Assert.Single(
            harness.Inventory.CreateSnapshot().Stacks);
        Assert.Equal(1, remaining.Quantity);
        Assert.Empty(remaining.Reservations);
        HorizontalTunnelSegmentSnapshot segment = harness.Tunnels.Get()
            .GetSegment(SegmentId)!;
        Assert.Contains(segment.StructuralAnchors, anchor =>
            anchor.Cell == new CellId(10, 0, 0)
            && anchor.Kind == TunnelStructuralAnchorKind.WoodenSupport);
        Assert.Equal(
            new CellId(20, 0, 0),
            segment.NextAutomaticSupportTarget!.Value.TargetCell);
        Assert.Equal(
            CompleteTunnelAutomaticWorkHandler.SkillGrantUnits,
            Skill(harness, AgentSkillCatalog.Woodworking));

        Result replay = harness.Complete.Handle(
            new CompleteTunnelAutomaticWorkCommand(JobId, tick: 4));
        Assert.True(replay.IsFailure);
        Assert.Equal(
            CompleteTunnelAutomaticWorkHandler.SkillGrantUnits,
            Skill(harness, AgentSkillCatalog.Woodworking));
        Assert.Equal(1, harness.Inventory.CreateSnapshot().GetTotal(
            new ItemId("material.mushroom_leg")));
    }

    [Fact]
    public void Automatic_junction_trim_is_rejected_before_material_or_skill_mutation()
    {
        Harness harness = CreateHarness(TunnelAutomaticWorkKind.JunctionStoneTrim);

        Result result = harness.Complete.Handle(
            new CompleteTunnelAutomaticWorkCommand(JobId, tick: 3));

        Assert.True(result.IsFailure);
        Assert.Equal(
            TunnelAutomaticWorkExecutionErrors.ManualPlacementRequired,
            result.Error);
        TunnelInfrastructureSnapshot snapshot = harness.Tunnels.Get().CaptureSnapshot();
        Assert.DoesNotContain(Junction, snapshot.CompletedJunctionStoneTrimCells);
        Assert.Single(snapshot.PendingJunctionStoneTrimTargets);
        Assert.Equal(JobStatus.InProgress, harness.Jobs.Get(JobId)!.Status);
        Assert.Equal(0, Skill(harness, AgentSkillCatalog.Stonework));
        Assert.Equal(2, harness.Inventory.CreateSnapshot().GetTotal(
            new ItemId("material.stone")));
        Assert.Equal(1, harness.Inventory.GetReservedQuantity(SourceStackId, JobId));
    }

    [Fact]
    public void Obsolete_support_target_rejects_before_material_or_skill_mutation()
    {
        Harness harness = CreateHarness(TunnelAutomaticWorkKind.WoodenSupport);
        RequireSuccess(harness.Tunnels.Get().RegisterCompletedDoor(
            SegmentId,
            new CellId(5, 0, 0),
            tick: 3));

        Result result = harness.Complete.Handle(
            new CompleteTunnelAutomaticWorkCommand(JobId, tick: 4));

        Assert.True(result.IsFailure);
        Assert.Equal(
            TunnelAutomaticWorkExecutionErrors.TargetObsolete,
            result.Error);
        Assert.Equal(JobStatus.InProgress, harness.Jobs.Get(JobId)!.Status);
        Assert.Equal(2, harness.Inventory.CreateSnapshot().GetTotal(
            new ItemId("material.mushroom_leg")));
        Assert.Equal(0, Skill(harness, AgentSkillCatalog.Woodworking));
        Assert.Equal(1, harness.Inventory.GetReservedQuantity(SourceStackId, JobId));
    }

    [Fact]
    public void Changed_source_contract_rejects_before_infrastructure_commit()
    {
        Harness harness = CreateHarness(TunnelAutomaticWorkKind.WoodenSupport);
        Assert.Equal(1, harness.Inventory.ReleaseReservations(JobId, tick: 3));

        Result result = harness.Complete.Handle(
            new CompleteTunnelAutomaticWorkCommand(JobId, tick: 4));

        Assert.True(result.IsFailure);
        Assert.Equal(TunnelAutomaticWorkExecutionErrors.SourceInvalid, result.Error);
        Assert.DoesNotContain(
            harness.Tunnels.Get().GetSegment(SegmentId)!.StructuralAnchors,
            anchor => anchor.Kind == TunnelStructuralAnchorKind.WoodenSupport);
        Assert.Equal(2, harness.Inventory.CreateSnapshot().GetTotal(
            new ItemId("material.mushroom_leg")));
        Assert.Equal(0, Skill(harness, AgentSkillCatalog.Woodworking));
    }

    private static Harness CreateHarness(TunnelAutomaticWorkKind kind)
    {
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        InMemoryTunnelInfrastructureRepository tunnels =
            new InMemoryTunnelInfrastructureRepository();
        if (kind == TunnelAutomaticWorkKind.WoodenSupport)
        {
            RequireSuccess(tunnels.Get().RegisterSegment(
                SegmentId,
                TunnelSegmentOriginKind.RoomExit,
                Origin,
                Enumerable.Range(1, 30)
                    .Select(x => new CellId(x, 0, 0)),
                tick: 1));
        }
        else
        {
            RequireSuccess(tunnels.Get().RegisterSegment(
                SegmentId,
                TunnelSegmentOriginKind.VerticalJunction,
                Junction,
                Enumerable.Range(1, 20)
                    .Select(distance => new CellId(Junction.X - distance, Junction.Y, Junction.Z)),
                tick: 1));
            RequireSuccess(tunnels.Get().RegisterSegment(
                SecondSegmentId,
                TunnelSegmentOriginKind.VerticalJunction,
                Junction,
                Enumerable.Range(1, 20)
                    .Select(distance => new CellId(Junction.X + distance, Junction.Y, Junction.Z)),
                tick: 1));
        }

        ItemId itemId = kind == TunnelAutomaticWorkKind.WoodenSupport
            ? new ItemId("material.mushroom_leg")
            : new ItemId("material.stone");
        CellId target = kind == TunnelAutomaticWorkKind.WoodenSupport
            ? new CellId(10, 0, 0)
            : Junction;
        CellId sourceCell = new CellId(2, 1, 0);
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(itemId, itemId.ToString(), 100, isTool: false),
        }));
        RequireSuccess(inventory.AddStack(
            SourceStackId,
            itemId,
            quantity: 2,
            ItemLocation.InWorld(sourceCell),
            tick: 1));
        RequireSuccess(inventory.ReserveQuantity(
            SourceStackId,
            JobId,
            quantity: 1,
            tick: 1));
        TestInventoryRepository inventoryRepository =
            new TestInventoryRepository(inventory);

        InMemoryJobRepository jobRepository = new InMemoryJobRepository();
        JobSystem jobs = jobRepository.Get();
        RequireSuccess(jobs.Add(new TunnelAutomaticWorkJobDefinition(
            JobId,
            SegmentId,
            kind,
            target,
            createdTick: 1,
            JobRetryPolicy.Default,
            SourceStackId,
            sourceCell)));
        RequireSuccess(jobs.MakeAvailable(JobId, tick: 1));
        RequireSuccess(jobs.Claim(JobId, WorkerId, tick: 2));
        RequireSuccess(jobs.Start(JobId, tick: 2));
        while (jobs.Get(JobId)!.Stage != JobStageKind.Finalize)
        {
            RequireSuccess(jobs.AdvanceStage(JobId, tick: 2));
        }

        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        RequireSuccess(agents.Add(AgentTestFactory.CreateAgent(id: WorkerId)));
        AgentSkillGrantService skills = new AgentSkillGrantService(agents, journal);
        return new Harness(
            tunnels,
            inventoryRepository,
            jobRepository,
            agents,
            new CompleteTunnelAutomaticWorkHandler(
                tunnels,
                inventoryRepository,
                jobRepository,
                journal,
                skills));
    }

    private static int Skill(Harness harness, AgentSkillId skillId)
    {
        return harness.Agents.Get(WorkerId)!
            .CreateSkillProgressionSnapshot()
            .GetLevel(skillId);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }

    private static void RequireSuccess(Result result)
    {
        Assert.True(result.IsSuccess, result.Error?.ToString());
    }

    private sealed class TestInventoryRepository : IInventoryRepository
    {
        private InventoryState _inventory;

        public TestInventoryRepository(InventoryState inventory)
        {
            _inventory = inventory;
        }

        public InventoryState Get() => _inventory;

        public void Save(InventoryState inventory)
        {
            _inventory = inventory;
        }
    }

    private sealed class Harness
    {
        public Harness(
            InMemoryTunnelInfrastructureRepository tunnels,
            TestInventoryRepository inventoryRepository,
            InMemoryJobRepository jobRepository,
            InMemoryAgentRepository agents,
            CompleteTunnelAutomaticWorkHandler complete)
        {
            Tunnels = tunnels;
            InventoryRepository = inventoryRepository;
            JobRepository = jobRepository;
            Agents = agents;
            Complete = complete;
        }

        public InMemoryTunnelInfrastructureRepository Tunnels { get; }
        public TestInventoryRepository InventoryRepository { get; }
        public InMemoryJobRepository JobRepository { get; }
        public InMemoryAgentRepository Agents { get; }
        public CompleteTunnelAutomaticWorkHandler Complete { get; }
        public InventoryState Inventory => InventoryRepository.Get();
        public JobSystem Jobs => JobRepository.Get();
    }
}
}
