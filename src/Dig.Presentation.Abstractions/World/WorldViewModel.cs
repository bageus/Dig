using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace Dig.Presentation.World
{

public sealed class WorldViewModel
{
    public WorldViewModel(
        int width,
        int height,
        int depth,
        int chunkSize,
        long version,
        IReadOnlyCollection<WorldChunkViewModel> chunks)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (depth != Dig.Domain.World.WorldSize.RequiredDepth)
        {
            throw new ArgumentOutOfRangeException(nameof(depth));
        }

        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize));
        }

        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (chunks is null)
        {
            throw new ArgumentNullException(nameof(chunks));
        }

        Width = width;
        Height = height;
        Depth = depth;
        ChunkSize = chunkSize;
        Version = version;
        Chunks = new ReadOnlyCollection<WorldChunkViewModel>(chunks
            .OrderBy(chunk => chunk.Z)
            .ThenBy(chunk => chunk.Y)
            .ThenBy(chunk => chunk.X)
            .ToArray());
    }

    public int Width { get; }
    public int Height { get; }
    public int Depth { get; }
    public int ChunkSize { get; }
    public long Version { get; }
    public IReadOnlyList<WorldChunkViewModel> Chunks { get; }
}
}
