using System;
using Dig.Application.Buildings;
using Dig.Domain.Buildings;

namespace Dig.Infrastructure.InMemory
{

public sealed class InMemoryBuildingsRepository : IBuildingsRepository
{
    private BuildingsState _buildings;

    public InMemoryBuildingsRepository(BuildingsState? buildings = null)
    {
        _buildings = buildings ?? new BuildingsState();
    }

    public BuildingsState Get()
    {
        return _buildings;
    }

    public void Save(BuildingsState buildings)
    {
        _buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
    }
}
}
