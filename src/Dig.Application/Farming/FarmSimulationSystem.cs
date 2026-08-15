using System;
using Dig.Application.Runtime;
using Dig.Domain.Core;
using Dig.Domain.Farming;

namespace Dig.Application.Farming
{

/// <summary>
/// Advances all registered farms from the authoritative simulation clock.
/// The farm model intentionally has no independent timer so save/load and
/// accelerated simulation remain deterministic.
/// </summary>
public sealed class FarmSimulationSystem : ISimulationSystem
{
    private readonly IFarmRepository _farms;

    public FarmSimulationSystem(IFarmRepository farms)
    {
        _farms = farms ?? throw new ArgumentNullException(nameof(farms));
    }

    public string Name => "farm-ecology";

    public int Order => 300;

    public int IntervalTicks => 1;

    public void Execute(SimulationContext context)
    {
        foreach (EntityId farmId in _farms.GetFarmIds())
        {
            FarmState? farm = _farms.Get(farmId);
            if (farm == null)
            {
                continue;
            }

            farm.Advance(context.Tick);
            _farms.Save(farmId, farm);
        }
    }
}

}
