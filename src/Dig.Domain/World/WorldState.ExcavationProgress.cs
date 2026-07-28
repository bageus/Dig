using System;
using Dig.Domain.Core;

namespace Dig.Domain.World
{

public sealed partial class WorldState
{
    public Result<WorldMutationResult> CommitExcavationQuarter(
        CellId cellId,
        ExcavationQuarter quarter,
        ExcavationCutPattern cutPattern,
        MaterialId emptyMaterialId,
        long tick)
    {
        ValidateTick(tick);
        if (!Size.Contains(cellId))
        {
            return Result<WorldMutationResult>.Failure(WorldErrors.CellOutOfBounds);
        }

        int quarterValue = (int)quarter;
        if (quarterValue == 0 || (quarterValue & (quarterValue - 1)) != 0)
        {
            return Result<WorldMutationResult>.Failure(
                WorldErrors.InvalidExcavationQuarter);
        }

        if (cutPattern == ExcavationCutPattern.None
            || !Enum.IsDefined(typeof(ExcavationCutPattern), cutPattern))
        {
            return Result<WorldMutationResult>.Failure(
                WorldErrors.InvalidExcavationCutPattern);
        }


        MaterialDefinition? emptyMaterial = Materials.Get(emptyMaterialId);
        if (emptyMaterial is null)
        {
            return Result<WorldMutationResult>.Failure(WorldErrors.UnknownMaterial);
        }

        if (emptyMaterial.IsSolid)
        {
            return Result<WorldMutationResult>.Failure(
                WorldErrors.ExcavationRequiresEmptyMaterial);
        }

        CellState current = _cells[GetCellIndex(cellId)];
        MaterialDefinition material = Materials.Get(current.MaterialId)!;
        if (!material.IsSolid)
        {
            // Finalize/retry can observe the already excavated terrain after the
            // authoritative World mutation. Treat that state as idempotent success.
            return Result<WorldMutationResult>.Success(CreateNoChangeResult());
        }

        if (current.Designation != CellDesignation.Dig)
        {
            return Result<WorldMutationResult>.Failure(
                WorldErrors.ExcavationQuarterRequiresDesignation);
        }

        if (current.ExcavationCutPattern != ExcavationCutPattern.None
            && current.ExcavationCutPattern != cutPattern)
        {
            return Result<WorldMutationResult>.Failure(
                WorldErrors.ExcavationCutPatternConflict);
        }

        if ((current.CompletedExcavationQuarters & quarter) != 0)
        {
            return Result<WorldMutationResult>.Success(CreateNoChangeResult());
        }

        ExcavationQuarter completed =
            current.CompletedExcavationQuarters | quarter;
        CellState target = current.WithExcavationProgress(completed, cutPattern);
        if (completed == ExcavationQuarter.All)
        {
            target = target.WithExcavatedTerrain(emptyMaterialId);
        }
        return ApplyTerrainChanges(
            new[] { new TerrainChange(cellId, target) },
            tick);
    }
}

}
