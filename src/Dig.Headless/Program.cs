using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Application.Navigation;
using Dig.Application.Runtime;
using Dig.Application.World;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.Runtime;
using Dig.Domain.Storage;
using Dig.Domain.World;
using Dig.Headless.Soak;
using Dig.Infrastructure.InMemory;

namespace Dig.Headless
{

internal static class Program
{
    public static int Main(string[] args)
    {
        if (args.Any(value => string.Equals(value, "--soak", StringComparison.Ordinal)))
        {
            return HeadlessSoakCommand.Run(args);
        }

        SimulationState state = SimulationState.Create(
            worldSeed: 42,
            tickDuration: TimeSpan.FromMilliseconds(100));
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        EntityId residentId = Require(state.Entities.RegisterNew());
        AgentState resident = new AgentState(
            residentId,
            "Headless Dwarf",
            new AgentNeedsSnapshot(
                new NeedValue(1_000),
                new NeedValue(2_500),
                new NeedValue(6_000),
                new NeedValue(10_000)),
            DailySchedule.CreateBalanced(ticksPerDay: 12),
            new[]
            {
                new AgentSkillValue(new AgentSkillId("general.work"), 4_000),
            });
        InMemoryAgentRepository agentRepository = new InMemoryAgentRepository();
        Require(agentRepository.Add(resident));
        InMemoryAgentDecisionContextProvider agentContexts =
            new InMemoryAgentDecisionContextProvider(AgentDecisionContext.AllAvailable());
        AgentAutonomySystem autonomy = new AgentAutonomySystem(
            agentRepository,
            agentContexts,
            journal,
            new AgentDecisionSystem(),
            AgentBehaviorPolicy.CreateDefault());

        SimulationScheduler scheduler = new SimulationScheduler();
        scheduler.Register(new EntityCreationSystem(intervalTicks: 5));
        scheduler.Register(autonomy);
        SimulationRunner runner = new SimulationRunner(state, scheduler);
        runner.Step(20);

        AgentSnapshot residentSnapshot = resident.CreateSnapshot(state.Clock.TickIndex);
        if (!residentSnapshot.IsAlive || residentSnapshot.LastDecision is null)
        {
            throw new InvalidOperationException(
                "Headless resident did not complete autonomous decisions.");
        }

        MaterialId rock = new MaterialId("rock");
        MaterialId air = new MaterialId("air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(rock, isSolid: true, hardness: 100),
            new MaterialDefinition(air, isSolid: false, hardness: 0),
        });
        WorldState world = Require(WorldState.CreateFilled(
            new WorldSize(8, 8),
            chunkSize: 4,
            materials,
            rock,
            explored: true));
        InMemoryWorldRepository worldRepository = new InMemoryWorldRepository(world);
        CommandPipeline commands = new CommandPipeline(journal);
        CellId target = new CellId(3, 3);
        EntityId digJobId = Require(state.Entities.RegisterNew());
        InMemoryJobRepository jobRepository = new InMemoryJobRepository();
        InMemoryJobCandidateProvider jobCandidates = new InMemoryJobCandidateProvider();
        CreateDigJobHandler createJob = new CreateDigJobHandler(jobRepository, journal);
        AssignAvailableJobsHandler assignJobs = new AssignAvailableJobsHandler(
            jobRepository,
            jobCandidates,
            journal);
        AdvanceJobHandler advanceJob = new AdvanceJobHandler(jobRepository, journal);
        DigJobDefinition digJob = new DigJobDefinition(
            digJobId,
            new DigJobTarget(target),
            priority: 700,
            createdTick: 20,
            JobRetryPolicy.Default);
        Require(createJob.Handle(new CreateDigJobCommand(digJob, makeAvailable: true)));
        SetCandidate(jobCandidates, digJobId, residentId, residentSnapshot, distanceCost: 6);
        EnsureSingleAssignment(assignJobs.Handle(new AssignAvailableJobsCommand(tick: 20)));
        Require(advanceJob.Handle(new AdvanceJobCommand(digJobId, tick: 20)));
        Require(advanceJob.Handle(new AdvanceJobCommand(digJobId, tick: 21)));
        Require(commands.Execute(
            new DesignateDiggingCommand(target, designated: true, tick: 21),
            new DesignateDiggingCommandHandler(worldRepository, journal),
            tick: 21));
        Require(commands.Execute(
            new ExcavateCellCommand(target, air, tick: 22),
            new ExcavateCellCommandHandler(worldRepository, journal),
            tick: 22));
        Require(advanceJob.Handle(new AdvanceJobCommand(digJobId, tick: 22)));
        Require(advanceJob.Handle(new AdvanceJobCommand(digJobId, tick: 23)));

