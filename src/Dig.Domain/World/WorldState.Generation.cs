using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.World
{

public sealed partial class WorldState
{
    internal static Result<WorldState> CreateGenerated(
        WorldSize size,
        int chunkSize,
        MaterialCatalog materials,
        IEnumerable<CellState> cells)
    {
        if (materials is null)
        {
            throw new ArgumentNullException(nameof(materials));
        }

        if (cells is null)
        {
            throw new ArgumentNullException(nameof(cells));
        }

        CellState[] generatedCells = cells.ToArray();
        int expectedCount = checked(size.Width * size.Height);
        if (generatedCells.Length != expectedCount)
        {
            throw new ArgumentException(
                $"Expected {expectedCount} generated cells but received {generatedCells.Length}.",
                nameof(cells));
        }

        foreach (CellState cell in generatedCells)
        {
            if (!materials.Contains(cell.MaterialId))
            {
                return Result<WorldState>.Failure(WorldErrors.UnknownMaterial);
            }
        }

        ChunkLayout layout = new ChunkLayout(size, chunkSize);
        return Result<WorldState>.Success(
            new WorldState(size, layout, materials, generatedCells));
    }
}

}
