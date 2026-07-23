using System;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed partial class DigWorldSession
{
    private int _miningOutputWorldSeed = DefaultDemoGenerationSeed;

    internal int MiningOutputWorldSeed => _miningOutputWorldSeed;

    internal int MiningOutputGeneratorVersion => DemoDepositAlgorithmVersion;

    internal TerrainDepositState TerrainDeposits => _terrainDeposits;

    internal MaterialDefinition ResolveTerrainMaterial(CellId cell)
    {
        Result<CellSnapshot> current = _repository.Get().GetCell(cell);
        if (current.IsFailure)
        {
            throw new InvalidOperationException(current.Error!.ToString());
        }

        MaterialDefinition? material = _repository.Get().Materials.Get(
            current.Value.State.MaterialId);
        return material ?? throw new InvalidOperationException(
            $"Terrain material '{current.Value.State.MaterialId}' is missing from the world catalog.");
    }
}

}