        CellSnapshot cell = Require(new QueryPipeline().Execute(
            new GetCellQuery(target),
            new GetCellQueryHandler(worldRepository)));
        JobSnapshot completedDig = jobRepository.Get().Get(digJobId)
            ?? throw new InvalidOperationException("Headless digging job disappeared.");
        if (cell.IsSolid
            || completedDig.Status != JobStatus.Completed
            || jobRepository.Get().GetReservations().Count != 0)
        {
            throw new InvalidOperationException(
                "Headless digging job did not complete and release reservations.");
        }

        ItemId rockChunk = new ItemId("resource.rock_chunk");
        ItemCatalog items = new ItemCatalog(new[]
        {
            new ItemDefinition(
                rockChunk,
                "Rock chunk",
                maximumStackSize: 100,
                isTool: false,
                new[] { new ItemCategoryId("raw") }),
        });
        InventoryState inventory = new InventoryState(items);
        EntityId resourceStackId = Require(state.Entities.RegisterNew());
        Require(inventory.AddStack(
            resourceStackId,
            rockChunk,
            quantity: 1,
            ItemLocation.InWorld(target),
            tick: 23));
        StorageState storage = new StorageState();
        EntityId storageId = Require(state.Entities.RegisterNew());
        Require(storage.AddZone(new StorageZoneDefinition(
            storageId,
            "Headless Stockpile",
            priority: 500,
            capacity: 10,
            StorageFilter.All())));
        InMemoryInventoryRepository inventoryRepository =
            new InMemoryInventoryRepository(inventory);
        InMemoryStorageRepository storageRepository =
            new InMemoryStorageRepository(storage);
        EntityId haulJobId = Require(state.Entities.RegisterNew());
        CreateHaulingJobHandler createHaul = new CreateHaulingJobHandler(
            inventoryRepository,
            storageRepository,
            jobRepository,
            journal);
        Require(createHaul.Handle(new CreateHaulingJobCommand(
            haulJobId,
            resourceStackId,
            quantity: 1,
            storageId,
            priority: 600,
            tick: 24)));
        SetCandidate(jobCandidates, haulJobId, residentId, residentSnapshot, distanceCost: 4);
        EnsureSingleAssignment(assignJobs.Handle(new AssignAvailableJobsCommand(tick: 24)));
        Require(advanceJob.Handle(new AdvanceJobCommand(haulJobId, tick: 25)));
        Require(advanceJob.Handle(new AdvanceJobCommand(haulJobId, tick: 26)));
        Require(advanceJob.Handle(new AdvanceJobCommand(haulJobId, tick: 27)));
        CompleteHaulingJobHandler completeHaul = new CompleteHaulingJobHandler(
            inventoryRepository,
            storageRepository,
            jobRepository,
            journal);
        Require(completeHaul.Handle(new CompleteHaulingJobCommand(
            haulJobId,
            splitStackId: default,
            tick: 28)));
        JobSnapshot completedHaul = jobRepository.Get().Get(haulJobId)
            ?? throw new InvalidOperationException("Headless hauling job disappeared.");
        if (completedHaul.Status != JobStatus.Completed
            || inventory.GetTotal(rockChunk) != 1
            || inventory.GetQuantityAt(rockChunk, ItemLocation.InStorage(storageId)) != 1
            || storage.GetReservations().Count != 0)
        {
            throw new InvalidOperationException("Headless hauling did not conserve the resource.");
        }

