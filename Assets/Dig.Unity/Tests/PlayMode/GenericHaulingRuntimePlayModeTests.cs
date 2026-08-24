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

    [Test]
    public void Path_failure_before_pickup_reassigns_without_duplicate_or_item_loss()
    {
        Scenario scenario = CreateScenario();

        scenario.Runtime.Terrain.HandleGenericHaulingPathFailure(
            scenario.Job,
            tick: 2);

        JobSnapshot released = scenario.Jobs.Get().Get(scenario.Job.Id)!;
        Assert.That(released.Status, Is.EqualTo(JobStatus.Available));
        Assert.That(released.AssignedAgentId.HasValue, Is.False);
        Assert.That(
            scenario.Inventory.Get().GetStack(scenario.StackId)!.ReservedQuantity,
            Is.EqualTo(1));
        Assert.That(scenario.Storage.Get().GetReservation(scenario.Job.Id), Is.Not.Null);

        Assert.That(scenario.Runtime.Terrain.SynchronizeGenericHauling(
            scenario.Agents,
            tick: 3).IsSuccess, Is.True);
        JobSnapshot[] matching = scenario.Jobs.Get().GetAll().Where(job =>
            job.Definition is HaulJobDefinition definition
                && definition.SourceStackId == scenario.StackId
                && !job.IsTerminal).ToArray();
        Assert.That(matching, Has.Length.EqualTo(1));
        Assert.That(matching[0].Status, Is.EqualTo(JobStatus.Claimed));

        CompleteScenario(scenario, firstTick: 3);
    }

    [Test]
    public void Path_failure_after_pickup_keeps_carrier_and_resumes_deposit()
    {
        Scenario scenario = CreateScenario();
        Assert.That(scenario.Runtime.Terrain.AdvanceGenericHauling(
            tick: 1,
            scenario.Agents).IsSuccess, Is.True);
        JobSnapshot carrying = scenario.Jobs.Get().Get(scenario.Job.Id)!;
        Assert.That(carrying.Stage, Is.EqualTo(JobStageKind.TravelToDestination));
        EntityId carrier = carrying.AssignedAgentId!.Value;

        scenario.Runtime.Terrain.HandleGenericHaulingPathFailure(carrying, tick: 2);

        JobSnapshot retained = scenario.Jobs.Get().Get(scenario.Job.Id)!;
        Assert.That(retained.Status, Is.EqualTo(JobStatus.InProgress));
        Assert.That(retained.Stage, Is.EqualTo(JobStageKind.TravelToDestination));
        Assert.That(retained.AssignedAgentId, Is.EqualTo(carrier));
        ItemStackSnapshot carried = scenario.Inventory.Get().GetStack(
            scenario.StackId)!;
        Assert.That(carried.Location.Kind, Is.EqualTo(ItemLocationKind.AgentInventory));
        Assert.That(carried.Location.OwnerId, Is.EqualTo(carrier));
        Assert.That(carried.ReservedQuantity, Is.EqualTo(1));

        Assert.That(scenario.Runtime.Terrain.AdvanceGenericHauling(
            tick: 2,
            scenario.Agents).IsSuccess, Is.True);
        Assert.That(scenario.Runtime.Terrain.AdvanceGenericHauling(
            tick: 3,
            scenario.Agents).IsSuccess, Is.True);
        AssertScenarioCompleted(scenario);
    }

    private static Scenario CreateScenario()
    {
        ResidentNeedsRuntimePlayModeHarness.Runtime runtime =
            ResidentNeedsRuntimePlayModeHarness.CreateRuntime();
        AgentViewModel resident = runtime.Residents.LoadView().First();
        CellId cell = new CellId(resident.CellX, resident.CellY, resident.CellZ);
        EntityId stackId = Id(101);
        EntityId storageId = Id(102);
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
        InventoryState state = inventory.Get();
        ItemId itemId = state.Catalog.Definitions
            .Where(definition => !definition.IsInventoryExpansion)
            .Select(definition => definition.Id)
            .First(candidate => state.GetAvailableWorldStacks()
                .All(stack => stack.ItemId != candidate));
        int expectedTotal = checked(state.GetTotal(itemId) + 1);
        Assert.That(state.AddUnit(
            stackId,
            itemId,
            ItemLocation.InWorld(cell),
            tick: 0).IsSuccess, Is.True);
        inventory.Save(state);
        StorageState storageState = storage.Get();
        Assert.That(storageState.AddZone(new StorageZoneDefinition(
            storageId,
            "Path retry storage",
            priority: 500,
            capacity: 10,
            filter: new StorageFilter(
                acceptsAll: false,
                allowedItems: new[] { itemId }),
            cell: cell)).IsSuccess, Is.True);
        storage.Save(storageState);
        AgentViewModel[] agents = runtime.Residents.LoadView().ToArray();
        Assert.That(runtime.Terrain.SynchronizeGenericHauling(
            agents,
            tick: 1).IsSuccess, Is.True);
        JobSnapshot job = jobs.Get().GetAll().Single(candidate =>
            candidate.Definition is HaulJobDefinition definition
                && definition.SourceStackId == stackId
                && !candidate.IsTerminal);
        return new Scenario(
            runtime,
            agents,
            inventory,
            storage,
            jobs,
            itemId,
            stackId,
            storageId,
            job,
            expectedTotal);
    }

    private static void CompleteScenario(Scenario scenario, long firstTick)
    {
        for (long tick = firstTick; tick < firstTick + 3; tick++)
        {
            Assert.That(scenario.Runtime.Terrain.AdvanceGenericHauling(
                tick,
                scenario.Agents).IsSuccess, Is.True);
        }
        AssertScenarioCompleted(scenario);
    }

    private static void AssertScenarioCompleted(Scenario scenario)
    {
        Assert.That(
            scenario.Jobs.Get().Get(scenario.Job.Id)!.Status,
            Is.EqualTo(JobStatus.Completed));
        Assert.That(scenario.Inventory.Get().GetQuantityAt(
            scenario.ItemId,
            ItemLocation.InStorage(scenario.StorageId)), Is.EqualTo(1));
        Assert.That(
            scenario.Inventory.Get().GetTotal(scenario.ItemId),
            Is.EqualTo(scenario.ExpectedTotal));
        Assert.That(scenario.Storage.Get().GetReservation(scenario.Job.Id), Is.Null);
    }

    private readonly struct Scenario
    {
        public Scenario(
            ResidentNeedsRuntimePlayModeHarness.Runtime runtime,
            AgentViewModel[] agents,
            InMemoryInventoryRepository inventory,
            InMemoryStorageRepository storage,
            InMemoryJobRepository jobs,
            ItemId itemId,
            EntityId stackId,
            EntityId storageId,
            JobSnapshot job,
            int expectedTotal)
        {
            Runtime = runtime;
            Agents = agents;
            Inventory = inventory;
            Storage = storage;
            Jobs = jobs;
            ItemId = itemId;
            StackId = stackId;
            StorageId = storageId;
            Job = job;
            ExpectedTotal = expectedTotal;
        }

        public ResidentNeedsRuntimePlayModeHarness.Runtime Runtime { get; }
        public AgentViewModel[] Agents { get; }
        public InMemoryInventoryRepository Inventory { get; }
        public InMemoryStorageRepository Storage { get; }
        public InMemoryJobRepository Jobs { get; }
        public ItemId ItemId { get; }
        public EntityId StackId { get; }
        public EntityId StorageId { get; }
        public JobSnapshot Job { get; }
        public int ExpectedTotal { get; }
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
