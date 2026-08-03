using System;
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

public sealed class TunnelTopologyProvenanceSynchronizationTests
{
    private static readonly EntityId SegmentId = Id(1);
    private static readonly EntityId ReplacementId = Id(2);
    private static readonly EntityId JobId = Id(3);
    private static readonly EntityId SourceStackId = Id(4);
    private static readonly CellId Origin = new CellId(10, 8, 1);
    private static readonly CellId SourceCell = new CellId(2, 8, 1);
    private static readonly ItemId Leg = new ItemId("material.mushroom_leg");

    [Fact]
    public void Repeated_completed_provenance_is_idempotent()
    {
        Harness harness = CreateHarness();
        TunnelTopologySegmentProvenance segment = Segment(length: 12);
        RequireSuccess(harness.Sync.Handle(Command(segment, tick: 1)));
        long version = harness.Tunnels.Get().Version;
        int eventCount = harness.Journal.Events.Count;

        TunnelTopologySynchronizationResult result = RequireSuccess(
            harness.Sync.Handle(Command(segment, tick: 2)));

        Assert.Equal(0, result.Added);
        Assert.Equal(0, result.Updated);
        Assert.Equal(0, result.Removed);
        Assert.Equal(1, result.Retained);
        Assert.Equal(version, harness.Tunnels.Get().Version);
        Assert.Equal(eventCount, harness.Journal.Events.Count);
    }

    [Fact]
    public void Extension_preserves_completed_anchor_and_derives_new_target()
    {
        Harness harness = CreateHarness();
        RequireSuccess(harness.Sync.Handle(Command(Segment(length: 15), tick: 1)));
        RequireSuccess(harness.Tunnels.Get().RegisterCompletedWoodenSupport(
            SegmentId,
            new CellId(20, 8, 1),
            tick: 2));
        harness.Tunnels.Save(harness.Tunnels.Get());
        harness.Journal.Append(harness.Tunnels.Get().DequeueUncommittedEvents());
        Assert.Null(harness.Tunnels.Get().GetSegment(SegmentId)!
            .NextAutomaticSupportTarget);

        TunnelTopologySynchronizationResult result = RequireSuccess(
            harness.Sync.Handle(Command(Segment(length: 25), tick: 3)));

        Assert.Equal(1, result.Updated);
        HorizontalTunnelSegmentSnapshot segment =
            harness.Tunnels.Get().GetSegment(SegmentId)!;
        Assert.Contains(segment.StructuralAnchors, anchor =>
            anchor.Cell == new CellId(20, 8, 1)
            && anchor.Kind == TunnelStructuralAnchorKind.WoodenSupport);
        Assert.Equal(
            new CellId(30, 8, 1),
            segment.NextAutomaticSupportTarget!.Value.TargetCell);
    }

