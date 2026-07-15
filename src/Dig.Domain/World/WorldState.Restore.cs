using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.World
{

public sealed partial class WorldState
{
    public static Result<WorldState> Restore(
        WorldSnapshot snapshot,
        MaterialCatalog materials)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (materials is null)
        {
            throw new ArgumentNullException(nameof(materials));
        }

        ChunkLayout layout = new ChunkLayout(snapshot.Size, snapshot.ChunkSize);
        if (snapshot.Chunks.Count != layout.ChunkCount)
        {
            return Result<WorldState>.Failure(new DomainError(
                "world.restore.chunk_count",
                "The world snapshot does not contain every expected chunk."));
        }

        CellState[] cells = new CellState[checked(snapshot.Size.Width * snapshot.Size.Height)];
        bool[] assigned = new bool[cells.Length];
        Dictionary<ChunkId, long> chunkVersions = new Dictionary<ChunkId, long>();
        foreach (ChunkSnapshot chunk in snapshot.Chunks.OrderBy(item => item.Id))
        {
            if (!layout.Contains(chunk.Id)
                || chunk.WorldVersion != snapshot.Version
                || !chunkVersions.TryAdd(chunk.Id, chunk.ChunkVersion))
            {
                return Result<WorldState>.Failure(new DomainError(
                    "world.restore.chunk_invalid",
                    "A world chunk is duplicated or has inconsistent metadata."));
            }

            foreach (CellSnapshot cell in chunk.Cells)
            {
                if (!snapshot.Size.Contains(cell.Id)
                    || !materials.Contains(cell.State.MaterialId))
                {
                    return Result<WorldState>.Failure(
                        !snapshot.Size.Contains(cell.Id)
                            ? WorldErrors.CellOutOfBounds
                            : WorldErrors.UnknownMaterial);
                }

                int index = checked((cell.Id.Y * snapshot.Size.Width) + cell.Id.X);
                if (assigned[index])
                {
                    return Result<WorldState>.Failure(new DomainError(
                        "world.restore.cell_duplicate",
                        "The world snapshot contains the same cell more than once."));
                }

                assigned[index] = true;
                cells[index] = cell.State;
            }
        }

        if (assigned.Any(value => !value))
        {
            return Result<WorldState>.Failure(new DomainError(
                "world.restore.cell_missing",
                "The world snapshot is missing one or more cells."));
        }

        WorldState world = new WorldState(snapshot.Size, layout, materials, cells)
        {
            Version = snapshot.Version,
        };
        foreach (KeyValuePair<ChunkId, long> pair in chunkVersions)
        {
            world._chunkVersions[world.GetChunkIndex(pair.Key)] = pair.Value;
        }

        return Result<WorldState>.Success(world);
    }
}
}
