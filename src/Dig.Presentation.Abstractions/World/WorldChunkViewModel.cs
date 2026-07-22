using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace Dig.Presentation.World
{

public sealed class WorldChunkViewModel
{
    public WorldChunkViewModel(
        int x,
        int y,
        int z,
        long version,
        IReadOnlyCollection<WorldCellViewModel> cells)
    {
        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (cells is null)
        {
            throw new ArgumentNullException(nameof(cells));
        }

        X = x;
        Y = y;
        Z = z;
        Version = version;
        Cells = new ReadOnlyCollection<WorldCellViewModel>(cells
            .OrderBy(cell => cell.Z)
            .ThenBy(cell => cell.Y)
            .ThenBy(cell => cell.X)
            .ToArray());
    }

    public int X { get; }
    public int Y { get; }
    public int Z { get; }
    public long Version { get; }
    public IReadOnlyList<WorldCellViewModel> Cells { get; }
}
}
