using System;
using System.Linq;
using Dig.Application.Rooms;
using Dig.Application.Saving;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Rooms;
using Dig.Domain.World;
using Dig.Infrastructure.Saving;
using Xunit;

namespace Dig.Tests
{
public sealed class RoomInfrastructureSaveTests
{
    private static readonly EntityId RoomId = Id(1);
    private static readonly EntityId WorkJobId = RoomUpgradeRuntimeIdentity.CreateJobId(1);
    private static readonly EntityId WorkerId = Id(2);
    private static readonly CellId StockCell = new CellId(5, 5, 0);
    [Fact]
    public void Adapter_round_trip_preserves_ready_room_job_reservations_and_sequence()
    {
        RoomSaveHarness harness = CreateReadyHarness();
        RoomInfrastructureRuntimeSnapshot runtime = CreateRuntime(harness.Rooms, 6);
        RoomInfrastructureSaveData saved = RoomInfrastructureSaveAdapter.Encode(
            runtime,
            harness.Inventory,
            harness.Jobs,
            harness.WorldSize);
        Result<RoomInfrastructureRuntimeSnapshot> restored =
            RoomInfrastructureSaveAdapter.Decode(
                saved,
                harness.Inventory,
                harness.Jobs,
                harness.WorldSize);

        Assert.True(restored.IsSuccess, restored.Error?.ToString());
        Assert.Equal((ulong)6, restored.Value.NextRuntimeSequence);
        AssertSnapshotsEqual(runtime.Infrastructure, restored.Value.Infrastructure);
        Assert.Single(restored.Value.Provenance);
        Assert.Equal(WorkJobId, restored.Value.Infrastructure.Rooms[0].ActiveJobIds[0]);
        Assert.Equal(8, harness.Inventory.GetReservedQuantityAt(
            WorkJobId,
            RoomUpgradeMaterialIds.Stone,
            ItemLocation.InWorld(StockCell))
            + harness.Inventory.GetReservedQuantityAt(
                WorkJobId,
                RoomUpgradeMaterialIds.MushroomLeg,
                ItemLocation.InWorld(StockCell)));
        Assert.Equal(JobStatus.Claimed, harness.Jobs.Get(WorkJobId)!.Status);
        Assert.NotEmpty(harness.Jobs.GetReservations());
    }

    [Fact]
    public void Version_fifteen_migration_adds_no_rooms_and_advances_sequence()
    {
        SaveGameDocument document = new SaveGameDocument
        {
            FormatVersion = 15,
            RoomInfrastructure = null!,
            Inventory = new InventorySaveData
            {
                Stacks =
                {
                    new ItemStackSaveData
                    {
                        StackId = RoomUpgradeRuntimeIdentity
                            .CreateTransitStackId(8).ToString(),
                    },
                },
            },
            Jobs = new JobsSaveData
            {
                Jobs =
                {
                    new JobSaveData
                    {
                        Definition = new JobDefinitionSaveData
                        {
                            JobId = RoomUpgradeRuntimeIdentity
                                .CreateJobId(5).ToString(),
                        },
                    },
                },
            },
        };

        new SaveVersionFifteenRoomInfrastructureMigration().Apply(document);

        Assert.Equal(16, document.FormatVersion);
        Assert.NotNull(document.RoomInfrastructure);
        Assert.Equal((ulong)9, document.RoomInfrastructure.NextRuntimeSequence);
        Assert.Empty(document.RoomInfrastructure.Rooms);
        Assert.Empty(document.RoomInfrastructure.Provenance);
    }

    [Fact]
    public void Decode_rejects_provenance_or_sequence_drift_atomically()
    {
        RoomSaveHarness harness = CreateReadyHarness();
        RoomInfrastructureSaveData saved = RoomInfrastructureSaveAdapter.Encode(
            CreateRuntime(harness.Rooms, 6),
            harness.Inventory,
            harness.Jobs,
            harness.WorldSize);
        saved.NextRuntimeSequence = 1;
        saved.Provenance[0].OrderedRoomCells[0].X = 99;

        Result<RoomInfrastructureRuntimeSnapshot> restored =
            RoomInfrastructureSaveAdapter.Decode(
                saved,
                harness.Inventory,
                harness.Jobs,
                harness.WorldSize);

        Assert.True(restored.IsFailure);
        Assert.Equal(RoomInfrastructureSaveErrors.InvalidSnapshot, restored.Error);
        Assert.Equal(RoomImprovementStatus.ReadyForWork, harness.Rooms.Get(RoomId)!.Status);
        Assert.Equal(JobStatus.Claimed, harness.Jobs.Get(WorkJobId)!.Status);
    }

