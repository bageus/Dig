using System;
using System.Linq;
using Dig.Domain.WorldObjects;

namespace Dig.Application.Saving
{

public static partial class SaveGameBuilder
{
    private static BarrelSectionSaveData? BuildBarrels(BarrelState? barrels)
    {
        if (barrels is null)
        {
            return null;
        }

        return new BarrelSectionSaveData
        {
            Barrels = barrels.GetAll()
                .OrderBy(value => value.BarrelId.ToString(), StringComparer.Ordinal)
                .Select(value => new BarrelSaveData
                {
                    BarrelId = value.BarrelId.ToString(),
                    DefinitionId = value.DefinitionId.ToString(),
                    CellX = value.Cell.X,
                    CellY = value.Cell.Y,
                    CellZ = value.Cell.Z,
                    Lifecycle = (int)value.Lifecycle,
                    ContentsItemId = value.ContentsItemId.ToString(),
                    ContentsGeneration = value.ContentsGeneration,
                    ContentsMaterialized = value.ContentsMaterialized,
                    HasFallSource = value.FallSourceCell.HasValue,
                    FallSourceX = value.FallSourceCell?.X ?? 0,
                    FallSourceY = value.FallSourceCell?.Y ?? 0,
                    FallSourceZ = value.FallSourceCell?.Z ?? 0,
                    HasFallLanding = value.FallLandingCell.HasValue,
                    FallLandingX = value.FallLandingCell?.X ?? 0,
                    FallLandingY = value.FallLandingCell?.Y ?? 0,
                    FallLandingZ = value.FallLandingCell?.Z ?? 0,
                    Version = value.Version,
                })
                .ToArray(),
        };
    }
}

}