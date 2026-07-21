using System;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Runtime;
using Dig.Domain.Storage;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Headless
{

internal static class HeadlessAutomaticHaulingScenario
{
    public static int Run(SimulationState state, IEventSink events, long startTick)
    {
        ItemId ore = new ItemId("headless.auto_ore");
        ItemCatalog catalog = new ItemCatalog(new[]
        {
            new ItemDefinition(
                ore,
                "Automatic hauling ore",
                maximumStackSize: 100,
                isTool: false,
                new[] { new ItemCategoryId("raw") }),
        });
        InventoryState inventory = new InventoryState(catalog);
        EntityId firstStack = Require(state.Entities.RegisterNew());
        EntityId secondStack = Require(state.Entities.RegisterNew());
        Require(inventory.AddStack(
            firstStack,
            ore,
            quantity: 3,
            location: ItemLocation.InWorld(new CellId(1, 4)),
            tick: startTick));
        Require(inventory.AddStack(
            secondStack,
            ore,
            quantity: 3,
            location: ItemLocation.InWorld(new CellId(2, 4)),
            tick: startTick));

        StorageState storage = new StorageState();
        EntityId highStorage = Require(state.Entities.RegisterNew());
        EntityId lowStorage = Require(state.Entities.RegisterNew());
        Require(storage.AddZone(new StorageZoneDefinition(
            highStorage,
            "Automatic high stockpile",
            priority: 900,
            capacity: 3,
            StorageFilter.All())));
        Require(storage.AddZone(new StorageZoneDefinition(
            lowStorage,
            "Automatic overflow stockpile",
            priority: 100,
            capacity: 3,
            StorageFilter.All())));

        InMemoryInventoryRepository inventoryRepository =
            new InMemoryInventoryRepository(inventory);
        InMemoryStorageRepository storageRepository =
            new InMemoryStorageRepository(storage);
        InMemoryJobRepository jobRepository = new InMemoryJobRepository();
        EntityId firstJob = Require(state.Entities.RegisterNew());
        EntityId secondJob = Require(state.Entities.RegisterNew());
        PlanHaulingHandler planner = new PlanHaulingHandler(
            inventoryRepository,
            storageRepository,
            jobRepository,
            new InMemoryHaulingJobIdSource(new[] { firstJob, secondJob }),
            events);
        HaulingPlanningReport planning = planner.Handle(new PlanHaulingCommand(
            maximumJobs: 2,
            priority: 600,
            tick: startTick + 1));
        if (planning.Created.Count != 2)
        {
            throw new InvalidOperationException("Automatic hauling did not create two jobs.");
        }

        EntityId firstWorker = Require(state.Entities.RegisterNew());
        EntityId secondWorker = Require(state.Entities.RegisterNew());
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        Require(agents.Add(CreateWorker(firstWorker)));
        Require(agents.Add(CreateWorker(secondWorker)));
        InMemoryJobCandidateProvider candidates = new InMemoryJobCandidateProvider();
        candidates.SetCandidates(firstJob, new[]
        {
            new JobCandidate(firstWorker, skillLevel: 5000, distanceCost: 1, isAvailable: true),
        });
        candidates.SetCandidates(secondJob, new[]
        {
            new JobCandidate(secondWorker, skillLevel: 5000, distanceCost: 1, isAvailable: true),
        });
        JobAssignmentReport assignment = new AssignAvailableJobsHandler(
            jobRepository,
            candidates,
            events).Handle(new AssignAvailableJobsCommand(startTick + 2));
        if (assignment.Assignments.Count != 2)
        {
            throw new InvalidOperationException("Automatic hauling jobs were not independently assigned.");
        }

        AdvanceJobHandler advance = new AdvanceJobHandler(jobRepository, events);
        CompleteHaulingJobHandler complete = new CompleteHaulingJobHandler(
            inventoryRepository,
            storageRepository,
            jobRepository,
            events,
            new AgentSkillGrantService(agents, events));
        long tick = startTick + 3;
        foreach (PlannedHaulingJob planned in planning.Created)
        {
            Require(advance.Handle(new AdvanceJobCommand(planned.JobId, tick++)));
            Require(advance.Handle(new AdvanceJobCommand(planned.JobId, tick++)));
            Require(advance.Handle(new AdvanceJobCommand(planned.JobId, tick++)));
            Require(complete.Handle(new CompleteHaulingJobCommand(
                planned.JobId,
                splitStackId: default,
                tick: tick++)));
        }

        int stored = inventory.GetQuantityAt(ore, ItemLocation.InStorage(highStorage))
            + inventory.GetQuantityAt(ore, ItemLocation.InStorage(lowStorage));
        if (inventory.GetTotal(ore) != 6
            || stored != 6
            || storage.GetReservations().Count != 0
            || jobRepository.Get().GetAll().Any(value => value.Status != JobStatus.Completed))
        {
            throw new InvalidOperationException(
                "Automatic hauling did not conserve and store all resources.");
        }

        return planning.Created.Count;
    }

    private static AgentState CreateWorker(EntityId id)
    {
        return new AgentState(
            id,
            "Automatic Hauler",
            new AgentNeedsSnapshot(
                new NeedValue(8_000),
                new NeedValue(8_000),
                new NeedValue(8_000),
                new NeedValue(10_000)),
            DailySchedule.CreateBalanced(ticksPerDay: 12),
            new[] { new AgentSkillValue(new AgentSkillId("general.work"), 4_000) });
    }

    private static T Require<T>(Result<T> result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.ToString());
        }

        return result.Value;
    }

    private static void Require(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.ToString());
        }
    }
}
}