    [Fact]
    public void Save_document_round_trip_is_byte_stable_with_active_room_runtime()
    {
        RoomSaveHarness harness = CreateReadyHarness();
        MaterialCatalog materials = Materials();
        ItemCatalog items = Items();
        WorldState world = WorldState.CreateFilled(
            harness.WorldSize,
            chunkSize: 4,
            materials,
            new MaterialId("terrain.rock"),
            explored: true).Value;
        JobDefinitionSaveRegistry registry =
            SaveGameCompositionRoot.CreateJobDefinitionRegistry();
        SaveGameBuilder builder = new SaveGameBuilder(registry);
        SaveGameContext context = new SaveGameContext(
            Metadata(),
            world,
            harness.Inventory,
            harness.Jobs,
            new BuildingsState(),
            Array.Empty<AgentState>(),
            roomInfrastructure: CreateRuntime(harness.Rooms, 6));
        DataContractJsonSaveCodec codec = new DataContractJsonSaveCodec();
        SaveGameDocument document = builder.Build(context);
        byte[] first = codec.Serialize(document);
        Result<LoadedGameState> loaded = new SaveGameLoader(
            new SaveMigrationPipeline(Array.Empty<ISaveMigration>()),
            registry).Load(codec.Deserialize(first), materials, items);

        Assert.True(loaded.IsSuccess, loaded.Error?.ToString());
        AssertSnapshotsEqual(
            context.RoomInfrastructure.Infrastructure,
            loaded.Value.RoomInfrastructure.Infrastructure);
        Assert.Equal(
            context.RoomInfrastructure.NextRuntimeSequence,
            loaded.Value.RoomInfrastructure.NextRuntimeSequence);
        SaveGameDocument rebuilt = builder.Build(new SaveGameContext(
            loaded.Value.Metadata,
            loaded.Value.World,
            loaded.Value.Inventory,
            loaded.Value.Jobs,
            loaded.Value.Buildings,
            Array.Empty<AgentState>(),
            roomInfrastructure: loaded.Value.RoomInfrastructure));
        Assert.Equal(first, codec.Serialize(rebuilt));
    }

    private static RoomSaveHarness CreateReadyHarness()
    {
        RoomInfrastructureState rooms = new RoomInfrastructureState();
        RequireSuccess(rooms.RegisterCompletedTemplateRoom(
            RoomId, "room.small.1", RoomTemplateKind.Small, tick: 0));
        RequireSuccess(rooms.OrderUpgrade(RoomId, RoomPurposeKind.Workshop, tick: 1));
        RequireSuccess(rooms.AssignTemporaryStockCell(RoomId, StockCell, tick: 2));
        Deliver(rooms, RoomUpgradeMaterialIds.Stone, 4, 2, tick: 3);
        Deliver(rooms, RoomUpgradeMaterialIds.MushroomLeg, 4, 3, tick: 4);
        RequireSuccess(rooms.AttachJob(RoomId, WorkJobId, tick: 5));

        InventoryState inventory = new InventoryState(Items());
        RequireSuccess(inventory.AddStack(
            RoomUpgradeRuntimeIdentity.CreateTransitStackId(4),
            RoomUpgradeMaterialIds.Stone,
            4,
            ItemLocation.InWorld(StockCell),
            tick: 5));
        RequireSuccess(inventory.AddStack(
            RoomUpgradeRuntimeIdentity.CreateTransitStackId(5),
            RoomUpgradeMaterialIds.MushroomLeg,
            4,
            ItemLocation.InWorld(StockCell),
            tick: 5));
        RequireSuccess(inventory.ReserveAvailableAt(
            ItemLocation.InWorld(StockCell),
            RoomUpgradeMaterialIds.Stone,
            WorkJobId,
            4,
            tick: 6));
        RequireSuccess(inventory.ReserveAvailableAt(
            ItemLocation.InWorld(StockCell),
            RoomUpgradeMaterialIds.MushroomLeg,
            WorkJobId,
            4,
            tick: 6));

        JobSystem jobs = new JobSystem();
        RequireSuccess(jobs.Add(new RoomUpgradeWorkJobDefinition(
            WorkJobId,
            RoomId,
            StockCell,
            priority: 500,
            createdTick: 5,
            JobRetryPolicy.Default)));
        RequireSuccess(jobs.MakeAvailable(WorkJobId, tick: 6));
        RequireSuccess(jobs.Claim(WorkJobId, WorkerId, tick: 7));
        return new RoomSaveHarness(
            rooms,
            inventory,
            jobs,
            new WorldSize(16, 16, 4));
    }

