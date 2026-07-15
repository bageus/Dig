using Dig.Application.Inventory;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Storage;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class StorageZonePlacementTests
{
    private static readonly EntityId ZoneId =
        EntityId.Parse("b1000000000000000000000000000001");
    private static readonly EntityId StackId =
        EntityId.Parse("b2000000000000000000000000000001");
    private static readonly EntityId JobId =
        EntityId.Parse("b3000000000000000000000000000001");
    private static readonly ItemId Stone = new ItemId("material.stone");
    private static readonly MaterialId Air = new MaterialId("terrain.air");
    private static readonly MaterialId Rock = new MaterialId("terrain.rock");

    [Fact]
    public void Empty_zone_moves_to_open_world_cell_and_publishes_event()
    {
        Harness harness = CreateHarness(fill: Air);
        MoveStorageZoneHandler handler = harness.CreateHandler();

        Result result = handler.Handle(
            new MoveStorageZoneCommand(ZoneId, new CellId(3, 2), tick: 4));

        Assert.True(result.IsSuccess);
        Assert.Equal(new CellId(3, 2), harness.Storage.GetZone(ZoneId)!.Cell);
        Assert.Contains(harness.Journal.Events, value => value is StorageZoneMoved);
    }

    [Fact]
    public void Solid_cell_is_rejected_without_storage_mutation()
    {
        Harness harness = CreateHarness(fill: Rock);
        long version = harness.Storage.Version;

        Result result = harness.CreateHandler().Handle(
            new MoveStorageZoneCommand(ZoneId, new CellId(2, 2), tick: 4));

        Assert.Equal(StoragePlacementErrors.CellMustBeOpen, result.Error);
        Assert.Equal(version, harness.Storage.Version);
        Assert.Equal(new CellId(1, 1), harness.Storage.GetZone(ZoneId)!.Cell);
    }

    [Fact]
    public void Occupied_zone_cannot_be_moved()
    {
        Harness harness = CreateHarness(fill: Air);
        Assert.True(harness.Inventory.AddStack(
            StackId,
            Stone,
            quantity: 3,
            ItemLocation.InStorage(ZoneId),
            tick: 1).IsSuccess);

        Result result = harness.CreateHandler().Handle(
            new MoveStorageZoneCommand(ZoneId, new CellId(3, 2), tick: 4));

        Assert.Equal(StoragePlacementErrors.ZoneOccupied, result.Error);
        Assert.Equal(new CellId(1, 1), harness.Storage.GetZone(ZoneId)!.Cell);
    }

    [Fact]
    public void Incoming_reservation_blocks_relocation()
    {
        Harness harness = CreateHarness(fill: Air);
        ItemDefinition item = harness.Inventory.Catalog.Get(Stone);
        Assert.True(harness.Storage.ReserveIncoming(
            ZoneId,
            JobId,
            item,
            quantity: 2,
            occupiedQuantity: 0,
            tick: 2).IsSuccess);

        Result result = harness.CreateHandler().Handle(
            new MoveStorageZoneCommand(ZoneId, new CellId(3, 2), tick: 4));

        Assert.Equal(StorageErrors.ZoneHasIncomingReservations, result.Error);
        Assert.Equal(new CellId(1, 1), harness.Storage.GetZone(ZoneId)!.Cell);
    }

    private static Harness CreateHarness(MaterialId fill)
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
            new MaterialDefinition(Rock, isSolid: true, hardness: 100),
        });
        WorldState world = WorldState.CreateFilled(
            new WorldSize(6, 6),
            chunkSize: 3,
            materials,
            fill,
            explored: true).Value;
        ItemCatalog items = new ItemCatalog(new[]
        {
            new ItemDefinition(
                Stone,
                "Stone",
                maximumStackSize: 100,
                isTool: false,
                new[] { new ItemCategoryId("raw.stone") }),
        });
        InventoryState inventory = new InventoryState(items);
        StorageState storage = new StorageState();
        Assert.True(storage.AddZone(new StorageZoneDefinition(
            ZoneId,
            "Stone stockpile",
            priority: 900,
            capacity: 100,
            new StorageFilter(
                acceptsAll: false,
                allowedItems: new[] { Stone }),
            new CellId(1, 1))).IsSuccess);
        return new Harness(world, inventory, storage);
    }

    private sealed class Harness
    {
        public Harness(
            WorldState world,
            InventoryState inventory,
            StorageState storage)
        {
            Inventory = inventory;
            Storage = storage;
            WorldRepository = new InMemoryWorldRepository(world);
            InventoryRepository = new InMemoryInventoryRepository(inventory);
            StorageRepository = new InMemoryStorageRepository(storage);
            Journal = new InMemoryExecutionJournal();
        }

        public InventoryState Inventory { get; }

        public StorageState Storage { get; }

        public InMemoryWorldRepository WorldRepository { get; }

        public InMemoryInventoryRepository InventoryRepository { get; }

        public InMemoryStorageRepository StorageRepository { get; }

        public InMemoryExecutionJournal Journal { get; }

        public MoveStorageZoneHandler CreateHandler()
        {
            return new MoveStorageZoneHandler(
                StorageRepository,
                InventoryRepository,
                WorldRepository,
                Journal);
        }
    }
}
