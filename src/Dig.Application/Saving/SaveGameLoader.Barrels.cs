using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Domain.WorldObjects;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameLoader
{
    private static Result<BarrelState> BuildBarrelState(
        BarrelSaveData data,
        BarrelCatalog? catalog)
    {
        if (data is null || data.Barrels is null)
        {
            throw new InvalidOperationException("Barrel save data is missing.");
        }

        if (data.Barrels.Count > 0 && catalog is null)
        {
            return Result<BarrelState>.Failure(SaveErrors.UnknownBarrelDefinition);
        }

        BarrelCatalog resolvedCatalog = catalog ?? new BarrelCatalog(
            Array.Empty<BarrelDefinition>());
        List<BarrelSnapshot> snapshots = new List<BarrelSnapshot>();
        foreach (BarrelEntitySaveData barrel in data.Barrels
            .OrderBy(value => value.BarrelId, StringComparer.Ordinal))
        {
            BarrelDefinitionId definitionId = new BarrelDefinitionId(barrel.DefinitionId);
            if (!Enum.IsDefined(typeof(BarrelLifecycle), barrel.Lifecycle)
                || !resolvedCatalog.Contains(definitionId))
            {
                return Result<BarrelState>.Failure(SaveErrors.UnknownBarrelDefinition);
            }

            snapshots.Add(new BarrelSnapshot(
                EntityId.Parse(barrel.BarrelId),
                definitionId,
                new CellId(barrel.X, barrel.Y, barrel.Z),
                (BarrelLifecycle)barrel.Lifecycle,
                new ItemId(barrel.ContentsItemId),
                barrel.ContentsGeneration,
                barrel.ContentsMaterialized,
                ParseOptionalCell(
                    barrel.FallSourceX,
                    barrel.FallSourceY,
                    barrel.FallSourceZ),
                ParseOptionalCell(
                    barrel.FallLandingX,
                    barrel.FallLandingY,
                    barrel.FallLandingZ),
                barrel.Version));
        }

        return BarrelState.Restore(resolvedCatalog, snapshots);
    }

    private static CellId? ParseOptionalCell(int? x, int? y, int? z)
    {
        if (!x.HasValue && !y.HasValue && !z.HasValue)
        {
            return null;
        }

        if (!x.HasValue || !y.HasValue || !z.HasValue)
        {
            throw new InvalidOperationException("Saved barrel fall cell is incomplete.");
        }

        return new CellId(x.Value, y.Value, z.Value);
    }
}

}
