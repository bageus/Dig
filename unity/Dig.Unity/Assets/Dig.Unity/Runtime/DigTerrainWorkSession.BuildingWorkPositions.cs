using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.World;
using Dig.Presentation.World;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private DemoBuildingPlacement FindDemoBuildingPlacement(
        BuildingDefinition definition,
        IReadOnlyCollection<CellId> excludedCells)
    {
        WorldCellViewModel[] openCells = _worldSession.LoadView().Chunks
            .SelectMany(chunk => chunk.Cells)
            .Where(value => !value.IsSolid)
            .ToArray();
        HashSet<CellId> open = new HashSet<CellId>(openCells.Select(
            value => new CellId(value.X, value.Y, value.Z)));
        HashSet<CellId> solid = new HashSet<CellId>(_worldSession.LoadView().Chunks
            .SelectMany(chunk => chunk.Cells)
            .Where(value => value.IsSolid)
            .Select(value => new CellId(value.X, value.Y, value.Z)));
        HashSet<CellId> excluded = new HashSet<CellId>(excludedCells);
        foreach (WorldCellViewModel candidate in openCells
            .Where(value => solid.Contains(new CellId(value.X, value.Y + 1, value.Z)))
            .OrderByDescending(value => value.X)
            .ThenByDescending(value => value.Y)
            .ThenBy(value => value.Z))
        {
            CellId origin = new CellId(candidate.X, candidate.Y, candidate.Z);
            if (excluded.Contains(origin))
            {
                continue;
            }

            CellId? workPosition = definition.ResolveWorkPositions(
                    origin,
                    BuildingOrientation.North)
                .Where(value => value.Y == origin.Y && value.Z == origin.Z)
                .Where(value => !excluded.Contains(value)
                    && open.Contains(value)
                    && solid.Contains(new CellId(value.X, value.Y + 1, value.Z)))
                .OrderBy(value => Math.Abs(value.X - origin.X))
                .ThenBy(value => value)
                .Cast<CellId?>()
                .FirstOrDefault();
            if (workPosition.HasValue)
            {
                return new DemoBuildingPlacement(origin, workPosition.Value);
            }
        }

        throw new InvalidOperationException(
            "The demo world has no supported same-plane building and side-work pair.");
    }


}

}
