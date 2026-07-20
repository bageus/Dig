using System;
using System.Collections.Generic;
using Dig.Presentation.World;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainRenderSnapshotBuilder
    {
        private readonly Dictionary<DigTerrainCellKey, ulong>
            _previousDepositVisuals =
                new Dictionary<DigTerrainCellKey, ulong>();

        private Dictionary<DigTerrainCellKey, TerrainDepositCellViewModel>
            BuildVisibleDeposits(
                TerrainDepositVolumeViewModel? depositVolume,
                int width,
                int height,
                int depth,
                int chunkSize,
                ISet<DigTerrainCellKey> solidCells,
                ISet<DigTerrainChunkKey> dirtyOrigins)
        {
            ValidateDepositVolume(
                depositVolume,
                width,
                height,
                depth);
            Dictionary<DigTerrainCellKey, ulong> current =
                new Dictionary<DigTerrainCellKey, ulong>();
            Dictionary<DigTerrainCellKey, TerrainDepositCellViewModel> visible =
                new Dictionary<DigTerrainCellKey, TerrainDepositCellViewModel>();
            if (depositVolume != null)
            {
                for (int index = 0; index < depositVolume.Cells.Count; index++)
                {
                    TerrainDepositCellViewModel source =
                        depositVolume.Cells[index];
                    if (!source.IsVisible)
                    {
                        continue;
                    }

                    DigTerrainCellKey key = new DigTerrainCellKey(
                        source.Cell.X,
                        source.Cell.Y,
                        source.Cell.Z);
                    ulong signature = CalculateDepositVisualSignature(source);
                    current.Add(key, signature);
                    if (solidCells.Contains(key))
                    {
                        visible.Add(key, source);
                    }
                }
            }

            MarkChangedDepositVisuals(
                _previousDepositVisuals,
                current,
                chunkSize,
                dirtyOrigins);
            Replace(_previousDepositVisuals, current);
            return visible;
        }

        private static void MarkChangedDepositVisuals(
            IReadOnlyDictionary<DigTerrainCellKey, ulong> previous,
            IReadOnlyDictionary<DigTerrainCellKey, ulong> current,
            int chunkSize,
            ISet<DigTerrainChunkKey> dirty)
        {
            foreach (KeyValuePair<DigTerrainCellKey, ulong> pair in previous)
            {
                if (!current.TryGetValue(
                        pair.Key,
                        out ulong currentSignature)
                    || currentSignature != pair.Value)
                {
                    dirty.Add(ChunkForCell(pair.Key, chunkSize));
                }
            }

            foreach (KeyValuePair<DigTerrainCellKey, ulong> pair in current)
            {
                if (!previous.TryGetValue(
                        pair.Key,
                        out ulong previousSignature)
                    || previousSignature != pair.Value)
                {
                    dirty.Add(ChunkForCell(pair.Key, chunkSize));
                }
            }
        }

        private static ulong CalculateDepositVisualSignature(
            TerrainDepositCellViewModel deposit)
        {
            const ulong offset = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            Mix(ref hash, (ulong)deposit.State, prime);
            Mix(ref hash, deposit.DamageBand, prime);
            Mix(ref hash, (byte)deposit.Connections, prime);
            for (int index = 0; index < deposit.VisibleDepositId.Length; index++)
            {
                Mix(ref hash, deposit.VisibleDepositId[index], prime);
            }

            return hash;
        }

        private static void ValidateDepositVolume(
            TerrainDepositVolumeViewModel? volume,
            int width,
            int height,
            int depth)
        {
            if (volume != null
                && (volume.Width != width
                    || volume.Height != height
                    || volume.Depth != depth))
            {
                throw new ArgumentException(
                    "Deposit presentation dimensions must match terrain.",
                    nameof(volume));
            }
        }
    }
}
