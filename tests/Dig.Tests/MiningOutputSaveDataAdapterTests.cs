using System;
using Dig.Application.Saving;
using Dig.Application.World;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class MiningOutputSaveDataAdapterTests
{
    [Fact]
    public void Encode_decode_preserves_ordered_exact_xyz_entries()
    {
        MiningOutputCommitSaveSnapshot snapshot = new MiningOutputCommitSaveSnapshot(
            MiningOutputCommitSaveSnapshot.CurrentFormatVersion,
            new[]
            {
                new MiningOutputCommitSaveEntry(
                    new CellId(4, 3, 2),
                    MiningOutputSourceKind.Deposit,
                    "ore.iron",
                    5,
                    "74000000000000000000000000000002",
                    hasStack: true),
                new MiningOutputCommitSaveEntry(
                    new CellId(1, 2, 0),
                    MiningOutputSourceKind.Terrain,
                    string.Empty,
                    0,
                    stackId: null,
                    hasStack: false),
            });

        MiningOutputCommitsSaveData data = MiningOutputSaveDataAdapter.Encode(snapshot);
        MiningOutputCommitSaveSnapshot restored = MiningOutputSaveDataAdapter.Decode(data);

        Assert.Equal(snapshot.FormatVersion, restored.FormatVersion);
        Assert.Equal(2, restored.Commits.Count);
        Assert.Equal(new CellId(1, 2, 0), restored.Commits[0].Cell);
        Assert.Equal(new CellId(4, 3, 2), restored.Commits[1].Cell);
        Assert.False(restored.Commits[0].HasStack);
        Assert.Equal("74000000000000000000000000000002", restored.Commits[1].StackId);
    }

    [Fact]
    public void Decode_rejects_unknown_source_kind()
    {
        MiningOutputCommitsSaveData data = new MiningOutputCommitsSaveData();
        data.Commits.Add(new MiningOutputCommitSaveData
        {
            X = 1,
            Y = 1,
            Z = 1,
            SourceKind = 99,
            ItemId = "material.stone",
            Quantity = 1,
            StackId = "74000000000000000000000000000003",
            HasStack = true,
        });

        Assert.Throws<InvalidOperationException>(
            () => MiningOutputSaveDataAdapter.Decode(data));
    }

    [Fact]
    public void Decode_rejects_duplicate_cells_through_snapshot_validation()
    {
        MiningOutputCommitsSaveData data = new MiningOutputCommitsSaveData();
        data.Commits.Add(CreateStackEntry(2, 2, 1, "74000000000000000000000000000004"));
        data.Commits.Add(CreateStackEntry(2, 2, 1, "74000000000000000000000000000005"));

        Assert.Throws<ArgumentException>(
            () => MiningOutputSaveDataAdapter.Decode(data));
    }

    private static MiningOutputCommitSaveData CreateStackEntry(
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
