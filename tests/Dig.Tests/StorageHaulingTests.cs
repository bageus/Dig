using System.Collections.Generic;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Storage;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class StorageHaulingTests
{
    private static readonly ItemId Ore = new ItemId("ore.iron");
    private static readonly ItemId Meal = new ItemId("meal.stew");
    private static readonly EntityId SourceStackId =
        EntityId.Parse("61000000000000000000000000000001");
    private static readonly EntityId RawStorageId =
        EntityId.Parse("62000000000000000000000000000001");
    private static readonly EntityId OtherStorageId =
        EntityId.Parse("62000000000000000000000000000002");
    private static readonly EntityId JobId =
        EntityId.Parse("63000000000000000000000000000001");
    private static readonly EntityId WorkerId =
        EntityId.Parse("64000000000000000000000000000001");

    [Fact]
    public void Storage_filters_and_priority_are_owned_by_storage()
    {
        HaulingHarness harness = CreateHarness();
        Assert.True(harness.Storage.AddZone(new StorageZoneDefinition(
            RawStorageId,
            "Raw high priority",
            priority: 900,
            capacity: 20,
            new StorageFilter(
                acceptsAll: false,
                allowedCategories: new[] { new ItemCategoryId("raw") }))).IsSuccess);
        Assert.True(harness.Storage.AddZone(new StorageZoneDefinition(
            OtherStorageId,
            "Everything low priority",
            priority: 100,
            capacity: 20,
            StorageFilter.All())).IsSuccess);
        FindStorageDestinationsHandler query = new FindStorageDestinationsHandler(
            harness.InventoryRepository,
            harness.StorageRepository);

        IReadOnlyList<StorageZoneSnapshot> oreDestinations = query.Handle(
            new FindStorageDestinationsQuery(Ore, quantity: 5));
        IReadOnlyList<StorageZoneSnapshot> mealDestinations = query.Handle(
            new FindStorageDestinationsQuery(Meal, quantity: 5));

        Assert.Equal(RawStorageId, oreDestinations[0].Definition.Id);
        Assert.DoesNotContain(
            mealDestinations,
            destination => destination.Definition.Id == RawStorageId);
        Assert.Equal(OtherStorageId, Assert.Single(mealDestinations).Definition.Id);
    }

    [Fact]
    public void Creating_haul_reserves_exact_items_and_destination_capacity()
    {
        HaulingHarness harness = CreateHarness(addDefaultStorage: true);
        CreateHaulingJobHandler create = harness.CreateHandler();

        Assert.True(create.Handle(new CreateHaulingJobCommand(
            JobId,
            SourceStackId,
            quantity: 6,
            RawStorageId,
            priority: 500,
            tick: 1)).IsSuccess);

        Assert.Equal(6, harness.Inventory.GetStack(SourceStackId)!.ReservedQuantity);
        StorageReservationSnapshot reservation = harness.Storage.GetReservation(JobId)!.Value;
        Assert.Equal(6, reservation.Quantity);
        Assert.Equal(JobStatus.Available, harness.Jobs.Get(JobId)!.Status);
    }

    [Fact]
    public void Two_jobs_cannot_reserve_more_units_than_the_stack_contains()
    {
        HaulingHarness harness = CreateHarness();
        harness.AddAcceptAllStorage(RawStorageId, capacity: 20);
        harness.AddAcceptAllStorage(OtherStorageId, capacity: 20);
        CreateHaulingJobHandler create = harness.CreateHandler();
        EntityId secondJobId = EntityId.Parse("63000000000000000000000000000002");

        Assert.True(create.Handle(new CreateHaulingJobCommand(
            JobId,
            SourceStackId,
            quantity: 7,
            RawStorageId,
            priority: 500,
            tick: 1)).IsSuccess);
        Result second = create.Handle(new CreateHaulingJobCommand(
            secondJobId,
            SourceStackId,
            quantity: 4,
            OtherStorageId,
            priority: 400,
            tick: 1));

        Assert.Equal(InventoryErrors.InsufficientAvailableQuantity, second.Error);
        Assert.Null(harness.Storage.GetReservation(secondJobId));
        Assert.Equal(7, harness.Inventory.GetStack(SourceStackId)!.ReservedQuantity);
    }

    [Fact]
    public void Cancelling_haul_releases_item_and_destination_reservations()
    {
        HaulingHarness harness = CreateHarness(addDefaultStorage: true);
        Assert.True(harness.CreateHandler().Handle(new CreateHaulingJobCommand(
            JobId,
            SourceStackId,
            quantity: 6,
            RawStorageId,
            priority: 500,
            tick: 1)).IsSuccess);
        CancelHaulingJobHandler cancel = new CancelHaulingJobHandler(
            harness.InventoryRepository,
            harness.StorageRepository,
            harness.JobRepository,
            harness.Journal);

        Assert.True(cancel.Handle(new CancelHaulingJobCommand(
            JobId,
            "Player changed priorities.",
            tick: 2)).IsSuccess);

        Assert.Equal(0, harness.Inventory.GetStack(SourceStackId)!.ReservedQuantity);
        Assert.Null(harness.Storage.GetReservation(JobId));
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(JobId)!.Status);
    }

    [Fact]
    public void Completed_haul_moves_exact_quantity_and_preserves_total()
    {
        HaulingHarness harness = CreateHarness(addDefaultStorage: true);
        Assert.True(harness.CreateHandler().Handle(new CreateHaulingJobCommand(
            JobId,
            SourceStackId,
            quantity: 6,
            RawStorageId,
            priority: 500,
            tick: 1)).IsSuccess);
        Assert.True(harness.Jobs.Claim(JobId, WorkerId, tick: 2).IsSuccess);
        Assert.True(harness.Jobs.Start(JobId, tick: 3).IsSuccess);
        Assert.Equal(JobStageKind.AcquireItem, harness.Jobs.Get(JobId)!.Stage);
        Assert.True(harness.Jobs.AdvanceStage(JobId, tick: 4).IsSuccess);
        Assert.Equal(JobStageKind.TravelToDestination, harness.Jobs.Get(JobId)!.Stage);
        Assert.True(harness.Jobs.AdvanceStage(JobId, tick: 5).IsSuccess);
        Assert.Equal(JobStageKind.DepositItem, harness.Jobs.Get(JobId)!.Stage);
        EntityId movedStackId = EntityId.Parse("65000000000000000000000000000001");
        CompleteHaulingJobHandler complete = new CompleteHaulingJobHandler(
            harness.InventoryRepository,
            harness.StorageRepository,
            harness.JobRepository,
            harness.Journal);

        Assert.True(complete.Handle(new CompleteHaulingJobCommand(
            JobId,
            movedStackId,
            tick: 6)).IsSuccess);

        Assert.Equal(10, harness.Inventory.GetTotal(Ore));
        Assert.Equal(4, harness.Inventory.GetStack(SourceStackId)!.Quantity);
        Assert.Equal(6, harness.Inventory.GetQuantityAt(
            Ore,
            ItemLocation.InStorage(RawStorageId)));
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(JobId)!.Status);
        Assert.Empty(harness.Jobs.GetReservations());
        Assert.Empty(harness.Storage.GetReservations());
    }

    private static HaulingHarness CreateHarness(bool addDefaultStorage = false)
    {
        ItemCategoryId raw = new ItemCategoryId("raw");
        ItemCategoryId food = new ItemCategoryId("food");
        ItemCatalog catalog = new ItemCatalog(new[]
        {
            new ItemDefinition(Ore, "Iron ore", 100, isTool: false, new[] { raw }),
            new ItemDefinition(Meal, "Stew", 20, isTool: false, new[] { food }),
        });
        InventoryState inventory = new InventoryState(catalog);
        Assert.True(inventory.AddStack(
            SourceStackId,
            Ore,
            quantity: 10,
            ItemLocation.InWorld(new CellId(2, 2)),
            tick: 0).IsSuccess);
        HaulingHarness harness = new HaulingHarness(inventory);
        if (addDefaultStorage)
        {
            harness.AddAcceptAllStorage(RawStorageId, capacity: 10);
        }

        return harness;
    }

    private sealed class HaulingHarness
    {
        public HaulingHarness(InventoryState inventory)
        {
            Inventory = inventory;
            Storage = new StorageState();
            Jobs = new JobSystem();
            InventoryRepository = new InMemoryInventoryRepository(Inventory);
            StorageRepository = new InMemoryStorageRepository(Storage);
            JobRepository = new InMemoryJobRepository(Jobs);
            Journal = new InMemoryExecutionJournal();
        }

        public InventoryState Inventory { get; }

        public StorageState Storage { get; }

        public JobSystem Jobs { get; }

        public InMemoryInventoryRepository InventoryRepository { get; }

        public InMemoryStorageRepository StorageRepository { get; }

        public InMemoryJobRepository JobRepository { get; }

        public InMemoryExecutionJournal Journal { get; }

        public void AddAcceptAllStorage(EntityId storageId, int capacity)
        {
            Assert.True(Storage.AddZone(new StorageZoneDefinition(
                storageId,
                storageId.ToString(),
                priority: 500,
                capacity,
                StorageFilter.All())).IsSuccess);
        }

        public CreateHaulingJobHandler CreateHandler()
        {
            return new CreateHaulingJobHandler(
                InventoryRepository,
                StorageRepository,
                JobRepository,
                Journal);
        }
    }
}
}
