using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dig.Unity
{
    internal sealed class DigTerrainRenderSnapshot
    {
        private readonly HashSet<DigTerrainCellKey> _solidCells;
        private readonly HashSet<DigTerrainCellKey> _cutawayCells;
        private readonly HashSet<DigTerrainCellKey> _protectedCells;
        private readonly HashSet<DigTerrainChunkKey> _dirtyChunks;

        internal DigTerrainRenderSnapshot(
            int width,
            int height,
            int depth,
            int chunkSize,
            long version,
            IReadOnlyCollection<DigTerrainRenderChunk> chunks,
            IEnumerable<DigTerrainCellKey> solidCells,
            IEnumerable<DigTerrainCellKey> cutawayCells,
            IEnumerable<DigTerrainCellKey> protectedCells,
            IEnumerable<DigTerrainChunkKey> dirtyChunks)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (depth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(depth));
            }

            if (chunkSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(chunkSize));
            }

            if (chunks == null)
            {
                throw new ArgumentNullException(nameof(chunks));
            }

            Width = width;
            Height = height;
            Depth = depth;
            ChunkSize = chunkSize;
            Version = version;
            Chunks = new ReadOnlyCollection<DigTerrainRenderChunk>(
                new List<DigTerrainRenderChunk>(chunks));
            _solidCells = new HashSet<DigTerrainCellKey>(solidCells);
            _cutawayCells = new HashSet<DigTerrainCellKey>(cutawayCells);
            _protectedCells = new HashSet<DigTerrainCellKey>(protectedCells);
            _dirtyChunks = new HashSet<DigTerrainChunkKey>(dirtyChunks);
        }

        internal int Width { get; }
        internal int Height { get; }
        internal int Depth { get; }
        internal int ChunkSize { get; }
        internal long Version { get; }
        internal IReadOnlyList<DigTerrainRenderChunk> Chunks { get; }

        internal bool IsSolid(DigTerrainCellKey cell)
        {
            return _solidCells.Contains(cell);
        }

        internal bool IsCutaway(DigTerrainCellKey cell)
        {
            return _cutawayCells.Contains(cell);
        }

        internal bool IsProtected(DigTerrainCellKey cell)
        {
            return _protectedCells.Contains(cell);
        }

        internal bool IsRenderedSolid(DigTerrainCellKey cell)
        {
            return IsSolid(cell) && !IsCutaway(cell);
        }

        internal bool IsDirty(DigTerrainChunkKey chunk)
        {
            return _dirtyChunks.Contains(chunk);
        }
    }
}
