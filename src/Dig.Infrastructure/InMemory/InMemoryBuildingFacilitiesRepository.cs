using Dig.Application.Agents;
using Dig.Domain.Buildings;

namespace Dig.Infrastructure.InMemory;

public sealed class InMemoryBuildingFacilitiesRepository
    : IBuildingFacilitiesRepository
{
    private BuildingFacilitiesState _facilities;

    public InMemoryBuildingFacilitiesRepository(
        BuildingFacilitiesState? facilities = null)
    {
        _facilities = facilities ?? new BuildingFacilitiesState();
    }

    public BuildingFacilitiesState Get()
    {
        return _facilities;
    }

    public void Save(BuildingFacilitiesState facilities)
    {
        _facilities = facilities ?? throw new ArgumentNullException(nameof(facilities));
    }
}
