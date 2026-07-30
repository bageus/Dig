using Dig.Application.Saving;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TerrainDepositSaveContractTests
{
    [Fact]
    public void Version_ten_migration_adds_generator_and_definition_versions()
    {
        SaveGameDocument document = new SaveGameDocument
        {
            FormatVersion = 10,
            Metadata = new SaveMetadataData
            {
                GeneratorVersion = 6,
            },
            TerrainDeposits = new TerrainDepositsSaveData
            {
                Deposits =
                {
                    new TerrainDepositSaveData
                    {
                        InstanceId = "legacy",
                        DefinitionId = "deposit.iron_ore",
                        X = 1,
                        Y = 2,
                        Z = 3,
                        RemainingYield = 4,
                    },
                },
            },
        };

        new SaveVersionTenTerrainDepositContractMigration().Apply(document);

        Assert.Equal(11, document.FormatVersion);
        Assert.Equal(
            TerrainDepositSaveSnapshot.CurrentFormatVersion,
            document.TerrainDeposits.FormatVersion);
        Assert.Equal(6, document.TerrainDeposits.GeneratorVersion);
        Assert.Equal(1, document.TerrainDeposits.Deposits[0].DefinitionVersion);
        Assert.Equal(3, document.TerrainDeposits.Deposits[0].Z);
    }

    [Fact]
    public void Empty_version_ten_section_migrates_to_a_valid_empty_snapshot()
    {
        SaveGameDocument document = new SaveGameDocument
        {
            FormatVersion = 10,
            Metadata = new SaveMetadataData { GeneratorVersion = 0 },
            TerrainDeposits = new TerrainDepositsSaveData(),
        };

        new SaveVersionTenTerrainDepositContractMigration().Apply(document);

        Assert.Equal(1, document.TerrainDeposits.GeneratorVersion);
        Assert.Empty(document.TerrainDeposits.Deposits);
    }
}

}
