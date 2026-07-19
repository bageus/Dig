using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Presentation.World
{

public sealed class TerrainDepthVolumeViewModel
{
    public TerrainDepthVolumeViewModel(
        int width,
        int height,
        int depth,
        string solidMaterialId,
        int hardness,
        long version,
        IReadOnlyCollection<SpatialCellId> solidCells)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (depth <= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(depth));
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

        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (solidCells is null)
        {
            throw new ArgumentNullException(nameof(solidCells));
        }

        SpatialCellId[] ordered = solidCells.OrderBy(cell => cell).ToArray();
        for (int index = 0; index < ordered.Length; index++)
        {
            SpatialCellId cell = ordered[index];
            if (cell.X < 0
                || cell.Y < 0
                || cell.Z <= 0
                || cell.X >= width
                || cell.Y >= height
                || cell.Z >= depth)
            {
                throw new ArgumentOutOfRangeException(nameof(solidCells));
            }
        }

        Width = width;
        Height = height;
        Depth = depth;
        SolidMaterialId = solidMaterialId;
        Hardness = hardness;
        Version = version;
        SolidCells = new ReadOnlyCollection<SpatialCellId>(ordered);
    }

    public int Width { get; }
    public int Height { get; }
    public int Depth { get; }
    public string SolidMaterialId { get; }
    public int Hardness { get; }
    public long Version { get; }
    public IReadOnlyList<SpatialCellId> SolidCells { get; }
}

}
