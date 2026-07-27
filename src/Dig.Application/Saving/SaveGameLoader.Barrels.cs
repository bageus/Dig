using System;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Domain.WorldObjects;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameLoader
{
    private Result<BarrelState?> BuildBarrels(BarrelSectionSaveData? section)
    {
        if (section is null)
        {
            return Result<BarrelState?>.Success(null);
        }

        if (_barrelCatalog is null)
        {
            return section.Barrels.Length == 0
                ? Result<BarrelState?>.Success(null)
                : Result<BarrelState?>.Failure(SaveErrors.UnknownBarrelDefinition);
        }

        BarrelSnapshot[] snapshots;
        try
        {
            snapshots = section.Barrels.Select(value => new BarrelSnapshot(
                EntityId.Parse(value.BarrelId),
                new BarrelDefinitionId(value.DefinitionId),
                new CellId(value.CellX, value.CellY, value.CellZ),
                (BarrelLifecycle)value.Lifecycle,
                new ItemId(value.ContentsItemId),
                value.ContentsGeneration,
                value.ContentsMaterialized,
                value.HasFallSource
                    ? new CellId(value.FallSourceX, value.FallSourceY, value.FallSourceZ)
                    : null,
                value.HasFallLanding
                    ? new CellId(value.FallLandingX, value.FallLandingY, value.FallLandingZ)
                    : null,
                value.Version)).ToArray();
        }
        catch (Exception)
        {
            return Result<BarrelState?>.Failure(SaveErrors.InvalidPayload);
        }

        Result<BarrelState> restored = BarrelState.Restore(_barrelCatalog, snapshots);
        return restored.IsSuccess
            ? Result<BarrelState?>.Success(restored.Value)
            : Result<BarrelState?>.Failure(restored.Error!);
    }
}

}