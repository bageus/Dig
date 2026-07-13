namespace Dig.Domain.Runtime;

public sealed class SimulationState
{
    public SimulationState(
        SimulationClock clock,
        RandomStreamCatalog randomStreams,
        EntityRegistry entities)
    {
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        RandomStreams = randomStreams ?? throw new ArgumentNullException(nameof(randomStreams));
        Entities = entities ?? throw new ArgumentNullException(nameof(entities));
    }

    public SimulationClock Clock { get; }

    public RandomStreamCatalog RandomStreams { get; }

    public EntityRegistry Entities { get; }

    public static SimulationState Create(
        ulong worldSeed,
        TimeSpan tickDuration,
        SimulationRate rate = SimulationRate.Normal)
    {
        RandomStreamCatalog randomStreams = new RandomStreamCatalog(worldSeed);
        return new SimulationState(
            new SimulationClock(tickDuration, initialRate: rate),
            randomStreams,
            new EntityRegistry(randomStreams));
    }
}
