using System;
using System.Linq;
using Dig.Application.Saving;
using Dig.Application.World;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class MiningOutputSaveDataAdapterTests
{
    [Fact]
    public void Encode_decode_preserves_ordered_xyz_source_and_multi_outputs()
    {
        MiningOutputCommitSaveSnapshot snapshot = new MiningOutputCommitSaveSnapshot(
            MiningOutputCommitSaveSnapshot.CurrentFormatVersion,
            new[]
            {
                new MiningOutputCommitSaveEntry(
                    new CellId(4, 3, 2),
                    MiningOutputSourceKind.Deposit,
                    "deposit.iron_ore",
                    sourceVersion: 2,
                    new[]
                    {
                        Line("ore.iron", 2,
                            "74000000000000000000000000000002",
                            "74000000000000000000000000000003"),
                    }),
                new MiningOutputCommitSaveEntry(
                    new CellId(1, 2, 0),
                    MiningOutputSourceKind.Terrain,
                    "terrain-output.multi",
                    sourceVersion: 4,
                    new[]
                    {
                        Line("material.stone", 1,
                            "74000000000000000000000000000004"),
                        Line("ore.gold", 1,
                            "74000000000000000000000000000005"),
                    }),
            });

        MiningOutputCommitsSaveData data = MiningOutputSaveDataAdapter.Encode(snapshot);
        MiningOutputCommitSaveSnapshot restored = MiningOutputSaveDataAdapter.Decode(data);

        Assert.Equal(snapshot.FormatVersion, restored.FormatVersion);
        Assert.Equal(new[] { new CellId(1, 2, 0), new CellId(4, 3, 2) },
            restored.Commits.Select(value => value.Cell));
        Assert.Equal("terrain-output.multi", restored.Commits[0].SourceId);
        Assert.Equal(4, restored.Commits[0].SourceVersion);
        Assert.Equal(2, restored.Commits[0].Outputs.Count);
        Assert.Equal(2, restored.Commits[1].Outputs[0].StackIds.Count);
    }

    [Fact]
    public void Decode_supports_legacy_format_one_until_save_migration_runs()
    {
        MiningOutputCommitsSaveData data = new MiningOutputCommitsSaveData
        {
            FormatVersion = 1,
        };
        data.Commits.Add(CreateLegacyStackEntry(
            2,
            2,
            1,
            "74000000000000000000000000000006"));

        MiningOutputCommitSaveSnapshot snapshot = MiningOutputSaveDataAdapter.Decode(data);

        MiningOutputCommitSaveEntry commit = Assert.Single(snapshot.Commits);
        Assert.Equal("legacy.terrain-output", commit.SourceId);
        Assert.Equal("material.stone", commit.ItemId);
        Assert.Equal("74000000000000000000000000000006", commit.StackId);
    }

    [Fact]
    public void Decode_rejects_unknown_source_kind()
    {
        MiningOutputCommitsSaveData data = new MiningOutputCommitsSaveData
        {
            FormatVersion = 1,
        };
        data.Commits.Add(new MiningOutputCommitSaveData
        {
            X = 1,
            Y = 1,
            Z = 1,
            SourceKind = 99,
            ItemId = "material.stone",
            Quantity = 1,
            StackId = "74000000000000000000000000000007",
            HasStack = true,
        });

        Assert.Throws<InvalidOperationException>(
            () => MiningOutputSaveDataAdapter.Decode(data));
    }

    [Fact]
    public void Decode_rejects_duplicate_cells_through_snapshot_validation()
    {
        MiningOutputCommitsSaveData data = new MiningOutputCommitsSaveData
        {
            FormatVersion = 1,
        };
        data.Commits.Add(CreateLegacyStackEntry(
            2, 2, 1, "74000000000000000000000000000008"));
        data.Commits.Add(CreateLegacyStackEntry(
            2, 2, 1, "74000000000000000000000000000009"));

        Assert.Throws<ArgumentException>(
            () => MiningOutputSaveDataAdapter.Decode(data));
    }

    private static MiningOutputCommitLineSaveEntry Line(
        string itemId,
        int quantity,
        params string[] stackIds)
    {
        return new MiningOutputCommitLineSaveEntry(itemId, quantity, stackIds);
    }

    private static MiningOutputCommitSaveData CreateLegacyStackEntry(
        int x,
        int y,
        int z,
        string stackId)
    {
        return new MiningOutputCommitSaveData
        {
            X = x,
            Y = y,
            Z = z,
            SourceKind = (int)MiningOutputSourceKind.Terrain,
            ItemId = "material.stone",
            Quantity = 1,
            StackId = stackId,
            HasStack = true,
        };
    }
}

}
