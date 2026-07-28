using System;
using Dig.Application.Production;
using Dig.Domain.Production;

namespace Dig.Infrastructure.InMemory
{

public sealed class InMemoryBuildingSupplyRepository : IBuildingSupplyRepository
{
    private BuildingSupplyState _supply;

    public InMemoryBuildingSupplyRepository(BuildingSupplyState? supply = null)
    {
        _supply = supply ?? new BuildingSupplyState();
    }

    public BuildingSupplyState Get() => _supply;

    public void Save(BuildingSupplyState supply)
    {
        _supply = supply ?? throw new ArgumentNullException(nameof(supply));
    }
}

}
