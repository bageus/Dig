using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.World;

namespace Dig.Presentation.World
{
    public sealed class TerrainDepositDecorationCellViewModel
    {
        public const byte MaximumVariant = 3;
        public const byte MaximumRotationQuarterTurns = 3;
        public const byte MaximumScaleBand = 3;
        public const sbyte MinimumOffsetBand = -1;
        public const sbyte MaximumOffsetBand = 1;
        public const byte MaximumConnectorsPerFace = 2;

        public TerrainDepositDecorationCellViewModel(
            CellId cell,
            string visibleDepositId,
            TerrainDepositVisualState state,
            byte damageBand,
            TerrainDepositConnection connections,
            byte variant,
            byte rotationQuarterTurns,
            byte scaleBand,
            sbyte offsetBandX,
            sbyte offsetBandY)
        {
            if (string.IsNullOrWhiteSpace(visibleDepositId))
            {
                throw new ArgumentException(
                    "A deposit decoration requires a visible deposit id.",
                    nameof(visibleDepositId));
            }

            if (state != TerrainDepositVisualState.Revealed
                && state != TerrainDepositVisualState.Damaged)
            {
                throw new ArgumentOutOfRangeException(nameof(state));
            }

            if (damageBand > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(damageBand));
            }

            if (state != TerrainDepositVisualState.Damaged && damageBand != 0)
            {
                throw new ArgumentException(
                    "Only damaged deposit decorations may expose a damage band.",
                    nameof(damageBand));
            }

            if (variant > MaximumVariant)
            {
                throw new ArgumentOutOfRangeException(nameof(variant));
            }

            if (rotationQuarterTurns > MaximumRotationQuarterTurns)
            {
                throw new ArgumentOutOfRangeException(nameof(rotationQuarterTurns));
            }

            if (scaleBand > MaximumScaleBand)
            {
                throw new ArgumentOutOfRangeException(nameof(scaleBand));
            }

            ValidateOffsetBand(offsetBandX, nameof(offsetBandX));
            ValidateOffsetBand(offsetBandY, nameof(offsetBandY));

            Cell = cell;
            VisibleDepositId = visibleDepositId;
            State = state;
            DamageBand = damageBand;
            Connections = connections;
            Variant = variant;
            RotationQuarterTurns = rotationQuarterTurns;
            ScaleBand = scaleBand;
            OffsetBandX = offsetBandX;
            OffsetBandY = offsetBandY;
        }

        public CellId Cell { get; }

        public string VisibleDepositId { get; }

        public TerrainDepositVisualState State { get; }

        public byte DamageBand { get; }

        public TerrainDepositConnection Connections { get; }

        public byte Variant { get; }

        public byte RotationQuarterTurns { get; }

        public byte ScaleBand { get; }

        public sbyte OffsetBandX { get; }

        public sbyte OffsetBandY { get; }

        private static void ValidateOffsetBand(sbyte value, string parameterName)
        {
            if (value < MinimumOffsetBand || value > MaximumOffsetBand)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }

    public sealed class TerrainDepositDecorationVolumeViewModel
    {
        public TerrainDepositDecorationVolumeViewModel(
            int width,
            int height,
            int depth,
            long version,
            IReadOnlyCollection<TerrainDepositDecorationCellViewModel> cells)
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

            if (version < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(version));
            }

            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            Width = width;
            Height = height;
            Depth = depth;
            Version = version;
            Cells = new ReadOnlyCollection<TerrainDepositDecorationCellViewModel>(
                new List<TerrainDepositDecorationCellViewModel>(cells));
        }

        public int Width { get; }

        public int Height { get; }

        public int Depth { get; }

        public long Version { get; }

        public IReadOnlyList<TerrainDepositDecorationCellViewModel> Cells { get; }

        public static TerrainDepositDecorationVolumeViewModel Empty(
            int width,
            int height,
            int depth)
        {
            return new TerrainDepositDecorationVolumeViewModel(
                width,
                height,
                depth,
                version: 0,
                Array.Empty<TerrainDepositDecorationCellViewModel>());
        }
    }
}
