using Dig.Application.Agents;
using Dig.Application.Messaging;
using Dig.Application.Navigation;
using Dig.Application.Runtime;
using Dig.Application.World;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.Runtime;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Headless;

internal static class Program
{
    public static int Main()
    {
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
            new InMemoryAgentDecisionContextProvider(
                AgentDecisionContext.AllAvailable());
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
        MaterialCatalog materials = new MaterialCatalog(
            new[]
            {
                new MaterialDefinition(rock, isSolid: true, hardness: 100),
                new MaterialDefinition(air, isSolid: false, hardness: 0),
            });
        Result<WorldState> worldCreation = WorldState.CreateFilled(
            new WorldSize(8, 8),
            chunkSize: 4,
            materials,
            rock,
            explored: true);
        WorldState world = Require(worldCreation);

        InMemoryWorldRepository worldRepository = new InMemoryWorldRepository(world);
        CommandPipeline commands = new CommandPipeline(journal);
        CellId target = new CellId(3, 3);

        Require(commands.Execute(
            new DesignateDiggingCommand(target, designated: true, tick: 20),
            new DesignateDiggingCommandHandler(worldRepository, journal),
            tick: 20));
        Require(commands.Execute(
            new ExcavateCellCommand(target, air, tick: 21),
            new ExcavateCellCommandHandler(worldRepository, journal),
            tick: 21));

        Result<CellSnapshot> cellResult = new QueryPipeline().Execute(
            new GetCellQuery(target),
            new GetCellQueryHandler(worldRepository));
        CellSnapshot cell = Require(cellResult);
        if (cell.IsSolid)
        {
            throw new InvalidOperationException("Excavated headless cell remained solid.");
        }

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

        Require(world.ApplyTerrainChanges(corridor, tick: 22));
        TraversalProfile profile = TraversalProfile.CreateGroundedDwarf();
        InMemoryNavigationRepository navigationRepository =
            new InMemoryNavigationRepository();
        RebuildNavigationCommandHandler rebuildNavigation =
            new RebuildNavigationCommandHandler(navigationRepository);
        Require(rebuildNavigation.Handle(
            new RebuildNavigationCommand(
                profile,
                world.CreateSnapshot(),
                Array.Empty<TraversalLink>())));
        NavigationMap navigation = navigationRepository.Get(profile.Id)
            ?? throw new InvalidOperationException("Navigation map was not stored.");
        NavigationSnapshot navigationSnapshot = Require(navigation.GetSnapshot());
        FindPathQueryHandler findPath = new FindPathQueryHandler(
            navigationRepository,
            new NavigationPathfinder());
        PathResult route = Require(findPath.Handle(
            new FindPathQuery(
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
            + $"{residentSnapshot.LastDecision.SelectedIntent}, world version {world.Version}, "
            + $"{navigationSnapshot.Regions.Count} navigation regions and "
            + $"a {route.Path!.Cells.Count}-cell route.");
        return 0;
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
