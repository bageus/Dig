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
            List<CellId> footprint = ladder.Footprint.OrderBy(cell => cell.Y).ToList();
            for (int y = footprint[0].Y - 1;
                footprint.Count < BuildingPlacementValidator.MaximumLadderHeight;
                y--)
            {
                CellId candidate = new CellId(ladder.Origin.X, y,
                    BuildingPlacementValidator.LadderDepth);
                if (!CanOccupy(candidate, cells, occupiedByOtherBuildings)) break;
                footprint.Insert(0, candidate);
            }

            for (int y = footprint[footprint.Count - 1].Y + 1;
                footprint.Count < BuildingPlacementValidator.MaximumLadderHeight;
                y++)
            {
                CellId candidate = new CellId(ladder.Origin.X, y,
                    BuildingPlacementValidator.LadderDepth);
                if (!CanOccupy(candidate, cells, occupiedByOtherBuildings)) break;
                footprint.Add(candidate);
            }

            if (ladder.ExtendAdaptiveLadder(footprint)) changed++;
        }

        return changed;
    }

    private static bool CanOccupy(
        CellId cell,
        IReadOnlyDictionary<CellId, CellSnapshot> cells,
        ISet<CellId> occupied)
    {
        return !occupied.Contains(cell)
            && cells.TryGetValue(cell, out CellSnapshot snapshot)
            && !snapshot.IsSolid
            && snapshot.State.IsExplored;
    }
}

}