    [Fact]
    public void Shortening_cancels_obsolete_support_and_releases_source()
    {
        Harness harness = CreateHarness(withSource: true);
        RequireSuccess(harness.Sync.Handle(Command(Segment(length: 20), tick: 1)));
        AddAvailableSupportJob(harness, target: new CellId(20, 8, 1));

        TunnelTopologySynchronizationResult result = RequireSuccess(
            harness.Sync.Handle(Command(Segment(length: 5), tick: 3)));

        Assert.Equal(1, result.Updated);
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(JobId)!.Status);
        Assert.Empty(harness.Inventory.CreateSnapshot().Stacks.Single().Reservations);
        Assert.Null(harness.Tunnels.Get().GetSegment(SegmentId)!
            .NextAutomaticSupportTarget);
    }

    [Fact]
    public void Removing_direction_cancels_every_automatic_job()
    {
        Harness harness = CreateHarness(withSource: true);
        RequireSuccess(harness.Sync.Handle(Command(Segment(length: 20), tick: 1)));
        AddAvailableSupportJob(harness, target: new CellId(20, 8, 1));

        TunnelTopologySynchronizationResult result = RequireSuccess(
            harness.Sync.Handle(new SynchronizeTunnelTopologyCommand(
                Array.Empty<TunnelTopologySegmentProvenance>(),
                tick: 3)));

        Assert.Equal(1, result.Removed);
        Assert.Null(harness.Tunnels.Get().GetSegment(SegmentId));
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(JobId)!.Status);
        Assert.Empty(harness.Inventory.CreateSnapshot().Stacks.Single().Reservations);
    }

    [Fact]
    public void Completed_junction_trim_survives_geometry_extension()
    {
        Harness harness = CreateHarness();
        TunnelTopologySegmentProvenance initial = Segment(
            length: 8,
            kind: TunnelSegmentOriginKind.VerticalJunction);
        RequireSuccess(harness.Sync.Handle(Command(initial, tick: 1)));
        RequireSuccess(harness.Tunnels.Get().RegisterCompletedJunctionStoneTrim(
            Origin,
            tick: 2));
        harness.Tunnels.Save(harness.Tunnels.Get());
        harness.Journal.Append(harness.Tunnels.Get().DequeueUncommittedEvents());

        RequireSuccess(harness.Sync.Handle(Command(Segment(
            length: 18,
            kind: TunnelSegmentOriginKind.VerticalJunction), tick: 3)));

        TunnelInfrastructureSnapshot snapshot = harness.Tunnels.Get().CaptureSnapshot();
        Assert.Contains(Origin, snapshot.CompletedJunctionStoneTrimCells);
        Assert.Empty(snapshot.PendingJunctionStoneTrimTargets);
    }

    [Fact]
    public void Stable_identity_change_rejects_before_mutation()
    {
        Harness harness = CreateHarness();
        RequireSuccess(harness.Sync.Handle(Command(Segment(length: 12), tick: 1)));
        TunnelInfrastructureSnapshot before = harness.Tunnels.Get().CaptureSnapshot();

        Result<TunnelTopologySynchronizationResult> result = harness.Sync.Handle(
            Command(Segment(length: 20, id: ReplacementId), tick: 2));

        Assert.True(result.IsFailure);
        Assert.Equal(
            TunnelTopologySynchronizationErrors.SegmentIdentityMismatch,
            result.Error);
        TunnelInfrastructureSnapshot after = harness.Tunnels.Get().CaptureSnapshot();
        Assert.Equal(before.Version, after.Version);
        Assert.Equal(
            before.Segments.Single().OrderedHorizontalCells,
            after.Segments.Single().OrderedHorizontalCells);
    }

    private static Harness CreateHarness(bool withSource = false)
    {
        InMemoryTunnelInfrastructureRepository tunnels =
            new InMemoryTunnelInfrastructureRepository();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(Leg, "Mushroom leg", 100, isTool: false),
        }));
        if (withSource)
        {
            RequireSuccess(inventory.AddStack(
                SourceStackId,
                Leg,
                quantity: 2,
                ItemLocation.InWorld(SourceCell),
                tick: 1));
        }

        TestInventoryRepository inventoryRepository =
            new TestInventoryRepository(inventory);
        InMemoryJobRepository jobs = new InMemoryJobRepository();
        return new Harness(
            tunnels,
            inventoryRepository,
            jobs,
            journal,
            new SynchronizeTunnelTopologyHandler(
                tunnels,
                inventoryRepository,
                jobs,
                journal));
    }

    private static void AddAvailableSupportJob(Harness harness, CellId target)
    {
        RequireSuccess(harness.Inventory.ReserveQuantity(
            SourceStackId,
            JobId,
            quantity: 1,
            tick: 2));
        RequireSuccess(harness.Jobs.Add(new TunnelAutomaticWorkJobDefinition(
            JobId,
            SegmentId,
            TunnelAutomaticWorkKind.WoodenSupport,
            target,
            createdTick: 2,
            JobRetryPolicy.Default,
            SourceStackId,
            SourceCell)));
        RequireSuccess(harness.Jobs.MakeAvailable(JobId, tick: 2));
    }

    private static SynchronizeTunnelTopologyCommand Command(
        TunnelTopologySegmentProvenance segment,
        long tick)
    {
        return new SynchronizeTunnelTopologyCommand(new[] { segment }, tick);
    }

    private static TunnelTopologySegmentProvenance Segment(
        int length,
        TunnelSegmentOriginKind kind = TunnelSegmentOriginKind.RoomExit,
        EntityId? id = null)
    {
        return new TunnelTopologySegmentProvenance(
            id ?? SegmentId,
            kind,
            Origin,
            Enumerable.Range(1, length)
                .Select(distance => new CellId(
                    Origin.X + distance,
                    Origin.Y,
                    Origin.Z)));
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
            SynchronizeTunnelTopologyHandler sync)
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
        public SynchronizeTunnelTopologyHandler Sync { get; }
        public InventoryState Inventory => InventoryRepository.Get();
        public JobSystem Jobs => JobRepository.Get();
    }
}
}
