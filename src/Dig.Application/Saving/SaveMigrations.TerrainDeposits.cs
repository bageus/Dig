using System;
using System.Collections.Generic;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed class SaveVersionTenTerrainDepositContractMigration : ISaveMigration
{
    public string Id => "save.v10_to_v11.terrain_deposit_contract";

    public int FromVersion => 10;

    public int ToVersion => 11;

    public void Apply(SaveGameDocument document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (document.FormatVersion != FromVersion)
        {
            throw new InvalidOperationException(
                "Migration received the wrong source version.");
        }

        document.TerrainDeposits ??= new TerrainDepositsSaveData();
        document.TerrainDeposits.Deposits ??= new List<TerrainDepositSaveData>();
        document.TerrainDeposits.FormatVersion =
            TerrainDepositSaveSnapshot.CurrentFormatVersion;
        document.TerrainDeposits.GeneratorVersion =
            document.Metadata?.GeneratorVersion > 0
                ? document.Metadata.GeneratorVersion
                : 1;
        foreach (TerrainDepositSaveData deposit in document.TerrainDeposits.Deposits)
        {
            if (deposit != null && deposit.DefinitionVersion <= 0)
            {
                deposit.DefinitionVersion = 1;
            }
        }

        document.FormatVersion = ToVersion;
    }
}

}
