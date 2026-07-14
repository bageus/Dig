using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Storage;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class HaulingPlanningQueryTests
{
    private static readonly ItemId Ore = new ItemId("ore.alpha");
    private static readonly ItemId Meal = new ItemId("meal.alpha");
    private static readonly ItemCategoryId Raw = new ItemCategoryId("raw");
    private static readonly ItemCategoryId Food = new ItemCategoryId("food");

    [Fact]
    public void Available_world_stacks_exclude_storage_and_use_stable_order()
    {
        InventoryState inventory = CreateInventory();
        EntityId first = Id("71000000000000000000000000000001");
        EntityId second = Id("71000000000000000000000000000002");
        EntityId later = Id("71000000000000000000000000000003");
        EntityId stored = Id("71000000000000000000000000000004");
        EntityId storageId = Id("72000000000000000000000000000001");
        Assert.True(inventory.AddStack(
            later,
            Ore,
            2,
            ItemLocation.InWorld(new CellId(4, 4)),
            0).IsSuccess);
        Assert.True(inventory.AddStack(
            second,
            Ore,
            2,
            ItemLocation.InWorld(new CellId(1, 1)),
            0).IsSuccess);
        Assert.True(inventory.AddStack(
            first,
            Ore,
            2,
            ItemLocation.InWorld(new CellId(1, 1)),
            0).IsSuccess);
        Assert.True(inventory.AddStack(
            stored,
            Ore,
            9,
            ItemLocation.InStorage(storageId),
            0).IsSuccess);

        IReadOnlyList<ItemStackSnapshot> result = inventory.GetAvailableWorldStacks();

        Assert.Collection(
            result,
            value => Assert.Equal(first, value.StackId),
            value => Assert.Equal(second, value.StackId),
            value => Assert.Equal(later, value.StackId));
        Assert.Equal(9, inventory.GetTotalQuantityAt(ItemLocation.InStorage(storageId)));
    }

    [Fact]
    public void First_destination_honors_filter_priority_and_total_occupancy()
    {
        InventoryState inventory = CreateInventory();
        StorageState storage = new StorageState();
        EntityId high = Id("72000000000000000000000000000001");
        EntityId low = Id("72000000000000000000000000000002");
        Assert.True(storage.AddZone(new StorageZoneDefinition(
            high,
            "Raw high",
            priority: 900,
            capacity: 8,
            new StorageFilter(
                acceptsAll: false,
                allowedCategories: new[] { Raw }))).IsSuccess);
        Assert.True(storage.AddZone(new StorageZoneDefinition(
            low,
            "All low",
            priority: 100,
            capacity: 20,
            StorageFilter.All())).IsSuccess);
        Assert.True(inventory.AddStack(
            Id("71000000000000000000000000000005"),
            Ore,
            4,
            ItemLocation.InStorage(high),
            0).IsSuccess);
        Assert.True(inventory.AddStack(
            Id("71000000000000000000000000000006"),
            Meal,
            3,
            ItemLocation.InStorage(high),
            0).IsSuccess);

        StorageZoneSnapshot? oreDestination = storage.FindFirstDestination(
            inventory.Catalog.Get(Ore),
            1,
            zoneId => inventory.GetTotalQuantityAt(ItemLocation.InStorage(zoneId)));
        StorageZoneSnapshot? mealDestination = storage.FindFirstDestination(
            inventory.Catalog.Get(Meal),
            1,
            zoneId => inventory.GetTotalQuantityAt(ItemLocation.InStorage(zoneId)));

        Assert.NotNull(oreDestination);
        Assert.Equal(high, oreDestination!.Definition.Id);
        Assert.Equal(7, oreDestination.OccupiedQuantity);
        Assert.Equal(1, oreDestination.AvailableCapacity);
        Assert.NotNull(mealDestination);
        Assert.Equal(low, mealDestination!.Definition.Id);
    }

    [Fact]
    public void Repeated_planning_passes_do_not_duplicate_reserved_quantity()
    {
        InventoryState inventory = CreateInventory();
        StorageState storage = new StorageState();
        JobSystem jobs = new JobSystem();
        EntityId stackId = Id("71000000000000000000000000000007");
        EntityId storageId = Id("72000000000000000000000000000003");
        EntityId jobId = Id("73000000000000000000000000000001");
        Assert.True(inventory.AddStack(
            stackId,
            Ore,
            5,
            ItemLocation.InWorld(new CellId(2, 2)),
            0).IsSuccess);
        Assert.True(storage.AddZone(new StorageZoneDefinition(
            storageId,
            "Stockpile",
            500,
            10,
            StorageFilter.All())).IsSuccess);
        InMemoryInventoryRepository inventoryRepository = new InMemoryInventoryRepository(inventory);
        InMemoryStorageRepository storageRepository = new InMemoryStorageRepository(storage);
        InMemoryJobRepository jobRepository = new InMemoryJobRepository(jobs);
        PlanHaulingHandler planner = new PlanHaulingHandler(
            inventoryRepository,
            storageRepository,
            jobRepository,
            new FixedJobIdSource(jobId),
            new InMemoryExecutionJournal());

        HaulingPlanningReport first = planner.Handle(new PlanHaulingCommand(4, 500, 1));
        HaulingPlanningReport second = planner.Handle(new PlanHaulingCommand(4, 500, 2));

        Assert.Single(first.Created);
        Assert.Empty(second.Created);
        Assert.Empty(second.Skipped);
        Assert.Equal(5, inventory.GetStack(stackId)!.ReservedQuantity);
        Assert.Single(storage.GetReservations());
        Assert.Single(jobs.GetAll());
    }

    private static InventoryState CreateInventory()
    {
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(Ore, "Ore", 100, false, new[] { Raw }),
            new ItemDefinition(Meal, "Meal", 100, false, new[] { Food }),
        }));
    }

    private static EntityId Id(string value)
    {
        return EntityId.Parse(value);
    }

    private sealed class FixedJobIdSource : IHaulingJobIdSource
    {
        private readonly EntityId _jobId;
        private bool _used;

        public FixedJobIdSource(EntityId jobId)
        {
            _jobId = jobId;
        }

        public EntityId Next()
        {
            if (_used)
            {
                throw new InvalidOperationException("The test requested more than one hauling job id.");
            }

            _used = true;
            return _jobId;
        }
    }
}
}
