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
    private static readonly ItemId IronOre = new ItemId("ore.iron");

    [Fact]
    public void Round_trip_preserves_multi_output_units_source_and_empty_commit()
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
                "terrain-output.multi",
                3,
                new[]
                {
                    new TerrainOutputEntry(Stone, 1_000, 2, 2),
                    new TerrainOutputEntry(IronOre, 1_000, 1, 1),
                })),
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
        EntityId[] ids =
        {
            Id("73000000000000000000000000000001"),
            Id("73000000000000000000000000000002"),
            Id("73000000000000000000000000000003"),
        };
        original.Record(output, ids);
        original.Record(empty, Array.Empty<EntityId>());

        MiningOutputCommitSaveSnapshot snapshot =
            MiningOutputCommitSaveSnapshot.Capture(original);
        MiningOutputCommitState restored = snapshot.Restore();

        Assert.Equal(MiningOutputCommitSaveSnapshot.CurrentFormatVersion,
            snapshot.FormatVersion);
        Assert.Equal(new[] { emptyCell, outputCell },
            snapshot.Commits.Select(value => value.Cell));
        MiningOutputCommit restoredOutput = restored.Snapshot()
            .Single(value => value.Cell == outputCell);
        Assert.Equal("terrain-output.multi", restoredOutput.SourceId);
        Assert.Equal(3, restoredOutput.SourceVersion);
        Assert.Equal(2, restoredOutput.Outputs.Count);
        Assert.Equal(ids, restoredOutput.StackIds);
        Assert.Equal(3, restoredOutput.Quantity);
        Assert.True(restored.IsCommitted(emptyCell));
    }

    [Fact]
    public void Unsupported_snapshot_version_is_rejected_without_partial_state()
    {
        MiningOutputCommitSaveSnapshot snapshot = new MiningOutputCommitSaveSnapshot(
            MiningOutputCommitSaveSnapshot.CurrentFormatVersion + 1,
            Array.Empty<MiningOutputCommitSaveEntry>());

        Assert.Throws<InvalidOperationException>(() => snapshot.Restore());
    }

    [Fact]
    public void Duplicate_cells_are_rejected_at_snapshot_boundary()
    {
        CellId cell = new CellId(2, 3, 1);
        MiningOutputCommitSaveEntry first = Entry(
            cell,
            "73000000000000000000000000000004");
        MiningOutputCommitSaveEntry second = Entry(
            cell,
            "73000000000000000000000000000005");

        Assert.Throws<ArgumentException>(() => new MiningOutputCommitSaveSnapshot(
            MiningOutputCommitSaveSnapshot.CurrentFormatVersion,
            new[] { first, second }));
    }

    private static MiningOutputCommitSaveEntry Entry(CellId cell, string stackId)
    {
        return new MiningOutputCommitSaveEntry(
            cell,
            MiningOutputSourceKind.Terrain,
            "terrain-output.stone",
            sourceVersion: 1,
            new[]
            {
                new MiningOutputCommitLineSaveEntry(
                    Stone.ToString(),
                    quantity: 1,
                    new[] { stackId }),
            });
    }

    private static MaterialDefinition MineableTerrain(TerrainOutputProfile profile)
    {
        return new MaterialDefinition(
            new MaterialId("terrain.test"),
            "Test terrain",
            true,
            10,
            true,
            profile);
    }

    private static EntityId Id(string value) => EntityId.Parse(value);
}

}
