using System;
using Dig.Domain.Runtime;

namespace Dig.Application.Runtime
{

public readonly struct SimulationContext
{
    public SimulationContext(long tick, SimulationState state)
    {
        Tick = tick;
        State = state ?? throw new ArgumentNullException(nameof(state));
    }

    public long Tick { get; }

    public SimulationState State { get; }
}

public interface ISimulationSystem
{
    string Name { get; }

    int Order { get; }

    int IntervalTicks { get; }

    void Execute(SimulationContext context);
}

public readonly struct SystemExecution
{
    public SystemExecution(long tick, string systemName, int order)
    {
        Tick = tick;
        SystemName = systemName;
        Order = order;
    }

    public long Tick { get; }

    public string SystemName { get; }

    public int Order { get; }
}

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
}
