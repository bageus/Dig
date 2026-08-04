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

public sealed class TunnelJunctionStoneTrimApplicationTests
{
    private static readonly EntityId FirstSegmentId = Id(1);
    private static readonly EntityId SecondSegmentId = Id(2);
    private static readonly EntityId FirstJobId = Id(3);
    private static readonly EntityId SecondJobId = Id(4);
    private static readonly EntityId SourceStackId = Id(5);
    private static readonly CellId Junction = new CellId(20, 8, 1);
    private static readonly CellId SourceCell = new CellId(18, 8, 1);
    private static readonly ItemId Stone = new ItemId("material.stone");

    [Fact]
    public void Missing_stone_keeps_one_created_job_without_reservations()
    {
        Harness harness = CreateHarness(withSource: false);

        Result<TunnelAutomaticJunctionTrimSyncResult> result =
            harness.Sync.Handle(Command(FirstJobId));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(
            TunnelAutomaticJunctionTrimSyncStatus.PendingSource,
            result.Value.Status);
        JobSnapshot job = harness.Jobs.Get(FirstJobId)!;
        TunnelAutomaticWorkJobDefinition definition =
            Assert.IsType<TunnelAutomaticWorkJobDefinition>(job.Definition);
        Assert.Equal(TunnelAutomaticWorkKind.JunctionStoneTrim, definition.Kind);
        Assert.Equal(FirstSegmentId, definition.SegmentId);
        Assert.Equal(Junction, definition.TargetCell);
        Assert.Equal(JobStatus.Created, job.Status);
        Assert.Empty(harness.Jobs.GetReservations());
        Assert.Empty(harness.Inventory.CreateSnapshot().Stacks);
    }

    [Fact]
    public void Appearing_stone_resolves_same_job_and_reserves_one_unit()
    {
        Harness harness = CreateHarness(withSource: false);
        RequireSuccess(harness.Sync.Handle(Command(FirstJobId)));
        RequireSuccess(harness.Inventory.AddStack(
            SourceStackId,
            Stone,
            quantity: 2,
            ItemLocation.InWorld(SourceCell),
            tick: 2));

        Result<TunnelAutomaticJunctionTrimSyncResult> result =
            harness.Sync.Handle(Command(FirstJobId, tick: 3, sourceVisible: true));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(
            TunnelAutomaticJunctionTrimSyncStatus.Available,
            result.Value.Status);
        JobSnapshot job = harness.Jobs.Get(FirstJobId)!;
        TunnelAutomaticWorkJobDefinition definition =
            Assert.IsType<TunnelAutomaticWorkJobDefinition>(job.Definition);
        Assert.Equal(JobStatus.Available, job.Status);
        Assert.Equal(SourceStackId, definition.SourceStackId);
        ItemQuantityReservationSnapshot reservation = Assert.Single(
            harness.Inventory.CreateSnapshot().Stacks.Single().Reservations);
        Assert.Equal(FirstJobId, reservation.JobId);
        Assert.Equal(1, reservation.Quantity);
    }

    [Fact]
    public void Removing_owner_segment_cancels_job_and_releases_source()
    {
        Harness harness = CreateHarness(withSource: true);
        RequireSuccess(harness.Sync.Handle(
            Command(FirstJobId, sourceVisible: true)));

        RequireSuccess(harness.Remove.Handle(
            new RemoveTunnelSegmentCommand(FirstSegmentId, tick: 3)));

        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(FirstJobId)!.Status);
        Assert.Empty(
            harness.Inventory.CreateSnapshot().Stacks.Single().Reservations);
        TunnelJunctionStoneTrimTargetSnapshot target = Assert.Single(
            harness.Tunnels.Get().CaptureSnapshot().PendingJunctionStoneTrimTargets);
        Assert.Equal(SecondSegmentId, target.OwnerSegmentId);

