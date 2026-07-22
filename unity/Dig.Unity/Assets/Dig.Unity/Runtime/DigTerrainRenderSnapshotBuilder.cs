using System;
using System.Collections.Generic;
using Dig.Presentation.World;
using Dig.Domain.World;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainRenderSnapshotBuilder
    {
        private readonly Dictionary<DigTerrainChunkKey, long> _chunkVersions =
            new Dictionary<DigTerrainChunkKey, long>();
        private readonly HashSet<DigTerrainCellKey> _previousCutaway =
            new HashSet<DigTerrainCellKey>();
        private readonly HashSet<DigTerrainCellKey> _previousProtected =
            new HashSet<DigTerrainCellKey>();
        private bool _initialized;
        private int _chunkSize;
        private int _depth;

        internal DigTerrainRenderSnapshot Build(
            WorldViewModel world,
            TerrainDepositDecorationVolumeViewModel? depositDecorations,
            IEnumerable<CellId> cutawayCells,
            IEnumerable<CellId> protectedCells)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            int depth = world.Depth;
            List<DigTerrainRenderChunk> chunks = new List<DigTerrainRenderChunk>();
            HashSet<DigTerrainCellKey> solid = new HashSet<DigTerrainCellKey>();
            Dictionary<DigTerrainChunkKey, long> currentVersions =
                new Dictionary<DigTerrainChunkKey, long>();
            HashSet<DigTerrainChunkKey> dirtyOrigins =
                new HashSet<DigTerrainChunkKey>();

            bool layoutChanged = _initialized
                && (_chunkSize != world.ChunkSize || _depth != depth);
            AddWorldChunks(
                world,
                chunks,
                solid,
                currentVersions,
                dirtyOrigins,
                layoutChanged);

            foreach (DigTerrainChunkKey previous in _chunkVersions.Keys)
            {
                if (!currentVersions.ContainsKey(previous))
                {
                    dirtyOrigins.Add(previous);
                }
            }

            HashSet<DigTerrainCellKey> currentCutaway = ToCellKeys(cutawayCells);
            HashSet<DigTerrainCellKey> currentProtected = ToCellKeys(protectedCells);
            MarkChangedCells(
                _previousCutaway,
                currentCutaway,
                world.ChunkSize,
                dirtyOrigins);
            MarkChangedCells(
                _previousProtected,
                currentProtected,
                world.ChunkSize,
                dirtyOrigins);
            Dictionary<
                DigTerrainCellKey,
                TerrainDepositDecorationCellViewModel> visibleDecorations =
                    BuildDepositDecorations(
                        depositDecorations,
                        world.Width,
                        world.Height,
                        depth,
                        world.ChunkSize,
                        solid,
                        dirtyOrigins);

            HashSet<DigTerrainChunkKey> dirty = new HashSet<DigTerrainChunkKey>();
            foreach (DigTerrainChunkKey origin in dirtyOrigins)
            {
                MarkChunkAndNeighbours(origin, dirty);
            }

            chunks.Sort(CompareChunks);
            Replace(_chunkVersions, currentVersions);
            Replace(_previousCutaway, currentCutaway);
            Replace(_previousProtected, currentProtected);
            _chunkSize = world.ChunkSize;
            _depth = depth;
            _initialized = true;

            return new DigTerrainRenderSnapshot(
                world.Width,
                world.Height,
                depth,
                world.ChunkSize,
                CombineVersion(
                    world.Version,
                    depthVersion: 0,
                    depositDecorations?.Version ?? 0),
                chunks,
                solid,
                currentCutaway,
                currentProtected,
                dirty,
                visibleDecorations);
        }

        private static long CombineVersion(
            long worldVersion,
            long depthVersion,
            long depositVersion)
        {
            unchecked
            {
                ulong mixed = (ulong)worldVersion * 1099511628211UL;
                mixed ^= (ulong)depthVersion;
                mixed *= 1099511628211UL;
                mixed ^= (ulong)depositVersion;
                return (long)(mixed & (ulong)long.MaxValue);
            }
        }

        internal void Invalidate()
        {
            _initialized = false;
        }

        private void AddWorldChunks(
            WorldViewModel world,
            ICollection<DigTerrainRenderChunk> chunks,
            ISet<DigTerrainCellKey> solid,
            IDictionary<DigTerrainChunkKey, long> currentVersions,
            ISet<DigTerrainChunkKey> dirtyOrigins,
            bool layoutChanged)
        {
            for (int index = 0; index < world.Chunks.Count; index++)
            {
                WorldChunkViewModel source = world.Chunks[index];
                DigTerrainChunkKey key = new DigTerrainChunkKey(source.X, source.Y, source.Z);
                List<DigTerrainRenderCell> cells = new List<DigTerrainRenderCell>(
                    source.Cells.Count);
                for (int cellIndex = 0; cellIndex < source.Cells.Count; cellIndex++)
                {
                    WorldCellViewModel sourceCell = source.Cells[cellIndex];
                    DigTerrainRenderCell cell = DigTerrainRenderCell.FromWorld(
                        sourceCell,
                        sourceCell.Z);
                    cells.Add(cell);
                    if (cell.IsSolid)
                    {
                        solid.Add(cell.Key);
                    }
                }

                cells.Sort(CompareCells);
                chunks.Add(new DigTerrainRenderChunk(key, source.Version, cells));
                TrackChunkVersion(
                    key,
                    source.Version,
                    currentVersions,
                    dirtyOrigins,
                    layoutChanged);
            }
        }

        private void TrackChunkVersion(
            DigTerrainChunkKey key,
            long version,
            IDictionary<DigTerrainChunkKey, long> currentVersions,
            ISet<DigTerrainChunkKey> dirtyOrigins,
            bool layoutChanged)
        {
            currentVersions.Add(key, version);
            if (!_initialized
                || layoutChanged
                || !_chunkVersions.TryGetValue(key, out long previousVersion)
                || previousVersion != version)
            {
                dirtyOrigins.Add(key);
            }
        }

        private static HashSet<DigTerrainCellKey> ToCellKeys(
            IEnumerable<CellId> cells)
        {
            HashSet<DigTerrainCellKey> result = new HashSet<DigTerrainCellKey>();
            foreach (CellId cell in cells)
            {
                result.Add(new DigTerrainCellKey(cell.X, cell.Y, cell.Z));
            }

            return result;
        }
    }
}
