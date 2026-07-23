using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.World
{

public sealed partial class WorldState
{
    public Result<WorldMutationResult> SetDigDesignations(
        IEnumerable<CellId> cellIds,
        long tick)
    {
        if (cellIds == null)
        {
            throw new ArgumentNullException(nameof(cellIds));
        }

        CellId[] requested = cellIds.ToArray();
        if (requested.Length == 0)
        {
            return ApplyTerrainChanges(Array.Empty<TerrainChange>(), tick);
        }

        HashSet<CellId> seen = new HashSet<CellId>();
        List<TerrainChange> changes = new List<TerrainChange>(requested.Length);
        foreach (CellId cellId in requested)
        {
            if (!Size.Contains(cellId))
            {
                return Result<WorldMutationResult>.Failure(WorldErrors.CellOutOfBounds);
            }

            if (!seen.Add(cellId))
            {
                return Result<WorldMutationResult>.Failure(WorldErrors.DuplicateCellChange);
            }

            CellState current = _cells[GetCellIndex(cellId)];
            MaterialDefinition material = Materials.Get(current.MaterialId)!;
            if (!material.IsSolid)
            {
                return Result<WorldMutationResult>.Failure(
                    WorldErrors.DigDesignationRequiresSolidCell);
            }

            if (!material.IsMineable)
            {
                return Result<WorldMutationResult>.Failure(WorldErrors.InvalidDesignation);
            }

            changes.Add(new TerrainChange(
                cellId,
                current.WithDesignation(CellDesignation.Dig)));
        }

        return ApplyTerrainChanges(changes, tick);
    }
}

}
