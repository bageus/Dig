using System;
using System.Collections.Generic;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Presentation.World
{

public sealed class TerrainDepthVolumePresenter
{
    public TerrainDepthVolumeViewModel Present(
        TunnelNavigationVolume volume,
        string solidMaterialId,
        int hardness,
        IReadOnlyCollection<SpatialCellId> excavatedCells)
    {
        if (volume is null)
        {
            throw new ArgumentNullException(nameof(volume));
        }

        if (string.IsNullOrWhiteSpace(solidMaterialId))
        {
            throw new ArgumentException(
                "A depth terrain material id is required.",
                nameof(solidMaterialId));
        }

        if (hardness < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hardness));
        }

        if (excavatedCells is null)
        {
            throw new ArgumentNullException(nameof(excavatedCells));
        }

        HashSet<SpatialCellId> excavated = new HashSet<SpatialCellId>(
            excavatedCells);
        List<SpatialCellId> solid = new List<SpatialCellId>();
        for (int z = 1; z < volume.Depth; z++)
        {
            for (int y = 0; y < volume.Height; y++)
            {
                for (int x = 0; x < volume.Width; x++)
                {
                    SpatialCellId cell = new SpatialCellId(x, y, z);
                    if (IsSolid(volume, cell, excavated))
                    {
                        solid.Add(cell);
                    }
                }
            }
        }

        long version = CalculateVersion(
            volume,
            solidMaterialId,
            hardness,
            solid);
        return new TerrainDepthVolumeViewModel(
            volume.Width,
            volume.Height,
            volume.Depth,
            solidMaterialId,
            hardness,
            version,
            solid);
    }

    private static bool IsSolid(
        TunnelNavigationVolume volume,
        SpatialCellId cell,
        ISet<SpatialCellId> excavated)
    {
        if (volume.IsOpen(cell) || excavated.Contains(cell))
        {
            return false;
        }

        TunnelDemoLayout? layout = volume.DemoLayout;
        if (layout == null)
        {
            return true;
        }

        if (cell.Y < layout.SurfaceY)
        {
            return false;
        }

        bool naturalCaveInterior = cell.X >= layout.CaveMinX
            && cell.X <= layout.CaveMaxX
            && cell.Y > layout.CaveCeilingY
            && cell.Y <= layout.CaveFloorY;
        return !naturalCaveInterior;
    }

    private static long CalculateVersion(
        TunnelNavigationVolume volume,
        string materialId,
        int hardness,
        IReadOnlyList<SpatialCellId> solidCells)
    {
        const ulong offset = 1469598103934665603UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offset;
        Mix(ref hash, (ulong)(uint)volume.Width, prime);
        Mix(ref hash, (ulong)(uint)volume.Height, prime);
        Mix(ref hash, (ulong)(uint)volume.Depth, prime);
        Mix(ref hash, (ulong)(uint)hardness, prime);
        for (int index = 0; index < materialId.Length; index++)
        {
            Mix(ref hash, materialId[index], prime);
        }

        for (int index = 0; index < solidCells.Count; index++)
        {
            SpatialCellId cell = solidCells[index];
            Mix(ref hash, (ulong)(uint)cell.X, prime);
            Mix(ref hash, (ulong)(uint)cell.Y, prime);
            Mix(ref hash, (ulong)(uint)cell.Z, prime);
        }

        return unchecked((long)(hash & (ulong)long.MaxValue));
    }

    private static void Mix(ref ulong hash, ulong value, ulong prime)
    {
        hash ^= value;
        hash *= prime;
    }
}

}
