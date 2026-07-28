using System;
using System.Linq;
using Dig.Domain.WorldObjects;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameBuilder
{
    private static BarrelSaveData BuildBarrels(BarrelState barrels)
    {
        if (barrels is null)
        {
            throw new ArgumentNullException(nameof(barrels));
        }

        BarrelSaveData data = new BarrelSaveData();
        foreach (BarrelSnapshot barrel in barrels.GetAll()
            .OrderBy(value => value.BarrelId.ToString(), StringComparer.Ordinal))
        {
            data.Barrels.Add(new BarrelEntitySaveData
            {
                BarrelId = barrel.BarrelId.ToString(),
                DefinitionId = barrel.DefinitionId.ToString(),
                X = barrel.Cell.X,
                Y = barrel.Cell.Y,
                Z = barrel.Cell.Z,
                Lifecycle = (int)barrel.Lifecycle,
                ContentsItemId = barrel.ContentsItemId.ToString(),
                ContentsGeneration = barrel.ContentsGeneration,
                ContentsMaterialized = barrel.ContentsMaterialized,
                FallSourceX = barrel.FallSourceCell?.X,
                FallSourceY = barrel.FallSourceCell?.Y,
                FallSourceZ = barrel.FallSourceCell?.Z,
                FallLandingX = barrel.FallLandingCell?.X,
                FallLandingY = barrel.FallLandingCell?.Y,
                FallLandingZ = barrel.FallLandingCell?.Z,
                Version = barrel.Version,
            });
        }

        return data;
    }
}

}
