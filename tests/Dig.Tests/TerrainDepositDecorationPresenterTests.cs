using System;
using Dig.Domain.World;
using Dig.Presentation.World;
using Xunit;

namespace Dig.Tests
{
    public sealed class TerrainDepositDecorationPresenterTests
    {
        [Fact]
        public void Hidden_and_depleted_deposits_do_not_create_decorations()
        {
            TerrainDepositDecorationPresenter presenter =
                new TerrainDepositDecorationPresenter();
            TerrainDepositVolumeViewModel deposits = Volume(
                Cell(1, 1, 1, TerrainDepositVisualState.Hidden, string.Empty),
                Cell(2, 1, 1, TerrainDepositVisualState.Depleted, string.Empty),
                Cell(
                    3,
                    1,
                    1,
                    TerrainDepositVisualState.Revealed,
                    "deposit.iron_ore"));

            TerrainDepositDecorationVolumeViewModel result =
                presenter.Present(deposits);

            TerrainDepositDecorationCellViewModel cell = Assert.Single(result.Cells);
            Assert.Equal(new SpatialCellId(3, 1, 1), cell.Cell);
            Assert.Equal("deposit.iron_ore", cell.VisibleDepositId);
        }

        [Fact]
        public void Layout_is_stable_for_reordered_input()
        {
            TerrainDepositDecorationPresenter presenter =
                new TerrainDepositDecorationPresenter();
            TerrainDepositCellViewModel left = Cell(
                4,
                2,
                1,
                TerrainDepositVisualState.Revealed,
                "deposit.crystal_ore",
                connections: TerrainDepositConnection.NegativeX);
            TerrainDepositCellViewModel right = Cell(
                3,
                2,
                1,
                TerrainDepositVisualState.Revealed,
                "deposit.crystal_ore",
                connections: TerrainDepositConnection.PositiveX);

            TerrainDepositDecorationVolumeViewModel ordered =
                presenter.Present(Volume(left, right));
            TerrainDepositDecorationVolumeViewModel reordered =
                presenter.Present(Volume(right, left));

            Assert.Equal(ordered.Version, reordered.Version);
            Assert.Equal(ordered.Cells.Count, reordered.Cells.Count);
            for (int index = 0; index < ordered.Cells.Count; index++)
            {
                AssertLayoutEqual(ordered.Cells[index], reordered.Cells[index]);
            }
        }

        [Fact]
        public void Damage_reduces_scale_without_changing_identity_variant_or_rotation()
        {
            TerrainDepositDecorationPresenter presenter =
                new TerrainDepositDecorationPresenter();
            TerrainDepositDecorationCellViewModel revealed = Assert.Single(
                presenter.Present(Volume(Cell(
                    2,
                    3,
                    2,
                    TerrainDepositVisualState.Revealed,
                    "deposit.gold_ore"))).Cells);
            TerrainDepositDecorationCellViewModel damaged = Assert.Single(
                presenter.Present(Volume(Cell(
                    2,
                    3,
                    2,
                    TerrainDepositVisualState.Damaged,
                    "deposit.gold_ore",
                    damageBand: 2))).Cells);

            Assert.Equal(revealed.Variant, damaged.Variant);
            Assert.Equal(revealed.RotationQuarterTurns, damaged.RotationQuarterTurns);
            Assert.Equal(revealed.OffsetBandX, damaged.OffsetBandX);
            Assert.Equal(revealed.OffsetBandY, damaged.OffsetBandY);
            Assert.Equal(3, revealed.ScaleBand);
            Assert.Equal(1, damaged.ScaleBand);
            Assert.NotEqual(
                presenter.Present(Volume(Cell(
                    2,
                    3,
                    2,
                    TerrainDepositVisualState.Revealed,
                    "deposit.gold_ore"))).Version,
                presenter.Present(Volume(Cell(
                    2,
                    3,
                    2,
                    TerrainDepositVisualState.Damaged,
                    "deposit.gold_ore",
                    damageBand: 2))).Version);
        }

        [Fact]
        public void Layout_fields_and_connector_budget_are_bounded()
        {
            TerrainDepositDecorationPresenter presenter =
                new TerrainDepositDecorationPresenter();
            TerrainDepositDecorationCellViewModel cell = Assert.Single(
                presenter.Present(Volume(Cell(
                    5,
                    4,
                    3,
                    TerrainDepositVisualState.Damaged,
                    "deposit.coal",
                    damageBand: 3,
                    connections: TerrainDepositConnection.NegativeX
                        | TerrainDepositConnection.PositiveX
                        | TerrainDepositConnection.NegativeY
                        | TerrainDepositConnection.PositiveY
                        | TerrainDepositConnection.NegativeZ
                        | TerrainDepositConnection.PositiveZ))).Cells);

            Assert.InRange(cell.Variant, (byte)0, (byte)3);
            Assert.InRange(cell.RotationQuarterTurns, (byte)0, (byte)3);
            Assert.InRange(cell.ScaleBand, (byte)0, (byte)3);
            Assert.InRange(cell.OffsetBandX, (sbyte)-1, (sbyte)1);
            Assert.InRange(cell.OffsetBandY, (sbyte)-1, (sbyte)1);
            Assert.Equal(2, TerrainDepositDecorationCellViewModel.MaximumConnectorsPerFace);
        }

        [Fact]
        public void Empty_layout_preserves_dimensions_and_zero_version()
        {
            TerrainDepositDecorationPresenter presenter =
                new TerrainDepositDecorationPresenter();

            TerrainDepositDecorationVolumeViewModel result = presenter.Present(
                TerrainDepositVolumeViewModel.Empty(9, 7, 4));

            Assert.Equal(9, result.Width);
            Assert.Equal(7, result.Height);
            Assert.Equal(4, result.Depth);
            Assert.Equal(0, result.Version);
            Assert.Empty(result.Cells);
        }

        private static void AssertLayoutEqual(
            TerrainDepositDecorationCellViewModel expected,
            TerrainDepositDecorationCellViewModel actual)
        {
            Assert.Equal(expected.Cell, actual.Cell);
            Assert.Equal(expected.VisibleDepositId, actual.VisibleDepositId);
            Assert.Equal(expected.State, actual.State);
            Assert.Equal(expected.DamageBand, actual.DamageBand);
            Assert.Equal(expected.Connections, actual.Connections);
            Assert.Equal(expected.Variant, actual.Variant);
            Assert.Equal(expected.RotationQuarterTurns, actual.RotationQuarterTurns);
            Assert.Equal(expected.ScaleBand, actual.ScaleBand);
            Assert.Equal(expected.OffsetBandX, actual.OffsetBandX);
            Assert.Equal(expected.OffsetBandY, actual.OffsetBandY);
        }

        private static TerrainDepositVolumeViewModel Volume(
            params TerrainDepositCellViewModel[] cells)
        {
            return new TerrainDepositVolumeViewModel(
                width: 8,
                height: 8,
                depth: 4,
                version: 1,
                cells);
        }

        private static TerrainDepositCellViewModel Cell(
            int x,
            int y,
            int z,
            TerrainDepositVisualState state,
            string id,
            byte damageBand = 0,
            TerrainDepositConnection connections = TerrainDepositConnection.None)
        {
            return new TerrainDepositCellViewModel(
                new SpatialCellId(x, y, z),
                state,
                id,
                damageBand,
                connections,
                sourceVersion: 1);
        }
    }
}
