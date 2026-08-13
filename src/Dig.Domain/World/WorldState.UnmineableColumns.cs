using System.Collections.Generic;
using System.Linq;

namespace Dig.Domain.World
{

public sealed partial class WorldState
{
    private static void NormalizeRestoredUnmineableColumns(
        WorldSize size,
        MaterialCatalog materials,
        CellState[] cells)
    {
        for (int y = 0; y < size.Height; y++)
        {
            for (int x = 0; x < size.Width; x++)
            {
                int frontIndex = checked((y * size.Width) + x);
                CellState front = cells[frontIndex];
                MaterialDefinition? material = materials.Get(front.MaterialId);
                if (material == null || !material.IsSolid || material.IsMineable) continue;

                for (int z = CellId.MinimumDepth + 1; z < size.Depth; z++)
                {
                    int index = checked((((z * size.Height) + y) * size.Width) + x);
                    cells[index] = cells[index].WithTerrain(front.MaterialId);
                }
            }
        }
    }

    private TerrainChange[] PropagateFrontUnmineableColumns(
        IReadOnlyCollection<TerrainChange> changes)
    {
        if (changes.GroupBy(value => value.CellId).Any(group => group.Count() > 1))
        {
            return changes.ToArray();
        }

        Dictionary<CellId, TerrainChange> normalized = changes
            .ToDictionary(value => value.CellId);
        foreach (TerrainChange front in changes.Where(value =>
            value.CellId.Z == CellId.MinimumDepth))
        {
            MaterialDefinition? material = Materials.Get(front.TargetState.MaterialId);
            if (material == null || !material.IsSolid || material.IsMineable) continue;

            for (int z = CellId.MinimumDepth + 1; z < Size.Depth; z++)
            {
                CellId deepCell = new CellId(front.CellId.X, front.CellId.Y, z);
                CellState current = _cells[GetCellIndex(deepCell)];
                normalized[deepCell] = new TerrainChange(
                    deepCell,
                    current.WithTerrain(front.TargetState.MaterialId));
            }
        }

        return normalized.Values.OrderBy(value => value.CellId).ToArray();
    }
}

}
