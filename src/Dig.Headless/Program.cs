using Dig.Application.Runtime;
using Dig.Domain.Core;
using Dig.Domain.Runtime;

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

        Console.WriteLine(
            $"Headless simulation completed at tick {state.Clock.TickIndex} "
            + $"with {state.Entities.Count} entities.");
        return 0;
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
            Result<EntityId> result = context.State.Entities.RegisterNew();
            if (result.IsFailure)
            {
                throw new InvalidOperationException(result.Error!.ToString());
            }
        }
    }
}
