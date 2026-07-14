using Dig.Application.Production;
using Dig.Domain.Core;
using Dig.Domain.Production;
using Dig.Domain.Technology;

namespace Dig.Infrastructure.InMemory;

public sealed class InMemoryProductionRepository : IProductionRepository
{
    private ProductionState _production;

    public InMemoryProductionRepository(ProductionState? production = null)
    {
        _production = production ?? new ProductionState();
    }

    public ProductionState Get()
    {
        return _production;
    }

    public void Save(ProductionState production)
    {
        _production = production ?? throw new ArgumentNullException(nameof(production));
    }
}

public sealed class InMemoryTechnologyRepository : ITechnologyRepository
{
    private TechnologyState _technology;

    public InMemoryTechnologyRepository(TechnologyState? technology = null)
    {
        _technology = technology ?? new TechnologyState();
    }

    public TechnologyState Get()
    {
        return _technology;
    }

    public void Save(TechnologyState technology)
    {
        _technology = technology ?? throw new ArgumentNullException(nameof(technology));
    }
}

public sealed class FixedEnergyAvailability : IEnergyAvailability
{
    private readonly HashSet<EntityId> _disabledBuildings = new HashSet<EntityId>();

    public FixedEnergyAvailability(bool suppliesEnergy = true)
    {
        SuppliesEnergy = suppliesEnergy;
    }

    public bool SuppliesEnergy { get; set; }

    public void SetBuildingEnabled(EntityId buildingId, bool enabled)
    {
        if (buildingId.IsEmpty)
        {
            throw new ArgumentException("Building id cannot be empty.", nameof(buildingId));
        }

        if (enabled)
        {
            _disabledBuildings.Remove(buildingId);
        }
        else
        {
            _disabledBuildings.Add(buildingId);
        }
    }

    public bool CanSupply(EntityId buildingId, int energyPerWorkTick)
    {
        if (buildingId.IsEmpty)
        {
            throw new ArgumentException("Building id cannot be empty.", nameof(buildingId));
        }

        if (energyPerWorkTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(energyPerWorkTick));
        }

        return energyPerWorkTick == 0
            || (SuppliesEnergy && !_disabledBuildings.Contains(buildingId));
    }
}
