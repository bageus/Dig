using Dig.Application.Saving;
using Dig.Application.World;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class MiningOutputSaveWorldValidationTests
{
    [Fact]
    public void Exact_xyz_cells_inside_authoritative_world_are_valid()
    {
        MiningOutputCommitSaveSnapshot snapshot = new MiningOutputCommitSaveSnapshot(
            MiningOutputCommitSaveSnapshot.CurrentFormatVersion,
            new[]
            {
                EmptyCommit(new CellId(0, 0, 0)),
                EmptyCommit(new CellId(7, 5, 3)),
            });

        MiningOutputSaveWorldValidationReport report =
            new MiningOutputSaveWorldValidator().Validate(
                snapshot,
                new WorldSize(8, 6, 4));

        Assert.True(report.IsValid);
        Assert.Empty(report.Issues);
    }

    [Fact]
    public void Out_of_bounds_cells_are_reported_in_deterministic_xyz_order()
    {
        MiningOutputCommitSaveSnapshot snapshot = new MiningOutputCommitSaveSnapshot(
            MiningOutputCommitSaveSnapshot.CurrentFormatVersion,
            new[]
            {
                EmptyCommit(new CellId(2, 2, 4)),
                EmptyCommit(new CellId(-1, 1, 0)),
                EmptyCommit(new CellId(3, 3, 1)),
            });

        MiningOutputSaveWorldValidationReport report =
            new MiningOutputSaveWorldValidator().Validate(
                snapshot,
                new WorldSize(4, 4, 4));

        Assert.False(report.IsValid);
        Assert.Equal(2, report.Issues.Count);
        Assert.Equal(new CellId(-1, 1, 0), report.Issues[0].Cell);
        Assert.Equal(new CellId(2, 2, 4), report.Issues[1].Cell);
        Assert.All(
            report.Issues,
            issue => Assert.Equal(
                MiningOutputSaveWorldValidationCodes.CellOutOfBounds,
                issue.Code));
    }

    [Fact]
    public void Data_contract_validation_reuses_existing_decode_invariants()
    {
        MiningOutputCommitsSaveData data = new MiningOutputCommitsSaveData();
        data.Commits.Add(new MiningOutputCommitSaveData
        {
            X = 5,
            Y = 1,
            Z = 0,
            SourceKind = (int)MiningOutputSourceKind.Terrain,
            ItemId = string.Empty,
            Quantity = 0,
            StackId = null,
            HasStack = false,
        });

        MiningOutputSaveWorldValidationReport report =
            new MiningOutputSaveWorldValidator().Validate(
                data,
                new WorldSize(5, 5, 4));

        Assert.False(report.IsValid);
        Assert.Single(report.Issues);
        Assert.Equal(new CellId(5, 1, 0), report.Issues[0].Cell);
    }

    private static MiningOutputCommitSaveEntry EmptyCommit(CellId cell)
    {
        return new MiningOutputCommitSaveEntry(
            cell,
            MiningOutputSourceKind.Terrain,
            string.Empty,
            quantity: 0,
            stackId: null,
            hasStack: false);
    }
}

}
