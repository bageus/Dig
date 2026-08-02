using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Tunnels;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class TunnelInfrastructureApplicationTests
{
    private static readonly EntityId SegmentId = Id(1);
    private static readonly EntityId FirstJobId = Id(2);
    private static readonly EntityId SecondJobId = Id(3);
    private static readonly EntityId SourceStackId = Id(4);
    private static readonly EntityId FirstAgentId = Id(5);
    private static readonly EntityId SecondAgentId = Id(6);
    private static readonly ItemId MushroomLeg = new ItemId("material.mushroom_leg");

    [Fact]
    public void Cqrs_registers_segment_and_completed_anchor()
    {
        InMemoryTunnelInfrastructureRepository repository = new InMemoryTunnelInfrastructureRepository();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        RegisterTunnelSegmentHandler register = new RegisterTunnelSegmentHandler(
            repository,
            journal);
        RegisterCompletedTunnelAnchorHandler anchor =
            new RegisterCompletedTunnelAnchorHandler(repository, journal);
        GetTunnelInfrastructureHandler query = new GetTunnelInfrastructureHandler(repository);

        RequireSuccess(register.Handle(new RegisterTunnelSegmentCommand(
            SegmentId,
            TunnelSegmentOriginKind.RoomExit,
            new CellId(0, 0, 0),
            Cells(30),
            tick: 1)));
        Assert.Equal(
            new CellId(10, 0, 0),
            RequireTarget(query.Handle(new GetTunnelInfrastructureQuery())).TargetCell);

        RequireSuccess(anchor.Handle(new RegisterCompletedTunnelAnchorCommand(
            SegmentId,
            new CellId(5, 0, 0),
            TunnelStructuralAnchorKind.Door,
            tick: 2)));

        Assert.Equal(
            new CellId(15, 0, 0),
            RequireTarget(query.Handle(new GetTunnelInfrastructureQuery())).TargetCell);
        Assert.Contains(journal.Events, value =>
            value is TunnelStructuralAnchorRegistered registered
            && registered.SegmentId == SegmentId
            && registered.Kind == TunnelStructuralAnchorKind.Door);
    }

    [Fact]
    public void Target_outside_completed_building_range_creates_no_job()
    {
        Harness harness = CreateHarness(withSource: false);

        Result<TunnelAutomaticSupportSyncResult> result = harness.Sync.Handle(
            Command(FirstJobId, completedBuildingCell: new CellId(31, 0, 0)));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(TunnelAutomaticSupportSyncStatus.OutOfRange, result.Value.Status);
        Assert.Empty(harness.Jobs.GetAll());
        Assert.Empty(harness.Jobs.GetReservations());
    }

    [Fact]
    public void Missing_source_keeps_created_job_without_phantom_reservations()
    {
        Harness harness = CreateHarness(withSource: false);

        Result<TunnelAutomaticSupportSyncResult> result = harness.Sync.Handle(
            Command(FirstJobId, completedBuildingCell: new CellId(30, 0, 0)));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(TunnelAutomaticSupportSyncStatus.PendingSource, result.Value.Status);
        JobSnapshot job = harness.Jobs.Get(FirstJobId)!;
        TunnelAutomaticWorkJobDefinition definition =
            Assert.IsType<TunnelAutomaticWorkJobDefinition>(job.Definition);
        Assert.Equal(JobStatus.Created, job.Status);
        Assert.False(definition.IsSourceResolved);
        Assert.Equal(TunnelAutomaticWorkJobDefinition.AutomaticPriority, definition.Priority);
        Assert.Empty(harness.Jobs.GetReservations());
        Assert.Empty(harness.Inventory.CreateSnapshot().Stacks);
    }

    [Fact]
    public void Appearing_source_resolves_same_job_and_reserves_one_unit()
    {
        Harness harness = CreateHarness(withSource: false);
        RequireSuccess(harness.Sync.Handle(
            Command(FirstJobId, completedBuildingCell: new CellId(30, 0, 0))));
        RequireSuccess(harness.Inventory.AddStack(
            SourceStackId,
            MushroomLeg,
            quantity: 2,
            ItemLocation.InWorld(new CellId(2, 0, 0)),
            tick: 2));

        Result<TunnelAutomaticSupportSyncResult> result = harness.Sync.Handle(
            Command(
                FirstJobId,
                completedBuildingCell: new CellId(30, 0, 0),
                sourceCell: new CellId(2, 0, 0),
                tick: 3));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(TunnelAutomaticSupportSyncStatus.Available, result.Value.Status);
        JobSnapshot job = harness.Jobs.Get(FirstJobId)!;
        TunnelAutomaticWorkJobDefinition definition =
            Assert.IsType<TunnelAutomaticWorkJobDefinition>(job.Definition);
        Assert.Equal(JobStatus.Available, job.Status);
        Assert.True(definition.IsSourceResolved);
        Assert.Equal(SourceStackId, definition.SourceStackId);
        ItemQuantityReservationSnapshot reservation = Assert.Single(
            harness.Inventory.CreateSnapshot().Stacks.Single().Reservations);
        Assert.Equal(FirstJobId, reservation.JobId);
        Assert.Equal(1, reservation.Quantity);
        Assert.Empty(harness.Jobs.GetReservations());
    }

    [Fact]
    public void New_anchor_cancels_obsolete_target_and_reuses_released_source()
    {
        Harness harness = CreateHarness(withSource: true);
        RequireSuccess(harness.Sync.Handle(Command(
            FirstJobId,
            completedBuildingCell: new CellId(30, 0, 0),
            sourceCell: new CellId(2, 0, 0))));
        RequireSuccess(new RegisterCompletedTunnelAnchorHandler(
            harness.Tunnels,
            harness.Journal).Handle(new RegisterCompletedTunnelAnchorCommand(
                SegmentId,
                new CellId(5, 0, 0),
                TunnelStructuralAnchorKind.WoodenSupport,
                tick: 3)));

        Result<TunnelAutomaticSupportSyncResult> result = harness.Sync.Handle(Command(
            SecondJobId,
            completedBuildingCell: new CellId(30, 0, 0),
            sourceCell: new CellId(2, 0, 0),
            tick: 4));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(FirstJobId)!.Status);
        JobSnapshot replacement = harness.Jobs.Get(SecondJobId)!;
        TunnelAutomaticWorkJobDefinition definition =
            Assert.IsType<TunnelAutomaticWorkJobDefinition>(replacement.Definition);
        Assert.Equal(JobStatus.Available, replacement.Status);
        Assert.Equal(new CellId(15, 0, 0), definition.TargetCell);
        ItemQuantityReservationSnapshot reservation = Assert.Single(
            harness.Inventory.CreateSnapshot().Stacks.Single().Reservations);
        Assert.Equal(SecondJobId, reservation.JobId);
    }

    [Fact]
    public void Interrupted_job_returns_available_and_another_worker_can_claim_it()
    {
        Harness harness = CreateHarness(withSource: true);
        RequireSuccess(harness.Sync.Handle(Command(
            FirstJobId,
            completedBuildingCell: new CellId(30, 0, 0),
            sourceCell: new CellId(2, 0, 0))));
        RequireSuccess(harness.Jobs.Claim(FirstJobId, FirstAgentId, tick: 2));
        RequireSuccess(harness.Jobs.Start(FirstJobId, tick: 2));
        harness.JobRepository.Save(harness.Jobs);

        RequireSuccess(new ReleaseJobAssignmentHandler(
            harness.JobRepository,
            harness.Journal).Handle(new ReleaseJobAssignmentCommand(
                FirstJobId,
                tick: 3)));

        Assert.Equal(JobStatus.Available, harness.Jobs.Get(FirstJobId)!.Status);
        Assert.Single(harness.Inventory.CreateSnapshot().Stacks.Single().Reservations);
        RequireSuccess(harness.Jobs.Claim(FirstJobId, SecondAgentId, tick: 4));
        Assert.Equal(SecondAgentId, harness.Jobs.Get(FirstJobId)!.AssignedAgentId);
    }

    [Fact]
    public void Source_selection_is_distance_then_cell_then_stack_id()
    {
        InventoryState inventory = CreateInventory();
        EntityId farther = Id(20);
        EntityId higherCell = Id(21);
        EntityId lowerCell = Id(22);
        RequireSuccess(inventory.AddUnit(
            farther,
            MushroomLeg,
            ItemLocation.InWorld(new CellId(1, 0, 0)),
            tick: 0));
        RequireSuccess(inventory.AddUnit(
            higherCell,
            MushroomLeg,
            ItemLocation.InWorld(new CellId(8, 1, 0)),
            tick: 0));
        RequireSuccess(inventory.AddUnit(
            lowerCell,
            MushroomLeg,
            ItemLocation.InWorld(new CellId(8, -1, 0)),
            tick: 0));
        CellId[] visible =
        {
            new CellId(1, 0, 0),
            new CellId(8, 1, 0),
            new CellId(8, -1, 0),
        };

        TunnelAutomaticWorkSource? source = TunnelAutomaticWorkPlanner.SelectSource(
            MushroomLeg,
            new CellId(10, 0, 0),
            inventory.GetAvailableWorldStacks(),
            visible,
            visible);

        Assert.True(source.HasValue);
        Assert.Equal(lowerCell, source.Value.StackId);
    }

    private static Harness CreateHarness(bool withSource)
    {
        InMemoryTunnelInfrastructureRepository tunnels =
            new InMemoryTunnelInfrastructureRepository();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        RequireSuccess(new RegisterTunnelSegmentHandler(tunnels, journal).Handle(
            new RegisterTunnelSegmentCommand(
                SegmentId,
                TunnelSegmentOriginKind.RoomExit,
                new CellId(0, 0, 0),
                Cells(30),
                tick: 1)));
        InventoryState inventory = CreateInventory();
        if (withSource)
        {
            RequireSuccess(inventory.AddStack(
                SourceStackId,
                MushroomLeg,
                quantity: 2,
                ItemLocation.InWorld(new CellId(2, 0, 0)),
                tick: 1));
        }

        TestInventoryRepository inventoryRepository = new TestInventoryRepository(inventory);
        InMemoryJobRepository jobRepository = new InMemoryJobRepository();
        return new Harness(
            tunnels,
            inventoryRepository,
            jobRepository,
            journal,
            new SynchronizeTunnelAutomaticSupportHandler(
                tunnels,
                inventoryRepository,
                jobRepository,
                journal));
    }

    private static SynchronizeTunnelAutomaticSupportCommand Command(
        EntityId jobId,
        CellId completedBuildingCell,
        CellId? sourceCell = null,
        long tick = 2)
    {
        CellId[] sources = sourceCell.HasValue
            ? new[] { sourceCell.Value }
            : Array.Empty<CellId>();
        return new SynchronizeTunnelAutomaticSupportCommand(
            SegmentId,
            jobId,
            new[] { completedBuildingCell },
            sources,
            sources,
            tick);
    }

    private static InventoryState CreateInventory()
    {
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(MushroomLeg, "Mushroom leg", 100, isTool: false),
        }));
    }

    private static IReadOnlyList<CellId> Cells(int count)
    {
        return Enumerable.Range(1, count)
            .Select(value => new CellId(value, 0, 0))
            .ToArray();
    }

    private static TunnelAutomaticSupportTargetSnapshot RequireTarget(
        TunnelInfrastructureSnapshot snapshot)
    {
        TunnelAutomaticSupportTargetSnapshot? target = Assert.Single(
            snapshot.Segments).NextAutomaticSupportTarget;
        Assert.True(target.HasValue);
        return target.Value;
    }

    private static void RequireSuccess(Result result)
    {
        Assert.True(result.IsSuccess, result.Error?.ToString());
    }

    private static T RequireSuccess<T>(Result<T> result)
    {
        Assert.True(result.IsSuccess, result.Error?.ToString());
        return result.Value;
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
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
            InMemoryExecutionJournal journal,
            SynchronizeTunnelAutomaticSupportHandler sync)
        {
            Tunnels = tunnels;
            InventoryRepository = inventoryRepository;
            JobRepository = jobRepository;
            Journal = journal;
            Sync = sync;
        }

        public InMemoryTunnelInfrastructureRepository Tunnels { get; }
        public TestInventoryRepository InventoryRepository { get; }
        public InMemoryJobRepository JobRepository { get; }
        public InMemoryExecutionJournal Journal { get; }
        public SynchronizeTunnelAutomaticSupportHandler Sync { get; }
        public InventoryState Inventory => InventoryRepository.Get();
        public JobSystem Jobs => JobRepository.Get();
    }
}
}
