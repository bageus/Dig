using System;
using Dig.Application.Strategy;
using Dig.Domain.Strategy;

namespace Dig.Infrastructure.InMemory
{

public sealed class InMemoryStrategicAiRepository : IStrategicAiRepository
{
    private StrategicAiState _strategy;

    public InMemoryStrategicAiRepository(StrategicAiState strategy)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    public StrategicAiState Get()
    {
        return _strategy;
    }

    public void Save(StrategicAiState strategy)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }
}
}
