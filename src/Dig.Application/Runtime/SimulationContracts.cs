using Dig.Domain.Runtime;

namespace Dig.Application.Runtime;

public readonly record struct SimulationContext(
    long Tick,
    SimulationState State);

public interface ISimulationSystem
{
    string Name { get; }

    int Order { get; }

    int IntervalTicks { get; }

    void Execute(SimulationContext context);
}

public readonly record struct SystemExecution(
    long Tick,
    string SystemName,
    int Order);

public interface ISimulationTrace
{
    void Record(SystemExecution execution);
}

public sealed class NullSimulationTrace : ISimulationTrace
{
    public static readonly NullSimulationTrace Instance = new NullSimulationTrace();

    private NullSimulationTrace()
    {
    }

    public void Record(SystemExecution execution)
    {
    }
}