    private static void Deliver(
        RoomInfrastructureState rooms,
        ItemId item,
        int quantity,
        ulong sequence,
        long tick)
    {
        EntityId delivery = RoomUpgradeRuntimeIdentity.CreateJobId(sequence);
        RequireSuccess(rooms.AttachJob(RoomId, delivery, tick));
        RequireSuccess(rooms.RecordDelivery(RoomId, delivery, item, quantity, tick + 1));
    }

    private static RoomInfrastructureRuntimeSnapshot CreateRuntime(
        RoomInfrastructureState rooms,
        ulong sequence)
    {
        return new RoomInfrastructureRuntimeSnapshot(
            rooms.CaptureSnapshot(),
            new[]
            {
                new CompletedRoomInfrastructureProvenance(
                    RoomId,
                    "room.small.1",
                    RoomTemplateKind.Small,
                    new[]
                    {
                        new CellId(4, 4, 0),
                        StockCell,
                        new CellId(6, 5, 0),
                    }),
            },
            sequence);
    }

    private static void AssertSnapshotsEqual(
        RoomInfrastructureSnapshot expected,
        RoomInfrastructureSnapshot actual)
    {
        Assert.Equal(expected.Version, actual.Version);
        Assert.Equal(expected.Rooms.Count, actual.Rooms.Count);
        for (int index = 0; index < expected.Rooms.Count; index++)
        {
            RoomInfrastructureProjectSnapshot left = expected.Rooms[index];
            RoomInfrastructureProjectSnapshot right = actual.Rooms[index];
            Assert.Equal(left.RoomInfrastructureId, right.RoomInfrastructureId);
            Assert.Equal(left.TemplateInstanceId, right.TemplateInstanceId);
            Assert.Equal(left.TemplateKind, right.TemplateKind);
            Assert.Equal(left.UpgradeOrderCount, right.UpgradeOrderCount);
            Assert.Equal(left.Status, right.Status);
            Assert.Equal(left.CancellationLocked, right.CancellationLocked);
            Assert.Equal(left.RequestedPurpose, right.RequestedPurpose);
            Assert.Equal(left.ActivePurpose, right.ActivePurpose);
            Assert.Equal(left.TemporaryStockCell, right.TemporaryStockCell);
            Assert.Equal(left.Materials.Select(MaterialTuple),
                right.Materials.Select(MaterialTuple));
            Assert.Equal(left.CompletedMaterialUnits, right.CompletedMaterialUnits);
            Assert.Equal(left.ActiveJobIds, right.ActiveJobIds);
            Assert.Equal(left.Version, right.Version);
        }
    }

    private static object MaterialTuple(RoomMaterialLedgerSnapshot value) =>
        new
        {
            value.ItemId,
            value.Required,
            value.Delivered,
            value.Consumed,
            value.ReleasedOnCancel,
        };

    private static MaterialCatalog Materials() => new MaterialCatalog(new[]
    {
        new MaterialDefinition(new MaterialId("terrain.rock"), true, 100),
    });

    private static ItemCatalog Items() => new ItemCatalog(new[]
    {
        Item(RoomUpgradeMaterialIds.Stone),
        Item(RoomUpgradeMaterialIds.MushroomLeg),
        Item(RoomUpgradeMaterialIds.Iron),
        Item(RoomUpgradeMaterialIds.Crystal),
    });

    private static ItemDefinition Item(ItemId id) =>
        new ItemDefinition(id, id.ToString(), 100, isTool: false);

    private static SaveMetadataData Metadata() => new SaveMetadataData
    {
        SlotId = "room-save",
        DisplayName = "Room save",
        SavedAtUtc = "2026-08-03T13:00:00Z",
        SimulationTick = 20,
        WorldSeed = 42,
        GeneratorVersion = 1,
    };

    private static EntityId Id(int value) =>
        EntityId.Parse(value.ToString("x32"));

    private static void RequireSuccess(Result result) =>
        Assert.True(result.IsSuccess, result.Error?.ToString());

    private sealed class RoomSaveHarness
    {
        public RoomSaveHarness(
            RoomInfrastructureState rooms,
            InventoryState inventory,
            JobSystem jobs,
            WorldSize worldSize)
        {
            Rooms = rooms;
            Inventory = inventory;
            Jobs = jobs;
            WorldSize = worldSize;
        }

        public RoomInfrastructureState Rooms { get; }
        public InventoryState Inventory { get; }
        public JobSystem Jobs { get; }
        public WorldSize WorldSize { get; }
    }
}
}