        BuildingSnapshot completedBuilding = HeadlessBuildingScenario.Run(
            state,
            journal,
            worldRepository,
            inventoryRepository,
            jobRepository,
            jobCandidates,
            residentId,
            residentSnapshot,
            resourceStackId,
            rockChunk,
            target,
            startTick: 29);
        int automaticHauls = HeadlessAutomaticHaulingScenario.Run(
            state,
            journal,
            startTick: 42);
        int settledResidents = HeadlessResidentSettlementScenario.Run(
            state,
            journal,
            startTick: 61);

        List<TerrainChange> corridor = new List<TerrainChange>();
        for (int x = 0; x < world.Size.Width; x++)
        {
            corridor.Add(new TerrainChange(
                new CellId(x, 1),
                new CellState(
                    air,
                    CellDesignation.None,
                    isExplored: true,
                    damage: 0,
                    temperature: 20)));
        }

        Require(world.ApplyTerrainChanges(corridor, tick: 60));
        TraversalProfile profile = TraversalProfile.CreateGroundedDwarf();
        InMemoryNavigationRepository navigationRepository =
            new InMemoryNavigationRepository();
        RebuildNavigationCommandHandler rebuildNavigation =
            new RebuildNavigationCommandHandler(navigationRepository);
        Require(rebuildNavigation.Handle(new RebuildNavigationCommand(
            profile,
            world.CreateSnapshot(),
            Array.Empty<TraversalLink>())));
        NavigationMap navigation = navigationRepository.Get(profile.Id)
            ?? throw new InvalidOperationException("Navigation map was not stored.");
        NavigationSnapshot navigationSnapshot = Require(navigation.GetSnapshot());
        PathResult route = Require(new FindPathQueryHandler(
            navigationRepository,
            new NavigationPathfinder()).Handle(new FindPathQuery(
                profile.Id,
                new PathRequest(
                    new CellId(0, 1),
                    new CellId(7, 1),
                    navigationSnapshot.NavigationVersion))));
        if (!route.Succeeded)
        {
            throw new InvalidOperationException(route.Diagnostics.Detail);
        }

        Console.WriteLine(
            $"Headless simulation completed at tick {state.Clock.TickIndex} "
            + $"with {state.Entities.Count} entities, resident intent "
            + $"{residentSnapshot.LastDecision.SelectedIntent}, dig {completedDig.Status}, "
            + $"haul {completedHaul.Status}, automatic hauls {automaticHauls}, "
            + $"settled residents {settledResidents}, building {completedBuilding.Status}, "
            + $"remaining resources {inventory.GetTotal(rockChunk)}, world version {world.Version}, "
            + $"{navigationSnapshot.Regions.Count} regions and a {route.Path!.Cells.Count}-cell route.");
        return 0;
    }

    private static void SetCandidate(
        InMemoryJobCandidateProvider provider,
        EntityId jobId,
        EntityId residentId,
        AgentSnapshot resident,
        int distanceCost)
    {
        provider.SetCandidates(jobId, new[]
        {
            new JobCandidate(
                residentId,
                resident.GetSkillLevel(new AgentSkillId("general.work")),
                distanceCost,
                isAvailable: true),
        });
    }

    private static void EnsureSingleAssignment(JobAssignmentReport report)
    {
        if (report.Assignments.Count != 1 || report.Failures.Count != 0)
        {
            throw new InvalidOperationException("Headless job was not assigned.");
        }
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

    private sealed class EntityCreationSystem : ISimulationSystem
    {
        public EntityCreationSystem(int intervalTicks)
        {
            IntervalTicks = intervalTicks;
        }

        public string Name => "headless.entity_creation";

        public int Order => 100;

        public int IntervalTicks { get; }

        public void Execute(SimulationContext context)
        {
            Require(context.State.Entities.RegisterNew());
        }
    }
}
}
