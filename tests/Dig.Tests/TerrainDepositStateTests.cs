using System;
using System.Linq;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TerrainDepositStateTests
{
    private static readonly TerrainDepositDefinition Iron = new TerrainDepositDefinition(
        "deposit.iron_ore",
        "Iron ore",
        new ItemId("ore.iron"),
        maximumYield: 8,
        generationWeight: 1);

    [Fact]
    public void Snapshot_is_ordered_and_rejects_duplicate_cells_or_ids()
    {
        TerrainDepositState state = new TerrainDepositState();
        TerrainDepositInstance first = Deposit("deposit-instance-b", 2, 1, 3);
        TerrainDepositInstance second = Deposit("deposit-instance-a", 1, 1, 0);

        state.ReplaceAll(new[] { first, second });

        Assert.Equal(new[] { second.Cell, first.Cell }, state.Snapshot().Select(value => value.Cell));
        Assert.Throws<ArgumentException>(() => state.ReplaceAll(new[]
        {
            first,
            Deposit("deposit-instance-c", first.Cell.X, first.Cell.Y, first.Cell.Z),
        }));
        Assert.Throws<ArgumentException>(() => state.ReplaceAll(new[]
        {
            first,
            new TerrainDepositInstance(
                first.InstanceId,
                new CellId(3, 1, 0),
                Iron,
                isRevealed: false,
                remainingYield: Iron.MaximumYield,
                version: 1),
        }));
    }

    [Fact]
    public void Reveal_and_deplete_are_idempotent_and_do_not_change_neighbor()
    {
        TerrainDepositState state = new TerrainDepositState();
        TerrainDepositInstance target = Deposit("deposit-instance-a", 2, 2, 1);
        TerrainDepositInstance neighbor = Deposit("deposit-instance-b", 3, 2, 1);
        state.ReplaceAll(new[] { target, neighbor });

        Assert.True(state.Reveal(target.Cell, version: 10));
        Assert.False(state.Reveal(target.Cell, version: 11));
        Assert.True(state.Deplete(target.Cell, version: 12));
        Assert.False(state.Deplete(target.Cell, version: 13));

        Assert.True(state.TryGet(target.Cell, out TerrainDepositInstance changed));
        Assert.True(changed.IsRevealed);
        Assert.True(changed.IsDepleted);
        Assert.Equal(12, changed.Version);
        Assert.True(state.TryGet(neighbor.Cell, out TerrainDepositInstance unchanged));
        Assert.False(unchanged.IsRevealed);
        Assert.False(unchanged.IsDepleted);
        Assert.Equal(Iron.MaximumYield, unchanged.RemainingYield);
    }

    [Fact]
    public void Reveal_adjacent_to_uses_six_xyz_neighbors_and_is_idempotent()
    {
        TerrainDepositState state = new TerrainDepositState();
        TerrainDepositInstance left = Deposit("deposit-left", 1, 2, 1);
        TerrainDepositInstance right = Deposit("deposit-right", 3, 2, 1);
        TerrainDepositInstance above = Deposit("deposit-above", 2, 2, 2);
        TerrainDepositInstance diagonal = Deposit("deposit-diagonal", 3, 3, 1);
        state.ReplaceAll(new[] { diagonal, above, right, left });

        Assert.Equal(3, state.RevealAdjacentTo(new CellId(2, 2, 1), version: 20));
        Assert.Equal(0, state.RevealAdjacentTo(new CellId(2, 2, 1), version: 21));

        Assert.True(state.TryGet(left.Cell, out TerrainDepositInstance leftChanged));
        Assert.True(state.TryGet(right.Cell, out TerrainDepositInstance rightChanged));
        Assert.True(state.TryGet(above.Cell, out TerrainDepositInstance aboveChanged));
        Assert.True(leftChanged.IsRevealed);
        Assert.True(rightChanged.IsRevealed);
        Assert.True(aboveChanged.IsRevealed);
        Assert.True(state.TryGet(diagonal.Cell, out TerrainDepositInstance diagonalUnchanged));
        Assert.False(diagonalUnchanged.IsRevealed);
    }

    private static TerrainDepositInstance Deposit(string id, int x, int y, int z)
    {
        return new TerrainDepositInstance(
            id,
            new CellId(x, y, z),
            Iron,
            isRevealed: false,
            remainingYield: Iron.MaximumYield,
            version: 1);
    }
}

}
