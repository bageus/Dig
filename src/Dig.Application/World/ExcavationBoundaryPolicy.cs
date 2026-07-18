using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.World;

namespace Dig.Application.World
{

public sealed class ExcavationBoundaryPolicy
{
    private readonly HashSet<CellId> _protected;

    public ExcavationBoundaryPolicy(
        int width,
        int height,
        int topRockY)
    {
        if (width < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (topRockY <= 0 || topRockY >= height - 1)
        {
            throw new ArgumentOutOfRangeException(nameof(topRockY));
        }

        Width = width;
        Height = height;
        TopRockY = topRockY;
        _protected = CreateProtectedCells(width, height, topRockY);
        ProtectedCells = new ReadOnlyCollection<CellId>(
            new List<CellId>(_protected));
    }

    public int Width { get; }

    public int Height { get; }

    public int TopRockY { get; }

    public IReadOnlyList<CellId> ProtectedCells { get; }

    public bool IsProtected(CellId cell)
    {
        return _protected.Contains(cell);
    }

    private static HashSet<CellId> CreateProtectedCells(
        int width,
        int height,
        int topRockY)
    {
        HashSet<CellId> cells = new HashSet<CellId>();
        for (int x = 0; x < width; x++)
        {
            cells.Add(new CellId(x, topRockY));
            cells.Add(new CellId(x, height - 1));
        }

        for (int y = 0; y < height; y++)
        {
            cells.Add(new CellId(0, y));
            cells.Add(new CellId(width - 1, y));
        }

        return cells;
    }
}

}
