using System.Diagnostics;
using Dig.Application.Agents;
using Dig.Application.Diagnostics;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Runtime;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Runtime;
using Dig.Domain.Storage;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Headless.Soak;

internal static class HeadlessSoakScenario
{
    private static readonly ItemId FoodItemId = new ItemId("soak.food");
    private static readonly ItemId OreItemId = new ItemId("soak.ore");
    private const int InitialFoodQuantity = 5_000;
    private const int DrainTicks = 20;

    public static HeadlessSoakReport Execute(HeadlessSoakConfiguration configuration)
    {
        SimulationState state = SimulationState.Create(
            unchecked((ulong)(uint)configuration.Seed),
            TimeSpan.FromMilliseconds(100));
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal(
            maximumCommands: 1_000,
            maximumEvents: 5_000);
        InMemorySimulationPerformance performance = new InMemorySimulationPerformance();

        EntityId storageId = Register(state);
        StorageState storage = new StorageState();
        Require(storage.AddZone(new StorageZoneDefinition(
            storageId,
            "Soak stockpile",
            priority: 500,
            capacity: 100_000,
            StorageFilter.All())));
        InMemoryStorageRepository storageRepository = new InMemoryStorageRepository(storage);

        InventoryState inventory = CreateInventory(state, storageId);
        InMemoryInventoryRepository inventoryRepository =
            new InMemoryInventoryRepository(inventory);
        InMemoryAgentRepository agentRepository = CreateAgents(
            state,
            configuration.ResidentCount);
        InMemoryBuildingFacilitiesRepository facilitiesRepository = CreateFacilities(
            state,
            configuration.ResidentCount);
        InMemoryJobRepository jobRepository = new InMemoryJobRepository();

        EntityId[] haulingWorkers = Enumerable.Range(0, Math.Min(4, configuration.ResidentCount))
            .Select(_ => Register(state))
            .ToArray();
        ResidentSettlementSystem settlement = new ResidentSettlementSystem(
            agentRepository,
            new InMemoryAgentDecisionContextProvider(AgentDecisionContext.AllAvailable()),
            inventoryRepository,
            facilitiesRepository,
            journal,
            new AgentDecisionSystem(),
            AgentBehaviorPolicy.CreateDefault());
        SoakResourceSpawnerSystem spawner = new SoakResourceSpawnerSystem(
            inventoryRepository,
            journal,
            OreItemId,
            stopAfterTick: configuration.TickCount);
        SoakHaulingSystem hauling = new SoakHaulingSystem(
            inventoryRepository,
            storageRepository,
            jobRepository,
            journal,
            state,
            haulingWorkers);
        SettlementInvariantChecker checker = new SettlementInvariantChecker(
            agentRepository,
            inventoryRepository,
            storageRepository,
            jobRepository,
            facilitiesRepository);
        SoakInvariantSystem invariants = new SoakInvariantSystem(checker);

        SimulationScheduler scheduler = new SimulationScheduler(
            performance: performance);
        scheduler.Register(spawner);
        scheduler.Register(settlement);
        scheduler.Register(hauling);
        scheduler.Register(invariants);
        SimulationRunner runner = new SimulationRunner(state, scheduler);

        Stopwatch elapsed = Stopwatch.StartNew();
        runner.Step(configuration.TickCount);
        runner.Step(DrainTicks);
        elapsed.Stop();

        SimulationPerformanceBudget budget = new SimulationPerformanceBudget(
            maximumAverageMicroseconds: 10_000,
            maximumAverageAllocatedBytes: 2_000_000,
            maximumSingleExecutionMilliseconds: 500);
        SimulationPerformanceReport performanceReport = performance.CreateReport(budget);
        SimulationInvariantReport invariantReport = checker.Check(state.Clock.TickIndex);
        JobSnapshot[] jobs = jobRepository.Get().GetAll().ToArray();
        int activeHauling = jobs.Count(value =>
            value.Definition is HaulJobDefinition && !value.IsTerminal);
        int totalOre = inventory.GetTotal(OreItemId);
        int storedOre = inventory.GetQuantityAt(
            OreItemId,
            ItemLocation.InStorage(storageId));
        List<string> endStateViolations = CreateEndStateViolations(
            state,
            agentRepository,
            jobRepository,
            storageRepository,
            spawner.SpawnedQuantity,
            totalOre,
            storedOre,
            activeHauling);
        List<string> budgetViolations = performanceReport.Violations
            .Select(value => $"{value.SystemName}:{value.Metric}:{value.Detail}")
            .ToList();
        if (elapsed.Elapsed.TotalSeconds > configuration.MaximumElapsedSeconds)
        {
            budgetViolations.Add(
                $"global:elapsed:{elapsed.Elapsed.TotalSeconds:F3}s exceeds "
                + $"{configuration.MaximumElapsedSeconds:F3}s");
        }

        string stateHash = HeadlessSoakStateHasher.Compute(
            state.Clock.TickIndex,
            state.Entities.Count,
            agentRepository,
            inventoryRepository,
            storageRepository,
            jobRepository,
            facilitiesRepository);
        string[] invariantViolations = invariantReport.Violations
            .Select(value => $"{value.Code}:{value.EntityId}:{value.Detail}")
            .Concat(endStateViolations)
            .ToArray();
        bool succeeded = invariantViolations.Length == 0
            && budgetViolations.Count == 0;
        return new HeadlessSoakReport
        {
            Seed = configuration.Seed,
            RequestedTicks = configuration.TickCount,
            FinalTick = state.Clock.TickIndex,
            ResidentCount = configuration.ResidentCount,
            EntityCount = state.Entities.Count,
            SpawnedOre = spawner.SpawnedQuantity,
            TotalOre = totalOre,
            StoredOre = storedOre,
            RemainingFood = inventory.GetTotal(FoodItemId),
            CompletedHaulingJobs = hauling.CompletedJobCount,
            ActiveHaulingJobs = activeHauling,
            JobReservationCount = jobRepository.Get().GetReservations().Count,
            StorageReservationCount = storageRepository.Get().GetReservations().Count,
            FacilityReservationCount = facilitiesRepository.Get().GetReservations().Count,
            RetainedEventCount = journal.Events.Count,
            DroppedEventCount = journal.DroppedEventCount,
            ElapsedMilliseconds = elapsed.Elapsed.TotalMilliseconds,
            MaximumElapsedMilliseconds = configuration.MaximumElapsedSeconds * 1000,
            StateHash = stateHash,
            Systems = performanceReport.Systems
                .Select(value => new HeadlessSoakSystemReport(value))
                .ToArray(),
            BudgetViolations = budgetViolations,
            InvariantViolations = invariantViolations,
            DeterministicReplayMatched = false,
            Succeeded = succeeded,
        };
    }

