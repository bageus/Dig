using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Domain.Buildings
{

public sealed partial class BuildingsState
{
    public int ReconcileAdaptiveLadders(WorldSnapshot world)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));

        Dictionary<CellId, CellSnapshot> cells = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(cell => cell.Id);
        HashSet<CellId> occupiedByOtherBuildings = new HashSet<CellId>(
            _buildings.Values
                .Where(value => value.Status is not BuildingStatus.Removed
                    and not BuildingStatus.Cancelled)
                .Where(value => value.Definition.Id.ToString() != "building.ladder")
                .SelectMany(value => value.Footprint));
        int changed = 0;
        foreach (BuildingProjectState ladder in _buildings.Values
            .Where(value => value.Definition.Id.ToString() == "building.ladder")
            .Where(value => value.Status is BuildingStatus.Completed
                or BuildingStatus.Damaged))
        {
            IReadOnlyList<CellId> footprint =
                BuildingPlacementValidator.ResolveLadderFootprint(
                    ladder.Origin,
                    cells,
                    occupiedByOtherBuildings);

            if (ladder.SynchronizeAdaptiveLadder(footprint)) changed++;
        }

        return changed;
    }

}

}
