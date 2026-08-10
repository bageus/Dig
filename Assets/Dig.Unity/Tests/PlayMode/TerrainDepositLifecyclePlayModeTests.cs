using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.World;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class TerrainDepositLifecyclePlayModeTests
{
    private static readonly CellId[] Offsets =
    {
        new CellId(-1, 0, 0),
        new CellId(1, 0, 0),
        new CellId(0, -1, 0),
        new CellId(0, 1, 0),
        new CellId(0, 0, -1),
        new CellId(0, 0, 1),
    };

    [Test]
    public void Hidden_xyz_deposit_reveals_then_depletes_through_world_owner()
    {
        DigWorldSession session = DigWorldSession.CreateDemo(24, 16, 19);
        WorldState world = session.Repository.Get();
        TerrainDepositInstance[] deposits = world.TerrainDeposits.Snapshot().ToArray();
        Assert.That(deposits, Is.Not.Empty);
        Assert.That(deposits.All(value => !value.IsRevealed), Is.True);
        Assert.That(
            deposits.All(value => value.Cell.Z >= CellId.MinimumDepth
                && value.Cell.Z <= CellId.MaximumDepth),
            Is.True);
        HashSet<CellId> depositCells = deposits.Select(value => value.Cell).ToHashSet();
        (TerrainDepositInstance deposit, CellId revealCell) = FindRevealTarget(
            world,
            deposits,
            depositCells);

        Result reveal = session.ExcavateSpatialCell(revealCell);

        Assert.That(reveal.IsSuccess, Is.True, reveal.Error?.ToString());
        Assert.That(
            world.TerrainDeposits.TryGet(
                deposit.Cell,
                out TerrainDepositInstance revealed),
            Is.True);
        Assert.That(revealed.IsRevealed, Is.True);
        TerrainDepositCellViewModel revealedView = session.LoadTerrainDeposits()
            .Cells.Single(value => value.Cell == deposit.Cell);
        Assert.That(revealedView.State, Is.EqualTo(TerrainDepositVisualState.Revealed));
        Assert.That(revealedView.VisibleDepositId, Is.EqualTo(deposit.Definition.Id));

        Result deplete = session.ExcavateSpatialCell(deposit.Cell);

        Assert.That(deplete.IsSuccess, Is.True, deplete.Error?.ToString());
        Assert.That(world.GetCell(deposit.Cell).Value.IsSolid, Is.False);
        Assert.That(
            world.TerrainDeposits.TryGet(
                deposit.Cell,
                out TerrainDepositInstance depleted),
            Is.True);
        Assert.That(depleted.IsDepleted, Is.True);
        TerrainDepositCellViewModel depletedView = session.LoadTerrainDeposits()
            .Cells.Single(value => value.Cell == deposit.Cell);
        Assert.That(depletedView.State, Is.EqualTo(TerrainDepositVisualState.Depleted));
        Assert.That(depletedView.IsVisible, Is.False);
    }

    private static (TerrainDepositInstance Deposit, CellId RevealCell) FindRevealTarget(
        WorldState world,
        IReadOnlyList<TerrainDepositInstance> deposits,
        ISet<CellId> depositCells)
    {
        foreach (TerrainDepositInstance deposit in deposits)
        {
            for (int index = 0; index < Offsets.Length; index++)
            {
                CellId offset = Offsets[index];
                CellId candidate = new CellId(
                    deposit.Cell.X + offset.X,
                    deposit.Cell.Y + offset.Y,
                    deposit.Cell.Z + offset.Z);
                if (!world.Size.Contains(candidate) || depositCells.Contains(candidate))
                {
                    continue;
                }

                Result<CellSnapshot> cell = world.GetCell(candidate);
                if (cell.IsSuccess && cell.Value.IsSolid)
                {
                    return (deposit, candidate);
                }
            }
        }

        Assert.Fail("No hidden deposit has an adjacent non-deposit solid reveal cell.");
        return default;
    }
}

}