    private static InventoryState CreateInventory(
        SimulationState state,
        EntityId storageId)
    {
        ItemCatalog catalog = new ItemCatalog(new[]
        {
            new ItemDefinition(
                FoodItemId,
                "Soak meal",
                maximumStackSize: 10_000,
                isTool: false,
                new[] { new ItemCategoryId("food") }),
            new ItemDefinition(
                OreItemId,
                "Soak ore",
                maximumStackSize: 100,
                isTool: false,
                new[] { new ItemCategoryId("raw") }),
        });
        InventoryState inventory = new InventoryState(catalog);
        Require(inventory.AddStack(
            Register(state),
            FoodItemId,
            InitialFoodQuantity,
            ItemLocation.InStorage(storageId),
            tick: 0));
        return inventory;
    }

    private static InMemoryAgentRepository CreateAgents(
        SimulationState state,
        int count)
    {
        InMemoryAgentRepository repository = new InMemoryAgentRepository();
        for (int index = 0; index < count; index++)
        {
            EntityId id = Register(state);
            AgentState agent = new AgentState(
                id,
                $"Soak Resident {index + 1}",
                new AgentNeedsSnapshot(
                    new NeedValue(5_000 + (index * 25)),
                    new NeedValue(5_500 - (index * 25)),
                    new NeedValue(4_500),
                    new NeedValue(10_000)),
                DailySchedule.CreateBalanced(ticksPerDay: 24),
                new[]
                {
                    new AgentSkillValue(new AgentSkillId("general.work"), 4_000 + index),
                });
            Require(repository.Add(agent));
        }

        return repository;
    }

    private static InMemoryBuildingFacilitiesRepository CreateFacilities(
        SimulationState state,
        int residentCount)
    {
        BuildingFacilitiesState facilities = new BuildingFacilitiesState();
        EntityId buildingId = Register(state);
        for (int index = 0; index < residentCount; index++)
        {
            Require(facilities.Add(new BuildingFacilityDefinition(
                Register(state),
                buildingId,
                BuildingFacilityKind.Bed,
                new CellId(index, 10))));
            Require(facilities.Add(new BuildingFacilityDefinition(
                Register(state),
                buildingId,
                BuildingFacilityKind.Leisure,
                new CellId(index, 11))));
        }

        return new InMemoryBuildingFacilitiesRepository(facilities);
    }

    private static List<string> CreateEndStateViolations(
        SimulationState state,
        IAgentRepository agents,
        IJobRepository jobs,
        IStorageRepository storage,
        int spawnedOre,
        int totalOre,
        int storedOre,
        int activeHauling)
    {
        List<string> violations = new List<string>();
        if (spawnedOre != totalOre || totalOre != storedOre)
        {
            violations.Add(
                $"ore_conservation:spawned={spawnedOre},total={totalOre},stored={storedOre}");
        }

        if (activeHauling != 0
            || jobs.Get().GetReservations().Count != 0
            || storage.Get().GetReservations().Count != 0)
        {
            violations.Add("hauling_not_drained:external or job reservations remain");
        }

        if (agents.GetAll().Any(value => !value.IsAlive))
        {
            violations.Add("resident_death:a soak resident died");
        }

        if (state.Clock.TickIndex <= 0)
        {
            violations.Add("clock_not_advanced:simulation tick did not advance");
        }

        return violations;
    }

    private static EntityId Register(SimulationState state)
    {
        Result<EntityId> id = state.Entities.RegisterNew();
        if (id.IsFailure)
        {
            throw new InvalidOperationException(id.Error!.ToString());
        }

        return id.Value;
    }

    private static void Require(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.ToString());
        }
    }
}
