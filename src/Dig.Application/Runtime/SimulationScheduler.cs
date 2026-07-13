using System.Collections.ObjectModel;

namespace Dig.Application.Runtime;

public sealed class SimulationScheduler
{
    private readonly List<ISimulationSystem> _systems = new List<ISimulationSystem>();
    private readonly HashSet<string> _systemNames = new HashSet<string>(StringComparer.Ordinal);
    private readonly ISimulationTrace _trace;
    private bool _isSealed;

    public SimulationScheduler(ISimulationTrace? trace = null)
    {
        _trace = trace ?? NullSimulationTrace.Instance;
    }

    public bool IsSealed => _isSealed;

    public IReadOnlyList<string> RegisteredSystems
    {
        get
        {
            string[] names = _systems.Select(system => system.Name).ToArray();
            return new ReadOnlyCollection<string>(names);
        }
    }

    public void Register(ISimulationSystem system)
    {
        if (system is null)
        {
            throw new ArgumentNullException(nameof(system));
        }

        if (_isSealed)
        {
            throw new InvalidOperationException(
                "Simulation systems cannot be registered after execution starts.");
        }

        if (string.IsNullOrWhiteSpace(system.Name))
        {
            throw new ArgumentException("Simulation system name is required.", nameof(system));
        }

        if (system.IntervalTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(system),
                "Simulation system interval must be greater than zero.");
        }

        if (!_systemNames.Add(system.Name))
        {
            throw new ArgumentException(
                $"Simulation system '{system.Name}' is already registered.",
                nameof(system));
        }

        _systems.Add(system);
    }

    public void Seal()
    {
        if (_isSealed)
        {
            return;
        }

        _systems.Sort(CompareSystems);
        _isSealed = true;
    }

    public void ExecuteTick(SimulationContext context)
    {
        if (context.Tick <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(context));
        }

        Seal();

        foreach (ISimulationSystem system in _systems)
        {
            if (context.Tick % system.IntervalTicks != 0)
            {
                continue;
            }

            system.Execute(context);
            _trace.Record(new SystemExecution(
                context.Tick,
                system.Name,
                system.Order));
        }
    }

    private static int CompareSystems(
        ISimulationSystem left,
        ISimulationSystem right)
    {
        int orderComparison = left.Order.CompareTo(right.Order);
        return orderComparison != 0
            ? orderComparison
            : StringComparer.Ordinal.Compare(left.Name, right.Name);
    }
}
