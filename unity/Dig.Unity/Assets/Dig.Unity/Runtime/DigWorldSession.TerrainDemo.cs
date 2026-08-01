using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed partial class DigWorldSession
{
    private static readonly MaterialId[] DemoTerrainBandMaterials =
    {
        DefaultTerrainMaterials.Sand,
        DefaultTerrainMaterials.StoneRock,
        DefaultTerrainMaterials.MetalBearingRock,
        DefaultTerrainMaterials.CrystallineRock,
        DefaultTerrainMaterials.LavaRock,
    };

    private static void ApplyDemoTerrainTestRegions(
        WorldState world,
        TunnelDemoLayout layout)
    {
        WorldSnapshot snapshot = world.CreateSnapshot();
        Dictionary<CellId, TerrainChange> changes = snapshot.Chunks
            .SelectMany(chunk => chunk.Cells)
            .Where(cell => cell.IsSolid)
            .Where(cell => cell.Id.X > 0 && cell.Id.X < world.Size.Width - 1)
            .ToDictionary(
                cell => cell.Id,
                cell => new TerrainChange(
                    cell.Id,
                    cell.State.WithTerrain(ResolveDemoTerrainBand(
                        cell.Id.X,
                        world.Size.Width))));

        int patchY = Math.Min(world.Size.Height - 2, layout.CaveFloorY + 2);
        for (int x = 1; x <= Math.Min(2, world.Size.Width - 2); x++)
        {
            for (int z = 0; z <= 1; z++)
            {
                CellId cell = new CellId(x, patchY, z);
                Result<CellSnapshot> current = world.GetCell(cell);
                if (current.IsSuccess && current.Value.IsSolid)
                {
                    changes[cell] = new TerrainChange(
                        cell,
                        current.Value.State.WithTerrain(
                            DefaultTerrainMaterials.Unmineable));
                }
            }
        }

        Result<WorldMutationResult> result = world.ApplyTerrainChanges(
            changes.Values,
            tick: 2);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.ToString());
        }
    }

    private static MaterialId ResolveDemoTerrainBand(int x, int width)
    {
        int interiorWidth = width - 2;
        int band = Math.Min(
            DemoTerrainBandMaterials.Length - 1,
            ((x - 1) * DemoTerrainBandMaterials.Length) / interiorWidth);
        return DemoTerrainBandMaterials[band];
    }
}

}
