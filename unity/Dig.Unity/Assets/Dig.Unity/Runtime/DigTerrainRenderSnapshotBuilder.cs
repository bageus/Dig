using System;
using System.Collections.Generic;
using Dig.Presentation.World;
using UnityEngine;

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
            TerrainDepthVolumeViewModel? depthVolume,
            TerrainDepositVolumeViewModel? depositVolume,
            IEnumerable<Vector2Int> cutawayCells,
            IEnumerable<Vector2Int> protectedCells)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            ValidateDepthVolume(world, depthVolume);
            int depth = depthVolume?.Depth ?? 1;
            List<DigTerrainRenderChunk> chunks = new List<DigTerrainRenderChunk>();
            HashSet<DigTerrainCellKey> solid = new HashSet<DigTerrainCellKey>();
            Dictionary<DigTerrainChunkKey, long> currentVersions =
                new Dictionary<DigTerrainChunkKey, long>();
            HashSet<DigTerrainChunkKey> dirtyOrigins =
                new HashSet<DigTerrainChunkKey>();

            bool layoutChanged = _initialized
                && (_chunkSize != world.ChunkSize || _depth != depth);
            AddFrontChunks(
                world,
                chunks,
                solid,
                currentVersions,
                dirtyOrigins,
                layoutChanged);
            if (depthVolume != null)
            {
                AddDepthChunks(
                    depthVolume,
                    world.ChunkSize,
                    chunks,
                    solid,
                    currentVersions,
                    dirtyOrigins,
                    layoutChanged);
            }

            foreach (DigTerrainChunkKey previous in _chunkVersions.Keys)
            {
                if (!currentVersions.ContainsKey(previous))
                {
                    dirtyOrigins.Add(previous);
                }
            }

            HashSet<DigTerrainCellKey> currentCutaway = ToDepthZero(cutawayCells);
            HashSet<DigTerrainCellKey> currentProtected = ToDepthZero(protectedCells);
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
            Dictionary<DigTerrainCellKey, TerrainDepositCellViewModel>
                visibleDeposits = BuildVisibleDeposits(
                    depositVolume,
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
                    depthVolume?.Version ?? 0,
                    depositVolume?.Version ?? 0),
                chunks,
                solid,
                currentCutaway,
                currentProtected,
                dirty,
                visibleDeposits);
        }

        internal void Invalidate()
        {
            _initialized = false;
        }

        private void AddFrontChunks(
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
                DigTerrainChunkKey key = new DigTerrainChunkKey(source.X, source.Y, 0);
                List<DigTerrainRenderCell> cells = new List<DigTerrainRenderCell>(
                    source.Cells.Count);
                for (int cellIndex = 0; cellIndex < source.Cells.Count; cellIndex++)
                {
                    DigTerrainRenderCell cell = DigTerrainRenderCell.FromWorld(
                        source.Cells[cellIndex],
                        z: 0);
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

        private static HashSet<DigTerrainCellKey> ToDepthZero(
            IEnumerable<Vector2Int> cells)
        {
            HashSet<DigTerrainCellKey> result = new HashSet<DigTerrainCellKey>();
            foreach (Vector2Int cell in cells)
            {
                result.Add(new DigTerrainCellKey(cell.x, cell.y, 0));
            }

            return result;
        }

        private static void MarkChangedCells(
            HashSet<DigTerrainCellKey> previous,
            HashSet<DigTerrainCellKey> current,
            int chunkSize,
            HashSet<DigTerrainChunkKey> dirty)
        {
            foreach (DigTerrainCellKey cell in previous)
            {
                if (!current.Contains(cell))
                {
                    dirty.Add(ChunkForCell(cell, chunkSize));
                }
            }

            foreach (DigTerrainCellKey cell in current)
            {
                if (!previous.Contains(cell))
                {
                    dirty.Add(ChunkForCell(cell, chunkSize));
                }
            }
        }

        private static DigTerrainChunkKey ChunkForCell(
            DigTerrainCellKey cell,
            int chunkSize)
        {
            return new DigTerrainChunkKey(
                FloorDivide(cell.X, chunkSize),
                FloorDivide(cell.Y, chunkSize),
                cell.Z);
        }

        private static int FloorDivide(int value, int divisor)
        {
            int quotient = value / divisor;
            int remainder = value % divisor;
            return remainder < 0 ? quotient - 1 : quotient;
        }

        private static void MarkChunkAndNeighbours(
            DigTerrainChunkKey chunk,
            HashSet<DigTerrainChunkKey> dirty)
        {
            dirty.Add(chunk);
            dirty.Add(chunk.Offset(-1, 0, 0));
            dirty.Add(chunk.Offset(1, 0, 0));
            dirty.Add(chunk.Offset(0, -1, 0));
            dirty.Add(chunk.Offset(0, 1, 0));
            dirty.Add(chunk.Offset(0, 0, -1));
            dirty.Add(chunk.Offset(0, 0, 1));
        }

        private static int CompareCells(
            DigTerrainRenderCell left,
            DigTerrainRenderCell right)
        {
            int z = left.Key.Z.CompareTo(right.Key.Z);
            if (z != 0)
            {
                return z;
            }

            int y = left.Key.Y.CompareTo(right.Key.Y);
            return y != 0 ? y : left.Key.X.CompareTo(right.Key.X);
        }

        private static int CompareChunks(
            DigTerrainRenderChunk left,
            DigTerrainRenderChunk right)
        {
            int z = left.Key.Z.CompareTo(right.Key.Z);
            if (z != 0)
            {
                return z;
            }

            int y = left.Key.Y.CompareTo(right.Key.Y);
            return y != 0 ? y : left.Key.X.CompareTo(right.Key.X);
        }

        private static void Mix(ref ulong hash, ulong value, ulong prime)
        {
            hash ^= value;
            hash *= prime;
        }

        private static void Replace<TKey, TValue>(
            Dictionary<TKey, TValue> target,
            Dictionary<TKey, TValue> source)
            where TKey : notnull
        {
            target.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in source)
            {
                target.Add(pair.Key, pair.Value);
            }
        }

        private static void Replace<T>(HashSet<T> target, HashSet<T> source)
        {
            target.Clear();
            target.UnionWith(source);
        }
    }
}
