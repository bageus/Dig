using System;
using System.Linq;
using Dig.Application.Inventory;
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
    private static readonly EntityId LegacyJobId = Id(3);
    private static readonly EntityId SourceStackId = Id(4);
    private static readonly CellId Junction = new CellId(20, 8, 1);
    private static readonly CellId SourceCell = new CellId(18, 8, 1);
    private static readonly ItemId Stone = new ItemId("material.stone");

    [Fact]
    public void Junction_target_is_placement_only_without_job_or_reservation()
    {
        Harness harness = CreateHarness(withLegacyJob: false);

        Result<TunnelJunctionTrimPlacementSyncResult> result =
            harness.Sync.Handle(
                new SynchronizeTunnelJunctionTrimPlacementCommand(tick: 2));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(
            TunnelJunctionTrimPlacementSyncStatus.PlacementOnly,
            result.Value.Status);
        Assert.Empty(result.Value.CancelledJobIds);
        Assert.Empty(harness.Jobs.GetAll());
        Assert.Empty(
            harness.Inventory.CreateSnapshot().Stacks.Single().Reservations);
        TunnelJunctionStoneTrimTargetSnapshot target = Assert.Single(
            harness.Tunnels.Get().CaptureSnapshot().PendingJunctionStoneTrimTargets);
        Assert.Equal(Junction, target.Cell);
        Assert.Empty(
            harness.Tunnels.Get().CaptureSnapshot().CompletedJunctionStoneTrimCells);
    }

    [Fact]
    public void Legacy_automatic_trim_job_is_cancelled_and_releases_source()
    {
        Harness harness = CreateHarness(withLegacyJob: true);

        Result<TunnelJunctionTrimPlacementSyncResult> result =
            harness.Sync.Handle(
                new SynchronizeTunnelJunctionTrimPlacementCommand(tick: 3));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(
            TunnelJunctionTrimPlacementSyncStatus.LegacyAutomaticJobsCancelled,
            result.Value.Status);
        Assert.Equal(new[] { LegacyJobId }, result.Value.CancelledJobIds);
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(LegacyJobId)!.Status);
        ItemStackSnapshot source = Assert.Single(
            harness.Inventory.CreateSnapshot().Stacks);
        Assert.Equal(2, source.Quantity);
        Assert.Empty(source.Reservations);
        Assert.Single(
            harness.Tunnels.Get().CaptureSnapshot().PendingJunctionStoneTrimTargets);
    }

    [Fact]
    public void Manual_completion_removes_target_after_placement_only_sync()
    {
        Harness harness = CreateHarness(withLegacyJob: false);
        RequireSuccess(harness.Sync.Handle(
            new SynchronizeTunnelJunctionTrimPlacementCommand(tick: 2)));

        Result completed = new RegisterCompletedJunctionStoneTrimHandler(
            harness.Tunnels,
            harness.Journal).Handle(
                new RegisterCompletedJunctionStoneTrimCommand(Junction, tick: 3));

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        TunnelInfrastructureSnapshot snapshot =
            harness.Tunnels.Get().CaptureSnapshot();
        Assert.Empty(snapshot.PendingJunctionStoneTrimTargets);
        Assert.Contains(Junction, snapshot.CompletedJunctionStoneTrimCells);
        Assert.Empty(harness.Jobs.GetAll());
    }

    private static Harness CreateHarness(bool withLegacyJob)
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
        RequireSuccess(inventory.AddStack(
            SourceStackId,
            Stone,
            quantity: 2,
            ItemLocation.InWorld(SourceCell),
            tick: 1));
        TestInventoryRepository inventoryRepository =
            new TestInventoryRepository(inventory);
        InMemoryJobRepository jobRepository = new InMemoryJobRepository();

        if (withLegacyJob)
        {
            RequireSuccess(inventory.ReserveQuantity(
                SourceStackId,
                LegacyJobId,
                quantity: 1,
                tick: 1));
            RequireSuccess(jobRepository.Get().Add(
                new TunnelAutomaticWorkJobDefinition(
                    LegacyJobId,
                    FirstSegmentId,
                    TunnelAutomaticWorkKind.JunctionStoneTrim,
                    Junction,
                    createdTick: 1,
                    JobRetryPolicy.Default,
                    SourceStackId,
                    SourceCell)));
            RequireSuccess(jobRepository.Get().MakeAvailable(LegacyJobId, tick: 1));
        }

        return new Harness(
            tunnels,
            inventoryRepository,
            jobRepository,
            journal,
            new SynchronizeTunnelJunctionTrimPlacementHandler(
                tunnels,
                inventoryRepository,
                jobRepository,
                journal));
    }

    private static CellId[] Cells(int direction)
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
            SynchronizeTunnelJunctionTrimPlacementHandler sync)
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
        public SynchronizeTunnelJunctionTrimPlacementHandler Sync { get; }
        public InventoryState Inventory => InventoryRepository.Get();
        public JobSystem Jobs => JobRepository.Get();
    }
}
}
