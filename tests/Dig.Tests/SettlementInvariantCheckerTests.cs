using Dig.Application.Diagnostics;
using Dig.Application.Inventory;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Storage;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests;

public sealed class SettlementInvariantCheckerTests
{
    [Fact]
    public void Checker_accepts_valid_hauling_and_reports_broken_item_reservation()
    {
        ItemId ore = new ItemId("quality.ore");
        ItemCatalog catalog = new ItemCatalog(new[]
        {
            new ItemDefinition(ore, "Quality ore", 100, isTool: false),
        });
        InventoryState inventory = new InventoryState(catalog);
        EntityId stackId = EntityId.Parse("b1000000000000000000000000000001");
        Assert.True(inventory.AddStack(
            stackId,
            ore,
            quantity: 3,
            ItemLocation.InWorld(new CellId(1, 1)),
            tick: 0).IsSuccess);
        StorageState storage = new StorageState();
        EntityId storageId = EntityId.Parse("b2000000000000000000000000000001");
        Assert.True(storage.AddZone(new StorageZoneDefinition(
            storageId,
            "Quality storage",
            priority: 500,
            capacity: 10,
            StorageFilter.All())).IsSuccess);
        InMemoryInventoryRepository inventoryRepository =
            new InMemoryInventoryRepository(inventory);
        InMemoryStorageRepository storageRepository =
            new InMemoryStorageRepository(storage);
        InMemoryJobRepository jobRepository = new InMemoryJobRepository();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        EntityId jobId = EntityId.Parse("b3000000000000000000000000000001");
        Assert.True(new CreateHaulingJobHandler(
            inventoryRepository,
            storageRepository,
            jobRepository,
            journal).Handle(new CreateHaulingJobCommand(
                jobId,
                stackId,
                quantity: 3,
                storageId,
                priority: 500,
                tick: 1)).IsSuccess);
        SettlementInvariantChecker checker = new SettlementInvariantChecker(
            new InMemoryAgentRepository(),
            inventoryRepository,
            storageRepository,
            jobRepository,
            new InMemoryBuildingFacilitiesRepository());

        Assert.True(checker.Check(tick: 1).IsValid);

        inventory.ReleaseReservations(jobId, tick: 2);
        SimulationInvariantReport broken = checker.Check(tick: 2);

        Assert.False(broken.IsValid);
        Assert.Contains(
            broken.Violations,
            value => value.Code == "hauling.item_reservation_mismatch"
                && value.EntityId == jobId);
    }
}
