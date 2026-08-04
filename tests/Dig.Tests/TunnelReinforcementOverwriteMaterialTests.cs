using System;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Tunnels;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class TunnelReinforcementOverwriteMaterialTests
{
    private static readonly EntityId LeftSegmentId = Id(1);
    private static readonly EntityId RightSegmentId = Id(2);
    private static readonly CellId Junction = new CellId(20, 8, 1);

    [Fact]
    public void Removing_completed_wooden_support_creates_no_recovered_material()
    {
        Harness harness = CreateHarness();
        TunnelTopologySegmentProvenance segment = new TunnelTopologySegmentProvenance(
            LeftSegmentId,
            TunnelSegmentOriginKind.RoomExit,
            new CellId(0, 0, 0),
            Enumerable.Range(1, 20).Select(x => new CellId(x, 0, 0)));
        RequireSuccess(harness.Sync.Handle(new SynchronizeTunnelTopologyCommand(
            new[] { segment },
            tick: 1)));
        RequireSuccess(harness.Tunnels.Get().RegisterCompletedWoodenSupport(
            LeftSegmentId,
            new CellId(10, 0, 0),
            tick: 2));
        harness.Tunnels.Save(harness.Tunnels.Get());

        RequireSuccess(harness.Sync.Handle(new SynchronizeTunnelTopologyCommand(
            Array.Empty<TunnelTopologySegmentProvenance>(),
            tick: 3)));

        Assert.Null(harness.Tunnels.Get().GetSegment(LeftSegmentId));
        Assert.Empty(harness.Inventory.Get().CreateSnapshot().Stacks);
    }

    [Fact]
    public void Removing_last_junction_direction_creates_no_recovered_stone()
    {
        Harness harness = CreateHarness();
        TunnelTopologySegmentProvenance left = Segment(LeftSegmentId, direction: -1);
        TunnelTopologySegmentProvenance right = Segment(RightSegmentId, direction: 1);
        RequireSuccess(harness.Sync.Handle(new SynchronizeTunnelTopologyCommand(
            new[] { left, right },
            tick: 1)));
        RequireSuccess(harness.Tunnels.Get().RegisterCompletedJunctionStoneTrim(
            Junction,
            tick: 2));
        harness.Tunnels.Save(harness.Tunnels.Get());

        RequireSuccess(harness.Sync.Handle(new SynchronizeTunnelTopologyCommand(
            Array.Empty<TunnelTopologySegmentProvenance>(),
            tick: 3)));

        Assert.Empty(
            harness.Tunnels.Get().CaptureSnapshot().CompletedJunctionStoneTrimCells);
        Assert.Empty(harness.Inventory.Get().CreateSnapshot().Stacks);
    }

    private static TunnelTopologySegmentProvenance Segment(
        EntityId id,
        int direction)
    {
        return new TunnelTopologySegmentProvenance(
            id,
            TunnelSegmentOriginKind.VerticalJunction,
            Junction,
            Enumerable.Range(1, 20).Select(distance => new CellId(
                Junction.X + (distance * direction),
                Junction.Y,
                Junction.Z)));
    }

    private static Harness CreateHarness()
    {
        InMemoryTunnelInfrastructureRepository tunnels =
            new InMemoryTunnelInfrastructureRepository();
        TestInventoryRepository inventory = new TestInventoryRepository(
            new InventoryState(new ItemCatalog(new[]
            {
                new ItemDefinition(
                    new ItemId("material.mushroom_leg"),
                    "Mushroom leg",
                    100,
                    isTool: false),
                new ItemDefinition(
                    new ItemId("material.stone"),
                    "Stone",
                    100,
                    isTool: false),
            })));
        InMemoryJobRepository jobs = new InMemoryJobRepository();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        return new Harness(
            tunnels,
            inventory,
            new SynchronizeTunnelTopologyHandler(
                tunnels,
                inventory,
                jobs,
                journal));
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
            TestInventoryRepository inventory,
            SynchronizeTunnelTopologyHandler sync)
        {
            Tunnels = tunnels;
            Inventory = inventory;
            Sync = sync;
        }

        public InMemoryTunnelInfrastructureRepository Tunnels { get; }
        public TestInventoryRepository Inventory { get; }
        public SynchronizeTunnelTopologyHandler Sync { get; }
    }
}
}
