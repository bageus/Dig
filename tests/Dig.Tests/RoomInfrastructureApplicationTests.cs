using System;
using System.Linq;
using Dig.Application.Rooms;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Rooms;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class RoomInfrastructureApplicationTests
{
    [Fact]
    public void Projector_emits_only_completed_instances_with_stable_identity()
    {
        ExcavationTemplateInstance completed = Instance("completed", CaveRoomPresetKind.Small);
        ExcavationTemplateInstance active = Instance("active", CaveRoomPresetKind.Medium);
        Complete(completed);
        RoomInfrastructureProvenanceProjector projector =
            new RoomInfrastructureProvenanceProjector();

        var first = projector.Project(new[] { active, completed });
        var second = projector.Project(new[] { completed });

        CompletedRoomInfrastructureProvenance room = Assert.Single(first);
        Assert.Equal("completed", room.TemplateInstanceId);
        Assert.Equal(RoomTemplateKind.Small, room.TemplateKind);
        Assert.Equal(room.RoomInfrastructureId, Assert.Single(second).RoomInfrastructureId);
        Assert.Equal(completed.OrderedMask, room.OrderedRoomCells);
    }

    [Fact]
    public void Synchronization_is_idempotent_and_rejects_stable_identity_drift()
    {
        InMemoryRoomInfrastructureRepository repository =
            new InMemoryRoomInfrastructureRepository();
        InMemoryEventJournal journal = new InMemoryEventJournal();
        SynchronizeCompletedRoomInfrastructureHandler handler =
            new SynchronizeCompletedRoomInfrastructureHandler(repository, journal);
        CompletedRoomInfrastructureProvenance room = Provenance(
            Id(1),
            "template.1",
            RoomTemplateKind.Small,
            new CellId(2, 2, 0));

        var first = handler.Handle(new SynchronizeCompletedRoomInfrastructureCommand(
            new[] { room },
            tick: 1));
        long version = repository.Get().Version;
        var replay = handler.Handle(new SynchronizeCompletedRoomInfrastructureCommand(
            new[] { room },
            tick: 2));
        var drift = handler.Handle(new SynchronizeCompletedRoomInfrastructureCommand(
            new[]
            {
                Provenance(Id(2), "template.1", RoomTemplateKind.Small, new CellId(2, 2, 0)),
            },
            tick: 3));

        Assert.True(first.IsSuccess);
        Assert.Equal(1, first.Value.Added);
        Assert.True(replay.IsSuccess);
        Assert.Equal(1, replay.Value.Retained);
        Assert.Equal(version, repository.Get().Version);
        Assert.True(drift.IsFailure);
    }

    [Fact]
    public void Stock_planner_uses_center_distance_then_stable_cell_and_reports_blocked()
    {
        CellId left = new CellId(2, 4, 0);
        CellId center = new CellId(3, 4, 0);
        CellId right = new CellId(4, 4, 0);
        CompletedRoomInfrastructureProvenance room = Provenance(
            Id(1),
            "template.1",
            RoomTemplateKind.Small,
            left,
            center,
            right);
        WorldSnapshot world = World(left, center, right);
        RoomTemporaryStockCellPlanner planner = new RoomTemporaryStockCellPlanner();

        RoomTemporaryStockCellPlan assigned = planner.Plan(
            room,
            world,
            new[] { left, center, right },
            new[] { center });
        RoomTemporaryStockCellPlan blocked = planner.Plan(
            room,
            world,
            new[] { left, center, right },
            new[] { left, center, right });

        Assert.Equal(RoomTemporaryStockCellPlanStatus.Assigned, assigned.Status);
        Assert.Equal(left, assigned.Cell);
        Assert.Equal(
            RoomTemporaryStockCellPlanStatus.BlockedNoFreeReachableCell,
            blocked.Status);
        Assert.Null(blocked.Cell);
    }

    [Fact]
    public void Stock_handler_assigns_once_and_diagnostics_expose_typed_block_reason()
    {
        CellId cell = new CellId(3, 4, 0);
        CompletedRoomInfrastructureProvenance room = Provenance(
            Id(1),
            "template.1",
            RoomTemplateKind.Small,
            cell);
        RoomInfrastructureState state = new RoomInfrastructureState();
        Assert.True(state.RegisterCompletedTemplateRoom(
            room.RoomInfrastructureId,
            room.TemplateInstanceId,
            room.TemplateKind,
            tick: 0).IsSuccess);
        Assert.True(state.OrderUpgrade(
            room.RoomInfrastructureId,
            RoomPurposeKind.Workshop,
            tick: 1).IsSuccess);
        InMemoryRoomInfrastructureRepository repository =
            new InMemoryRoomInfrastructureRepository(state);
        SynchronizeRoomTemporaryStockCellHandler handler =
            new SynchronizeRoomTemporaryStockCellHandler(
                repository,
                new InMemoryEventJournal());

        var plan = handler.Handle(new SynchronizeRoomTemporaryStockCellCommand(
            room,
            World(cell),
            new[] { cell },
            Array.Empty<CellId>(),
            tick: 2));
        RoomInfrastructureDiagnostic diagnostic = Assert.Single(
            new RoomInfrastructureDiagnosticsProjector().Project(
                repository.Get().CaptureSnapshot()));

        Assert.True(plan.IsSuccess);
        Assert.Equal(cell, repository.Get().Get(room.RoomInfrastructureId)!.TemporaryStockCell);
        Assert.Equal(RoomInfrastructureBlockReason.MaterialsIncomplete, diagnostic.BlockReason);
        Assert.True(diagnostic.CancellationAllowed);
        Assert.Equal(RoomPurposeKind.Workshop, diagnostic.RequestedPurpose);
        Assert.Equal(RoomPurposeKind.None, diagnostic.ActivePurpose);
    }

    private static CompletedRoomInfrastructureProvenance Provenance(
        EntityId id,
        string instanceId,
        RoomTemplateKind kind,
        params CellId[] cells)
    {
        return new CompletedRoomInfrastructureProvenance(id, instanceId, kind, cells);
    }

    private static WorldSnapshot World(params CellId[] cells)
    {
        CellSnapshot[] snapshots = cells.Select(cell => new CellSnapshot(
            cell,
            new CellState(
                new MaterialId("terrain.air"),
                CellDesignation.None,
                isExplored: true,
                damage: 0,
                temperature: 0),
            isSolid: false,
            hardness: 0,
            worldVersion: 1)).ToArray();
        return new WorldSnapshot(
            new WorldSize(20, 20),
            chunkSize: 20,
            version: 1,
            new[]
            {
                new ChunkSnapshot(
                    new ChunkId(0, 0, 0),
                    new CellBounds(0, 0, 0, 20, 20, 1),
                    worldVersion: 1,
                    chunkVersion: 1,
                    snapshots),
            });
    }

    private static ExcavationTemplateInstance Instance(
        string id,
        CaveRoomPresetKind kind)
    {
        CaveRoomPreset preset = CaveRoomPresetCatalog.Get(kind);
        CellId[] cells = Enumerable.Range(0, preset.Depth)
            .SelectMany(z => Enumerable.Range(0, preset.BaseWidth)
                .Select(x => new CellId(5 + x, 5, z)))
            .ToArray();
        CaveRoomPlan plan = CaveRoomPlan.CreateSnapshot(
            preset,
            new CellId(5, 5, 0),
            cells.Where(cell => cell.Z == 0),
            cells,
            Array.Empty<CellId>());
        CaveRoomTemplatePlacementUnlock unlock = CaveRoomTemplatePlacementUnlock.Capture(
            new CaveRoomTemplateUnlockEvaluator().Evaluate(new[]
            {
                new CaveRoomTemplateCandidate(
                    "resident.builder",
                    preset.RequiredStoneworkUnits,
                    isEligible: true),
            }).Get(kind));
        return new ExcavationTemplateInstanceFactory().Create(id, plan, unlock, "style");
    }

    private static void Complete(ExcavationTemplateInstance instance)
    {
        foreach (CellId cell in instance.OrderedMask)
        {
            instance.MarkExcavated(cell);
        }
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