        Result<TunnelAutomaticJunctionTrimSyncResult> replacement =
            harness.Sync.Handle(Command(
                SecondJobId,
                tick: 4,
                sourceVisible: true));
        Assert.True(replacement.IsSuccess, replacement.Error?.ToString());
        Assert.Equal(JobStatus.Available, harness.Jobs.Get(SecondJobId)!.Status);
    }

    [Fact]
    public void Completed_trim_removes_target_and_cancels_stale_job()
    {
        Harness harness = CreateHarness(withSource: true);
        RequireSuccess(harness.Sync.Handle(
            Command(FirstJobId, sourceVisible: true)));
        RequireSuccess(new RegisterCompletedJunctionStoneTrimHandler(
            harness.Tunnels,
            harness.Journal).Handle(
                new RegisterCompletedJunctionStoneTrimCommand(Junction, tick: 3)));

        Result<TunnelAutomaticJunctionTrimSyncResult> result =
            harness.Sync.Handle(Command(SecondJobId, tick: 4, sourceVisible: true));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(TunnelAutomaticJunctionTrimSyncStatus.NoTarget, result.Value.Status);
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(FirstJobId)!.Status);
        Assert.Null(harness.Jobs.Get(SecondJobId));
        Assert.Empty(
            harness.Inventory.CreateSnapshot().Stacks.Single().Reservations);
    }

    private static Harness CreateHarness(bool withSource)
    {
        InMemoryTunnelInfrastructureRepository tunnels =
            new InMemoryTunnelInfrastructureRepository();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        RegisterTunnelSegmentHandler register =
            new RegisterTunnelSegmentHandler(tunnels, journal);
        RequireSuccess(register.Handle(new RegisterTunnelSegmentCommand(
            FirstSegmentId,
            TunnelSegmentOriginKind.VerticalJunction,
            Junction,
            Cells(direction: -1),
            tick: 1)));
        RequireSuccess(register.Handle(new RegisterTunnelSegmentCommand(
            SecondSegmentId,
            TunnelSegmentOriginKind.VerticalJunction,
            Junction,
            Cells(direction: 1),
            tick: 1)));

        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(Stone, "Stone", 100, isTool: false),
        }));
        if (withSource)
        {
            RequireSuccess(inventory.AddStack(
                SourceStackId,
                Stone,
                quantity: 2,
                ItemLocation.InWorld(SourceCell),
                tick: 1));
        }

        TestInventoryRepository inventoryRepository =
            new TestInventoryRepository(inventory);
        InMemoryJobRepository jobRepository = new InMemoryJobRepository();
        return new Harness(
            tunnels,
            inventoryRepository,
            jobRepository,
            journal,
            new SynchronizeTunnelAutomaticJunctionTrimHandler(
                tunnels,
                inventoryRepository,
                jobRepository,
                journal),
            new RemoveTunnelSegmentHandler(
                tunnels,
                inventoryRepository,
                jobRepository,
                journal));
    }

    private static SynchronizeTunnelAutomaticJunctionTrimCommand Command(
        EntityId jobId,
        long tick = 2,
        bool sourceVisible = false)
    {
        CellId[] sources = sourceVisible
            ? new[] { SourceCell }
            : Array.Empty<CellId>();
        return new SynchronizeTunnelAutomaticJunctionTrimCommand(
            Junction,
            jobId,
            new[] { new CellId(30, 8, 1) },
            sources,
            sources,
            tick);
    }

    private static IReadOnlyList<CellId> Cells(int direction)
    {
        return Enumerable.Range(1, 20)
            .Select(distance => new CellId(
                Junction.X + (distance * direction),
                Junction.Y,
                Junction.Z))
            .ToArray();
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
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
            SynchronizeTunnelAutomaticJunctionTrimHandler sync,
            RemoveTunnelSegmentHandler remove)
        {
            Tunnels = tunnels;
            InventoryRepository = inventoryRepository;
            JobRepository = jobRepository;
            Journal = journal;
            Sync = sync;
            Remove = remove;
        }

        public InMemoryTunnelInfrastructureRepository Tunnels { get; }
        public TestInventoryRepository InventoryRepository { get; }
        public InMemoryJobRepository JobRepository { get; }
        public InMemoryExecutionJournal Journal { get; }
        public SynchronizeTunnelAutomaticJunctionTrimHandler Sync { get; }
        public RemoveTunnelSegmentHandler Remove { get; }
        public InventoryState Inventory => InventoryRepository.Get();
        public JobSystem Jobs => JobRepository.Get();
    }
}
}
