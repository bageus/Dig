using System;
using System.Collections.Generic;
using Dig.Domain.World;

namespace Dig.Domain.Navigation
{

public sealed class TunnelDemoLayout
{
    public TunnelDemoLayout(
        int surfaceMinX,
        int surfaceMaxX,
        int surfaceY,
        int shaftX,
        int shaftZ,
        int caveMinX,
        int caveMaxX,
        int caveCeilingY,
        int caveFloorY)
    {
        if (surfaceMinX < 0 || surfaceMaxX < surfaceMinX)
        {
            throw new ArgumentOutOfRangeException(nameof(surfaceMinX));
        }

        if (surfaceY < 0 || shaftX < 0 || shaftZ < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(surfaceY));
        }

        if (caveMinX < 0 || caveMaxX < caveMinX)
        {
            throw new ArgumentOutOfRangeException(nameof(caveMinX));
        }

        if (caveCeilingY < 0 || caveFloorY <= caveCeilingY)
        {
            throw new ArgumentOutOfRangeException(nameof(caveFloorY));
        }

        SurfaceMinX = surfaceMinX;
        SurfaceMaxX = surfaceMaxX;
        SurfaceY = surfaceY;
        ShaftX = shaftX;
        ShaftZ = shaftZ;
        CaveMinX = caveMinX;
        CaveMaxX = caveMaxX;
        CaveCeilingY = caveCeilingY;
        CaveFloorY = caveFloorY;
    }

    public int SurfaceMinX { get; }
    public int SurfaceMaxX { get; }
    public int SurfaceY { get; }
    public int ShaftX { get; }
    public int ShaftZ { get; }
    public int CaveMinX { get; }
    public int CaveMaxX { get; }
    public int CaveCeilingY { get; }
    public int CaveFloorY { get; }
    public int CaveWidth => CaveMaxX - CaveMinX + 1;
    public int CaveHeight => CaveFloorY - CaveCeilingY;
}

public sealed partial class TunnelNavigationVolume
{
    public static TunnelNavigationVolume CreateDemo(int width, int height, int depth = 4)
    {
        if (width < 8)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height < 8)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (depth != 4)
        {
            throw new ArgumentOutOfRangeException(
                nameof(depth),
                "The demo uses exactly four depth cells.");
        }

        const int surfaceY = 2;
        const int nearestDepthZ = 0;
        int caveFloorY = surfaceY + 4;
        int caveCeilingY = caveFloorY - 3;
        int surfaceMinX = 1;
        int surfaceMaxX = width - 2;
        int shaftX = width / 2;
        int caveWidth = Math.Max(4, Math.Min(6, width - 4));
        int caveMinX = Math.Max(
            1,
            Math.Min(width - caveWidth - 1, shaftX + 2));
        int caveMaxX = caveMinX + caveWidth - 1;
        TunnelDemoLayout layout = new TunnelDemoLayout(
            surfaceMinX,
            surfaceMaxX,
            surfaceY,
            shaftX,
            nearestDepthZ,
            caveMinX,
            caveMaxX,
            caveCeilingY,
            caveFloorY);

        HashSet<SpatialCellId> open = new HashSet<SpatialCellId>();
        AddPlatform(
            open,
            surfaceMinX,
            surfaceMaxX,
            surfaceY,
            depth);
        AddPlatform(
            open,
            caveMinX,
            caveMaxX,
            caveFloorY,
            depth);

        HashSet<SpatialCellId> vertical = new HashSet<SpatialCellId>();
        for (int y = surfaceY; y <= caveFloorY; y++)
        {
            SpatialCellId shaft = new SpatialCellId(shaftX, y, nearestDepthZ);
            open.Add(shaft);
            vertical.Add(shaft);
        }

        int corridorMinX = Math.Min(shaftX, caveMinX);
        int corridorMaxX = Math.Max(shaftX, caveMinX);
        for (int x = corridorMinX; x <= corridorMaxX; x++)
        {
            open.Add(new SpatialCellId(x, caveFloorY, nearestDepthZ));
        }

        return new TunnelNavigationVolume(
            width,
            height,
            depth,
            open,
            vertical,
            layout);
    }

    private static void AddPlatform(
        ISet<SpatialCellId> cells,
        int minX,
        int maxX,
        int y,
        int depth)
    {
        for (int x = minX; x <= maxX; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                cells.Add(new SpatialCellId(x, y, z));
            }
        }
    }
}

}
