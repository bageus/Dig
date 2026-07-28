using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Application.Saving
{
public sealed partial class SaveGameLoader
{
    private static WorldSnapshot BuildWorldSnapshot(
        WorldSaveData data,
        MaterialCatalog materials)
    {
        if (data is null || data.Chunks is null)
        {
            throw new InvalidOperationException("World save data is missing.");
        }

        WorldSize size = new WorldSize(data.Width, data.Height, data.Depth);
        ChunkLayout layout = new ChunkLayout(size, data.ChunkSize);
        List<ChunkSnapshot> chunks = new List<ChunkSnapshot>();
        foreach (WorldChunkSaveData savedChunk in data.Chunks
            .OrderBy(item => item.Z)
            .ThenBy(item => item.Y)
            .ThenBy(item => item.X))
        {
            if (savedChunk is null || savedChunk.Cells is null)
            {
                throw new InvalidOperationException("World chunk save data is missing.");
            }

            ChunkId chunkId = new ChunkId(savedChunk.X, savedChunk.Y, savedChunk.Z);
            List<CellSnapshot> cells = new List<CellSnapshot>();
            foreach (WorldCellSaveData savedCell in savedChunk.Cells
                .OrderBy(item => item.Z)
                .ThenBy(item => item.Y)
                .ThenBy(item => item.X))
            {
                if (savedCell is null
                    || !Enum.IsDefined(typeof(CellDesignation), savedCell.Designation)
                    || !Enum.IsDefined(
                        typeof(ExcavationCutPattern),
                        savedCell.ExcavationCutPattern)
                    || (savedCell.CompletedExcavationQuarters
                        & ~(int)ExcavationQuarter.All) != 0)
                {
                    throw new InvalidOperationException("World cell save data is invalid.");
                }

                MaterialId materialId = new MaterialId(savedCell.MaterialId);
                MaterialDefinition material = materials.Get(materialId)
                    ?? throw new InvalidOperationException(
                        $"Unknown saved material '{materialId}'.");
                MaterialId sourceMaterialId = string.IsNullOrWhiteSpace(
                        savedCell.ExcavationSourceMaterialId)
                    ? default
                    : new MaterialId(savedCell.ExcavationSourceMaterialId);
                CellState state = new CellState(
                    materialId,
                    (CellDesignation)savedCell.Designation,
                    savedCell.IsExplored,
                    savedCell.Damage,
                    savedCell.Temperature,
                    (ExcavationQuarter)savedCell.CompletedExcavationQuarters,
                    (ExcavationCutPattern)savedCell.ExcavationCutPattern,
                    sourceMaterialId);
                cells.Add(new CellSnapshot(
                    new CellId(savedCell.X, savedCell.Y, savedCell.Z),
                    state,
                    material.IsSolid,
                    material.Hardness,
                    data.Version));
            }

            chunks.Add(new ChunkSnapshot(
                chunkId,
                layout.GetBounds(chunkId),
                data.Version,
                savedChunk.Version,
                cells));
        }

        return new WorldSnapshot(
            size,
            data.ChunkSize,
            data.Version,
            new ReadOnlyCollection<ChunkSnapshot>(chunks));
    }


}
}
