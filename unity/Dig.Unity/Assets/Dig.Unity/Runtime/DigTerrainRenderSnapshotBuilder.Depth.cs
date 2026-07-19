using System;
using System.Collections.Generic;
using Dig.Domain.World;
using Dig.Presentation.World;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainRenderSnapshotBuilder
    {
        private void AddDepthChunks(
            TerrainDepthVolumeViewModel depthVolume,
            int chunkSize,
            ICollection<DigTerrainRenderChunk> chunks,
            ISet<DigTerrainCellKey> solid,
            IDictionary<DigTerrainChunkKey, long> currentVersions,
            ISet<DigTerrainChunkKey> dirtyOrigins,
            bool layoutChanged)
        {
            Dictionary<DigTerrainChunkKey, List<DigTerrainRenderCell>> grouped =
                new Dictionary<DigTerrainChunkKey, List<DigTerrainRenderCell>>();
            for (int index = 0; index < depthVolume.SolidCells.Count; index++)
            {
                SpatialCellId source = depthVolume.SolidCells[index];
                DigTerrainCellKey key = new DigTerrainCellKey(
                    source.X,
                    source.Y,
                    source.Z);
                solid.Add(key);
                DigTerrainChunkKey chunk = ChunkForCell(key, chunkSize);
                if (!grouped.TryGetValue(
                        chunk,
                        out List<DigTerrainRenderCell>? cells))
                {
                    cells = new List<DigTerrainRenderCell>();
                    grouped.Add(chunk, cells);
                }

                cells.Add(new DigTerrainRenderCell(
                    key,
                    depthVolume.SolidMaterialId,
                    isSolid: true,
                    isExplored: true,
                    isDesignated: false,
                    depthVolume.Hardness,
                    damage: 0,
                    temperature: 20,
                    depthVolume.Version));
            }

            foreach (KeyValuePair<DigTerrainChunkKey, List<DigTerrainRenderCell>> pair
                in grouped)
            {
                pair.Value.Sort(CompareCells);
                long version = CalculateDepthChunkVersion(pair.Key, pair.Value);
                chunks.Add(new DigTerrainRenderChunk(pair.Key, version, pair.Value));
                TrackChunkVersion(
                    pair.Key,
                    version,
                    currentVersions,
                    dirtyOrigins,
                    layoutChanged);
            }
        }

        private static long CalculateDepthChunkVersion(
            DigTerrainChunkKey chunk,
            IReadOnlyList<DigTerrainRenderCell> cells)
        {
            const ulong offset = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            Mix(ref hash, (ulong)(uint)chunk.X, prime);
            Mix(ref hash, (ulong)(uint)chunk.Y, prime);
            Mix(ref hash, (ulong)(uint)chunk.Z, prime);
            for (int index = 0; index < cells.Count; index++)
            {
                DigTerrainRenderCell cell = cells[index];
                Mix(ref hash, (ulong)(uint)cell.Key.X, prime);
                Mix(ref hash, (ulong)(uint)cell.Key.Y, prime);
                Mix(ref hash, (ulong)(uint)cell.Key.Z, prime);
                Mix(ref hash, (ulong)(uint)cell.Hardness, prime);
                for (int character = 0; character < cell.MaterialId.Length; character++)
                {
                    Mix(ref hash, cell.MaterialId[character], prime);
                }
            }

            return unchecked((long)(hash & long.MaxValue));
        }

        private static long CombineVersion(long worldVersion, long depthVersion)
        {
            unchecked
            {
                ulong mixed = (ulong)worldVersion * 1099511628211UL;
                mixed ^= (ulong)depthVersion;
                return (long)(mixed & long.MaxValue);
            }
        }

        private static void ValidateDepthVolume(
            WorldViewModel world,
            TerrainDepthVolumeViewModel? depthVolume)
        {
            if (depthVolume != null
                && (depthVolume.Width != world.Width
                    || depthVolume.Height != world.Height))
            {
                throw new ArgumentException(
                    "Depth terrain dimensions must match the front world projection.",
                    nameof(depthVolume));
            }
        }
    }
}
