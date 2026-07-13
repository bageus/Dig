using Dig.Application.Messaging;
using Dig.Application.Runtime;
using Dig.Application.World;
using Dig.Domain.Core;
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
        SimulationScheduler scheduler = new SimulationScheduler();
        scheduler.Register(new EntityCreationSystem(intervalTicks: 5));
        SimulationRunner runner = new SimulationRunner(state, scheduler);
        runner.Step(20);

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
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
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

        Console.WriteLine(
            $"Headless simulation completed at tick {state.Clock.TickIndex} "
            + $"with {state.Entities.Count} entities, world version {world.Version}, "
            + $"and {world.PeekDirtyChunks().Count} dirty chunks.");
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
