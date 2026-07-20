using System;
using System.Collections.Generic;
using Dig.Domain.World;

namespace Dig.Presentation.World
{
    public sealed class TerrainDepositDecorationPresenter
    {
        public TerrainDepositDecorationVolumeViewModel Present(
            TerrainDepositVolumeViewModel deposits)
        {
            if (deposits == null)
            {
                throw new ArgumentNullException(nameof(deposits));
            }

            List<TerrainDepositDecorationCellViewModel> cells =
                new List<TerrainDepositDecorationCellViewModel>();
            for (int index = 0; index < deposits.Cells.Count; index++)
            {
                TerrainDepositCellViewModel source = deposits.Cells[index];
                if (!source.IsVisible)
                {
                    continue;
                }

                cells.Add(Project(source));
            }

            cells.Sort(CompareCells);
            long version = CalculateVersion(cells);
            return new TerrainDepositDecorationVolumeViewModel(
                deposits.Width,
                deposits.Height,
                deposits.Depth,
                version,
                cells);
        }

        private static TerrainDepositDecorationCellViewModel Project(
            TerrainDepositCellViewModel source)
        {
            ulong seed = CalculateSeed(source.Cell, source.VisibleDepositId);
            byte variant = (byte)(seed & 3UL);
            byte rotation = (byte)((seed >> 2) & 3UL);
            sbyte offsetX = ResolveOffsetBand((byte)((seed >> 4) & 3UL));
            sbyte offsetY = ResolveOffsetBand((byte)((seed >> 6) & 3UL));
            byte scaleBand = ResolveScaleBand(source);

            return new TerrainDepositDecorationCellViewModel(
                source.Cell,
                source.VisibleDepositId,
                source.State,
                source.DamageBand,
                source.Connections,
                variant,
                rotation,
                scaleBand,
                offsetX,
                offsetY);
        }

        private static byte ResolveScaleBand(TerrainDepositCellViewModel source)
        {
            if (source.State != TerrainDepositVisualState.Damaged)
            {
                return TerrainDepositDecorationCellViewModel.MaximumScaleBand;
            }

            return source.DamageBand >= 3
                ? (byte)0
                : (byte)(3 - source.DamageBand);
        }

        private static sbyte ResolveOffsetBand(byte value)
        {
            switch (value)
            {
                case 0:
                    return -1;
                case 1:
                case 2:
                    return 0;
                default:
                    return 1;
            }
        }

        private static ulong CalculateSeed(SpatialCellId cell, string depositId)
        {
            const ulong offset = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            Mix(ref hash, (ulong)(uint)cell.X, prime);
            Mix(ref hash, (ulong)(uint)cell.Y, prime);
            Mix(ref hash, (ulong)(uint)cell.Z, prime);
            for (int index = 0; index < depositId.Length; index++)
            {
                Mix(ref hash, depositId[index], prime);
            }

            return hash;
        }

        private static long CalculateVersion(
            IReadOnlyList<TerrainDepositDecorationCellViewModel> cells)
        {
            if (cells.Count == 0)
            {
                return 0;
            }

            const ulong offset = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            for (int index = 0; index < cells.Count; index++)
            {
                TerrainDepositDecorationCellViewModel cell = cells[index];
                Mix(ref hash, (ulong)(uint)cell.Cell.X, prime);
                Mix(ref hash, (ulong)(uint)cell.Cell.Y, prime);
                Mix(ref hash, (ulong)(uint)cell.Cell.Z, prime);
                Mix(ref hash, (ulong)cell.State, prime);
                Mix(ref hash, cell.DamageBand, prime);
                Mix(ref hash, (byte)cell.Connections, prime);
                Mix(ref hash, cell.Variant, prime);
                Mix(ref hash, cell.RotationQuarterTurns, prime);
                Mix(ref hash, cell.ScaleBand, prime);
                Mix(ref hash, unchecked((byte)cell.OffsetBandX), prime);
                Mix(ref hash, unchecked((byte)cell.OffsetBandY), prime);
                for (int character = 0;
                    character < cell.VisibleDepositId.Length;
                    character++)
                {
                    Mix(ref hash, cell.VisibleDepositId[character], prime);
                }
            }

            return unchecked((long)(hash & (ulong)long.MaxValue));
        }

        private static int CompareCells(
            TerrainDepositDecorationCellViewModel left,
            TerrainDepositDecorationCellViewModel right)
        {
            int z = left.Cell.Z.CompareTo(right.Cell.Z);
            if (z != 0)
            {
                return z;
            }

            int y = left.Cell.Y.CompareTo(right.Cell.Y);
            return y != 0 ? y : left.Cell.X.CompareTo(right.Cell.X);
        }

        private static void Mix(ref ulong hash, ulong value, ulong prime)
        {
            hash ^= value;
            hash *= prime;
        }
    }
}
