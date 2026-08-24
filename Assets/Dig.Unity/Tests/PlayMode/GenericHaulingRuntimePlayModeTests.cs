using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Storage;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class GenericHaulingRuntimePlayModeTests
{
    [Test]
    public void Generic_hauling_transfers_world_item_through_resident_into_storage()
    {
        ResidentNeedsRuntimePlayModeHarness.Runtime runtime =
            ResidentNeedsRuntimePlayModeHarness.CreateRuntime();
        AgentViewModel resident = runtime.Residents.LoadView().First();
        CellId cell = new CellId(resident.CellX, resident.CellY, resident.CellZ);
        EntityId stackId = Id(1);
        EntityId storageId = Id(2);
        InMemoryInventoryRepository inventory =
            ResidentNeedsRuntimePlayModeHarness.GetField<InMemoryInventoryRepository>(
                runtime.Terrain,
                "_inventoryRepository");
        InMemoryStorageRepository storage =
            ResidentNeedsRuntimePlayModeHarness.GetField<InMemoryStorageRepository>(
                runtime.Terrain,
                "_storageRepository");
        InMemoryJobRepository jobs =
            ResidentNeedsRuntimePlayModeHarness.GetField<InMemoryJobRepository>(
                runtime.Terrain,
                "_jobRepository");
        InventoryState inventoryState = inventory.Get();
        ItemId itemId = inventoryState.Catalog.Definitions
            .Where(definition => !definition.IsInventoryExpansion)
            .Select(definition => definition.Id)
            .First(candidate => inventoryState.GetAvailableWorldStacks()
                .All(stack => stack.ItemId != candidate));
        Assert.That(inventoryState.AddStack(
            stackId,
            itemId,
            quantity: 1,
            ItemLocation.InWorld(cell),
            tick: 0).IsSuccess, Is.True);
        inventory.Save(inventoryState);
        StorageState storageState = storage.Get();
        Assert.That(storageState.AddZone(new StorageZoneDefinition(
            storageId,
            "Runtime storage",
            priority: 500,
            capacity: 10,
            filter: new StorageFilter(
                acceptsAll: false,
                allowedItems: new[] { itemId }),
            cell: cell)).IsSuccess, Is.True);
        storage.Save(storageState);
        AgentViewModel[] agents = runtime.Residents.LoadView().ToArray();

        Assert.That(
            runtime.Terrain.SynchronizeGenericHauling(agents, tick: 1).IsSuccess,
            Is.True);
        JobSnapshot hauling = jobs.Get().GetAll().Single(job =>
            job.Definition is HaulJobDefinition definition
                && definition.SourceStackId == stackId
                && !job.IsTerminal);

        Assert.That(
            runtime.Terrain.AdvanceGenericHauling(tick: 1, agents).IsSuccess,
            Is.True);
        Assert.That(
            jobs.Get().Get(hauling.Id)!.Stage,
            Is.EqualTo(JobStageKind.TravelToDestination));
        Assert.That(
            runtime.Terrain.AdvanceGenericHauling(tick: 2, agents).IsSuccess,
            Is.True);
        Assert.That(
            jobs.Get().Get(hauling.Id)!.Stage,
            Is.EqualTo(JobStageKind.DepositItem));
        Assert.That(
            runtime.Terrain.AdvanceGenericHauling(tick: 3, agents).IsSuccess,
            Is.True);

        Assert.That(jobs.Get().Get(hauling.Id)!.Status, Is.EqualTo(JobStatus.Completed));
        Assert.That(
            inventory.Get().GetQuantityAt(
                itemId,
                ItemLocation.InStorage(storageId)),
            Is.EqualTo(1));
        Assert.That(inventory.Get().GetStack(stackId)!.Location.Kind,
            Is.EqualTo(ItemLocationKind.Storage));
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
