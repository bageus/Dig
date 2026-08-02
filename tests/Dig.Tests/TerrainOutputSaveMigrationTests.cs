using Dig.Application.Saving;
using Dig.Application.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TerrainOutputSaveMigrationTests
{
    [Fact]
    public void Version_twelve_migrates_legacy_metal_and_multi_output_contract_once()
    {
        SaveGameDocument document = new SaveGameDocument
        {
            FormatVersion = 12,
            Inventory = new InventorySaveData
            {
                Stacks =
                {
                    new ItemStackSaveData
                    {
                        StackId = "7a000000000000000000000000000001",
                        ItemId = "material.metal",
                        Quantity = 1,
                    },
                },
                ResidentSlotClaims =
                {
                    new ResidentSlotClaimSaveData
                    {
                        ItemId = "material.metal",
                        Quantity = 1,
                    },
                },
            },
            MiningOutput = new MiningOutputCommitsSaveData
            {
                FormatVersion = 1,
                Commits =
                {
                    new MiningOutputCommitSaveData
                    {
                        X = 2,
                        Y = 3,
                        Z = 1,
                        SourceKind = (int)MiningOutputSourceKind.Terrain,
                        ItemId = "material.metal",
                        Quantity = 1,
                        StackId = "7a000000000000000000000000000002",
                        HasStack = true,
                    },
                },
            },
        };
        SaveVersionTwelveTerrainOutputContractMigration migration =
            new SaveVersionTwelveTerrainOutputContractMigration();

        migration.Apply(document);

        Assert.Equal(13, document.FormatVersion);
        Assert.Equal("material.iron", document.Inventory.Stacks[0].ItemId);
        Assert.Equal("material.iron", document.Inventory.ResidentSlotClaims[0].ItemId);
        Assert.Equal(MiningOutputCommitSaveSnapshot.CurrentFormatVersion,
            document.MiningOutput.FormatVersion);
        MiningOutputCommitSaveData commit = Assert.Single(document.MiningOutput.Commits);
        Assert.Equal("legacy.terrain-output", commit.SourceId);
        Assert.Equal(1, commit.SourceVersion);
        MiningOutputCommitOutputSaveData output = Assert.Single(commit.Outputs);
        Assert.Equal("material.iron", output.ItemId);
        Assert.Equal(1, output.Quantity);
        Assert.Equal(commit.StackId, Assert.Single(output.StackIds));
    }

    [Fact]
    public void Pipeline_replay_does_not_apply_version_eleven_migration_twice()
    {
        SaveGameDocument document = new SaveGameDocument { FormatVersion = 12 };
        SaveMigrationPipeline pipeline = new SaveMigrationPipeline(new ISaveMigration[]
        {
            new SaveVersionTwelveTerrainOutputContractMigration(),
            new SaveVersionThirteenVukerEcologyMigration(),
        });

        var first = pipeline.Apply(document);
        var replay = pipeline.Apply(document);

        Assert.True(first.IsSuccess);
        Assert.Equal(new[]
        {
            "save.v12_to_v13.terrain_output_contract",
            "save.v13_to_v14.vuker_ecology",
        }, first.Value.AppliedSteps);
        Assert.True(replay.IsSuccess);
        Assert.Empty(replay.Value.AppliedSteps);
    }
}

}
