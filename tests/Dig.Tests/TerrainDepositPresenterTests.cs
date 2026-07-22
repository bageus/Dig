using System;
using Dig.Domain.World;
using Dig.Presentation.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TerrainDepositPresenterTests
{
    [Fact]
    public void Hidden_deposit_does_not_expose_visual_identity_or_connections()
    {
        TerrainDepositPresenter presenter = new TerrainDepositPresenter();

        TerrainDepositVolumeViewModel result = presenter.Present(
            width: 8,
            height: 8,
            depth: 4,
            new[]
            {
                Deposit(
                    new CellId(2, 3, 1),
                    "deposit.iron_ore",
                    revealed: false,
                    remaining: 4,
                    maximum: 4),
            });

        TerrainDepositCellViewModel cell = Assert.Single(result.Cells);
        Assert.Equal(TerrainDepositVisualState.Hidden, cell.State);
        Assert.Empty(cell.VisibleDepositId);
        Assert.Equal(0, cell.DamageBand);
        Assert.Equal(TerrainDepositConnection.None, cell.Connections);
        Assert.False(cell.IsVisible);
    }

    [Fact]
    public void Presenter_projects_revealed_damaged_and_depleted_states()
    {
        TerrainDepositPresenter presenter = new TerrainDepositPresenter();

        TerrainDepositVolumeViewModel result = presenter.Present(
            width: 8,
            height: 8,
            depth: 4,
            new[]
            {
                Deposit(
                    new CellId(1, 2, 1),
                    "deposit.iron_ore",
                    revealed: true,
                    remaining: 9,
                    maximum: 9),
                Deposit(
                    new CellId(3, 2, 1),
                    "deposit.gold_ore",
                    revealed: true,
                    remaining: 4,
                    maximum: 10),
                Deposit(
                    new CellId(5, 2, 1),
                    "deposit.coal",
                    revealed: true,
                    remaining: 0,
                    maximum: 7),
            });

        Assert.Equal(TerrainDepositVisualState.Revealed, result.Cells[0].State);
        Assert.Equal("deposit.iron_ore", result.Cells[0].VisibleDepositId);
        Assert.Equal(0, result.Cells[0].DamageBand);

        Assert.Equal(TerrainDepositVisualState.Damaged, result.Cells[1].State);
        Assert.Equal("deposit.gold_ore", result.Cells[1].VisibleDepositId);
        Assert.Equal(2, result.Cells[1].DamageBand);

        Assert.Equal(TerrainDepositVisualState.Depleted, result.Cells[2].State);
        Assert.Empty(result.Cells[2].VisibleDepositId);
        Assert.Equal(0, result.Cells[2].DamageBand);
        Assert.False(result.Cells[2].IsVisible);
    }

    [Fact]
    public void Connections_include_only_visible_neighbours_with_the_same_id()
    {
        TerrainDepositPresenter presenter = new TerrainDepositPresenter();
        CellId center = new CellId(3, 3, 2);

        TerrainDepositVolumeViewModel result = presenter.Present(
            width: 8,
            height: 8,
            depth: 4,
            new[]
            {
                Deposit(center, "deposit.crystal_ore", true, 5, 5),
                Deposit(
                    new CellId(2, 3, 2),
                    "deposit.crystal_ore",
                    true,
                    5,
                    5),
                Deposit(
                    new CellId(4, 3, 2),
                    "deposit.gold_ore",
                    true,
                    5,
                    5),
                Deposit(
                    new CellId(3, 3, 3),
                    "deposit.crystal_ore",
                    false,
                    5,
                    5),
                Deposit(
                    new CellId(3, 4, 2),
                    "deposit.crystal_ore",
                    true,
                    2,
                    5),
            });

        TerrainDepositCellViewModel projected = Find(result, center);
        Assert.Equal(
            TerrainDepositConnection.NegativeX
                | TerrainDepositConnection.PositiveY,
            projected.Connections);
    }

    [Fact]
    public void Projection_is_deterministic_and_hidden_type_does_not_change_visual_version()
    {
        TerrainDepositPresenter presenter = new TerrainDepositPresenter();
        TerrainDepositPresentationInput visible = Deposit(
            new CellId(2, 2, 1),
            "deposit.stone",
            true,
            3,
            5);
        TerrainDepositPresentationInput hiddenIron = Deposit(
            new CellId(4, 2, 1),
            "deposit.iron_ore",
            false,
            5,
            5);
        TerrainDepositPresentationInput hiddenGold = Deposit(
            new CellId(4, 2, 1),
            "deposit.gold_ore",
            false,
            5,
            5);

        TerrainDepositVolumeViewModel left = presenter.Present(
            8,
            8,
            4,
            new[] { visible, hiddenIron });
        TerrainDepositVolumeViewModel reordered = presenter.Present(
            8,
            8,
            4,
            new[] { hiddenIron, visible });
        TerrainDepositVolumeViewModel hiddenTypeChanged = presenter.Present(
            8,
            8,
            4,
            new[] { visible, hiddenGold });

        Assert.Equal(left.Version, reordered.Version);
        Assert.Equal(left.Version, hiddenTypeChanged.Version);
        Assert.Equal(left.Cells[0].Cell, reordered.Cells[0].Cell);
        Assert.Equal(left.Cells[1].Cell, reordered.Cells[1].Cell);
    }

    [Fact]
    public void Presenter_rejects_duplicate_and_out_of_bounds_cells()
    {
        TerrainDepositPresenter presenter = new TerrainDepositPresenter();
        TerrainDepositPresentationInput deposit = Deposit(
            new CellId(1, 1, 1),
            "deposit.coal",
            true,
            2,
            2);

        Assert.Throws<ArgumentException>(() => presenter.Present(
            4,
            4,
            4,
            new[] { deposit, deposit }));
        Assert.Throws<ArgumentOutOfRangeException>(() => presenter.Present(
            4,
            4,
            4,
            new[]
            {
                Deposit(
                    new CellId(4, 1, 1),
                    "deposit.coal",
                    true,
                    2,
                    2),
            }));
    }

    [Fact]
    public void Empty_projection_preserves_volume_dimensions()
    {
        TerrainDepositVolumeViewModel result =
            TerrainDepositVolumeViewModel.Empty(10, 7, 4);

        Assert.Equal(10, result.Width);
        Assert.Equal(7, result.Height);
        Assert.Equal(4, result.Depth);
        Assert.Equal(0, result.Version);
        Assert.Empty(result.Cells);
    }

    private static TerrainDepositCellViewModel Find(
        TerrainDepositVolumeViewModel volume,
        CellId cell)
    {
        for (int index = 0; index < volume.Cells.Count; index++)
        {
            if (volume.Cells[index].Cell == cell)
            {
                return volume.Cells[index];
            }
        }

        throw new InvalidOperationException($"Deposit cell '{cell}' was not projected.");
    }

    private static TerrainDepositPresentationInput Deposit(
        CellId cell,
        string id,
        bool revealed,
        int remaining,
        int maximum)
    {
        return new TerrainDepositPresentationInput(
            cell,
            id,
            revealed,
            remaining,
            maximum,
            version: 1);
    }
}

}
