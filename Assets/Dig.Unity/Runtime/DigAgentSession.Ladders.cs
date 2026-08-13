using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.World;
using Dig.Presentation.Buildings;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    private const double WoodenLadderSpeedMultiplier = 2d;
    private readonly HashSet<CellId> _woodenLadderCells = new HashSet<CellId>();

    internal void SynchronizeWoodenLadders(
        IReadOnlyList<BuildingWorldViewModel> buildings)
    {
        if (buildings == null) throw new ArgumentNullException(nameof(buildings));
        _woodenLadderCells.Clear();
        foreach (BuildingWorldViewModel building in buildings.Where(value =>
            value.DefinitionId == "building.ladder"
            && value.Status is BuildingStatus.Completed or BuildingStatus.Damaged))
        {
            foreach (BuildingFootprintCellViewModel cell in building.Footprint)
            {
                _woodenLadderCells.Add(new CellId(cell.X, cell.Y, cell.Z));
            }
        }
    }

    private bool IsWoodenLadderStep(CellId from, CellId to)
    {
        return from.X == to.X
            && from.Z == to.Z
            && Math.Abs(from.Y - to.Y) == 1
            && _woodenLadderCells.Contains(from)
            && _woodenLadderCells.Contains(to);
    }
}

}
