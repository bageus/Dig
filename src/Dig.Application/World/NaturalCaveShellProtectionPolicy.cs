using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Application.World
{

public sealed class NaturalCaveShellProtectionPolicy
{
    public IReadOnlyList<CellId> Resolve(
        TunnelDemoLayout layout,
        WorldSize worldSize)
    {
        if (layout == null)
        {
            throw new ArgumentNullException(nameof(layout));
        }

        HashSet<CellId> protectedCells = new HashSet<CellId>();
        int leftX = layout.CaveMinX - 1;
        int rightX = layout.CaveMaxX + 1;
        int topY = layout.CaveCeilingY;
        int bottomY = layout.CaveFloorY + 1;
        for (int x = leftX; x <= rightX; x++)
        {
            AddIfContained(protectedCells, worldSize, x, topY);
            AddIfContained(protectedCells, worldSize, x, bottomY);
        }

        for (int y = topY + 1; y < bottomY; y++)
        {
            AddIfContained(protectedCells, worldSize, leftX, y);
            AddIfContained(protectedCells, worldSize, rightX, y);
        }

        return protectedCells.OrderBy(cell => cell).ToArray();
    }

    private static void AddIfContained(
        ISet<CellId> cells,
        WorldSize size,
        int x,
        int y)
    {
        if (x >= 0 && y >= 0 && x < size.Width && y < size.Height)
        {
            cells.Add(new CellId(x, y));
        }
    }
}

}