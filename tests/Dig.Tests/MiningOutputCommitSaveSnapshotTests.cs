using System;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class MiningOutputCommitSaveSnapshotTests
{
    private static readonly ItemId Stone = new ItemId("material.stone");
    private static readonly EntityId Stack =
        EntityId.Parse("73000000000000000000000000000001");

    [Fact]
    public void Round_trip_preserves_exactly_once_cells_and_world_stack_identity()
    {
        CellId outputCell = new CellId(4, 5, 2);
        CellId emptyCell = new CellId(1, 2, 3);
        TerrainDepositState deposits = new TerrainDepositState();
        MiningOutputCommitState original = new MiningOutputCommitState();
        MiningOutputResolver resolver = new MiningOutputResolver();

        MiningOutputPlan output = resolver.Resolve(
            17,
            2,
            outputCell,
            MineableTerrain(new TerrainOutputProfile(
                "terrain-output.stone",
                1,
                new[] { new TerrainOutputEntry(Stone, 1_000, 3, 3) })),
            deposits);
        MiningOutputPlan empty = resolver.Resolve(
            17,
            2,
            emptyCell,
            MineableTerrain(new TerrainOutputProfile(
                "terrain-output.empty",
                1,
                Array.Empty<TerrainOutputEntry>())),
            deposits);
        original.Record(output, Stack);
        original.Record(empty, default);

        MiningOutputCommitSaveSnapshot snapshot =
            MiningOutputCommitSaveSnapshot.Capture(original);
        MiningOutputCommitState restored = snapshot.Restore();

        Assert.Equal(MiningOutputCommitSaveSnapshot.CurrentFormatVersion, snapshot.FormatVersion);
        Assert.Equal(new[] { emptyCell, outputCell }, snapshot.Commits.Select(value => value.Cell));
        Assert.True(restored.IsCommitted(outputCell));
        Assert.True(restored.IsCommitted(emptyCell));
        MiningOutputCommit restoredOutput = restored.Snapshot().Single(value => value.Cell == outputCell);
        Assert.True(restoredOutput.HasStack);
        Assert.Equal(Stack, restoredOutput.StackId);
        Assert.Equal(Stone, restoredOutput.ItemId);
        Assert.Equal(3, restoredOutput.Quantity);
    }

    [Fact]
    public void Unsupported_snapshot_version_is_rejected_without_partial_state()
    {
        MiningOutputCommitSaveSnapshot snapshot = new MiningOutputCommitSaveSnapshot(
            formatVersion: MiningOutputCommitSaveSnapshot.CurrentFormatVersion + 1,
            Array.Empty<MiningOutputCommitSaveEntry>());

        Assert.Throws<InvalidOperationException>(() => snapshot.Restore());
    }

    [Fact]
    public void Duplicate_cells_are_rejected_at_snapshot_boundary()
    {
        CellId cell = new CellId(2, 3, 1);
        MiningOutputCommitSaveEntry first = new MiningOutputCommitSaveEntry(
            cell,
            MiningOutputSourceKind.Terrain,
            Stone.ToString(),
            quantity: 1,
            Stack.ToString(),
            hasStack: true);
        MiningOutputCommitSaveEntry second = new MiningOutputCommitSaveEntry(
            cell,
            MiningOutputSourceKind.Terrain,
            Stone.ToString(),
            quantity: 1,
            EntityId.Parse("73000000000000000000000000000002").ToString(),
            hasStack: true);

        Assert.Throws<ArgumentException>(() => new MiningOutputCommitSaveSnapshot(
            MiningOutputCommitSaveSnapshot.CurrentFormatVersion,
            new[] { first, second }));
    }

    private static MaterialDefinition MineableTerrain(TerrainOutputProfile profile)
    {
        return new MaterialDefinition(
            new MaterialId("terrain.test"),
            "Test terrain",
            isSolid: true,
            hardness: 10,
            isMineable: true,
            outputProfile: profile);
    }
}

}
