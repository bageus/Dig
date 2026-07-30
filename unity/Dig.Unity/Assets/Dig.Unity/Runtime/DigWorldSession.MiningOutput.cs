using System;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed partial class DigWorldSession
{
    private int _miningOutputWorldSeed = DefaultDemoGenerationSeed;

    internal int MiningOutputWorldSeed => _miningOutputWorldSeed;

    internal int MiningOutputGeneratorVersion => DemoDepositAlgorithmVersion;

    internal TerrainDepositState TerrainDeposits => _repository.Get().TerrainDeposits;

    internal MaterialDefinition ResolveTerrainMaterial(CellId cell)
    {
        Result<CellSnapshot> current = _repository.Get().GetCell(cell);
        if (current.IsFailure)
        {
            throw new InvalidOperationException(current.Error!.ToString());
        }

        MaterialId sourceMaterialId = current.Value.State.ExcavationSourceMaterialId.IsEmpty
            ? current.Value.State.MaterialId
            : current.Value.State.ExcavationSourceMaterialId;
        MaterialDefinition? material = _repository.Get().Materials.Get(sourceMaterialId);
        if (material == null)
        {
            throw new InvalidOperationException(
                $"Terrain material '{sourceMaterialId}' is missing from the world catalog.");
        }

        if (!material.IsMineable || material.OutputProfile != null)
        {
            return material;
        }

        return new MaterialDefinition(
            material.Id,
            material.DisplayName,
            material.IsSolid,
            material.Hardness,
            isMineable: true,
            outputProfile: new TerrainOutputProfile(
                "terrain-output.legacy-demo-rock",
                version: 1,
                new[]
                {
                    new TerrainOutputEntry(
                        new ItemId("material.stone"),
                        probabilityPermille: 1_000,
                        minimumQuantity: 1,
                        maximumQuantity: 3),
                }));
    }
}

}
