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
