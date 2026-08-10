using System;
using System.Collections.Generic;
using Dig.Presentation.World;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainRenderSnapshotBuilder
    {
        private readonly Dictionary<DigTerrainCellKey, ulong>
            _previousDepositDecorations =
                new Dictionary<DigTerrainCellKey, ulong>();

        private Dictionary<
            DigTerrainCellKey,
            TerrainDepositDecorationCellViewModel> BuildDepositDecorations(
                TerrainDepositDecorationVolumeViewModel? decorationVolume,
                int width,
                int height,
                int depth,
                int chunkSize,
                ISet<DigTerrainCellKey> solidCells,
                ISet<DigTerrainChunkKey> dirtyOrigins)
        {
            ValidateDepositDecorationVolume(
                decorationVolume,
                width,
                height,
                depth);
            Dictionary<DigTerrainCellKey, ulong> current =
                new Dictionary<DigTerrainCellKey, ulong>();
            Dictionary<
                DigTerrainCellKey,
                TerrainDepositDecorationCellViewModel> visible =
                    new Dictionary<
                        DigTerrainCellKey,
                        TerrainDepositDecorationCellViewModel>();
            if (decorationVolume != null)
            {
                for (int index = 0; index < decorationVolume.Cells.Count; index++)
                {
                    TerrainDepositDecorationCellViewModel source =
                        decorationVolume.Cells[index];
                    DigTerrainCellKey key = new DigTerrainCellKey(
                        source.Cell.X,
                        source.Cell.Y,
                        source.Cell.Z);
                    if (!solidCells.Contains(key))
                    {
                        continue;
                    }

                    ulong signature = CalculateDepositDecorationSignature(source);
                    current.Add(key, signature);
                    visible.Add(key, source);
                }
            }

            MarkChangedDepositDecorations(
                _previousDepositDecorations,
                current,
                chunkSize,
                dirtyOrigins);
            Replace(_previousDepositDecorations, current);
            return visible;
        }

        private static void MarkChangedDepositDecorations(
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

        private static ulong CalculateDepositDecorationSignature(
            TerrainDepositDecorationCellViewModel decoration)
        {
            const ulong offset = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            Mix(ref hash, (ulong)decoration.State, prime);
            Mix(ref hash, decoration.DamageBand, prime);
            Mix(ref hash, (byte)decoration.Connections, prime);
            Mix(ref hash, decoration.Variant, prime);
            Mix(ref hash, decoration.RotationQuarterTurns, prime);
            Mix(ref hash, decoration.ScaleBand, prime);
            Mix(ref hash, unchecked((byte)decoration.OffsetBandX), prime);
            Mix(ref hash, unchecked((byte)decoration.OffsetBandY), prime);
            for (int index = 0;
                index < decoration.VisibleDepositId.Length;
                index++)
            {
                Mix(ref hash, decoration.VisibleDepositId[index], prime);
            }

            return hash;
        }

        private static void ValidateDepositDecorationVolume(
            TerrainDepositDecorationVolumeViewModel? volume,
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
                    "Deposit decoration dimensions must match terrain.",
                    nameof(volume));
            }
        }
    }
}
