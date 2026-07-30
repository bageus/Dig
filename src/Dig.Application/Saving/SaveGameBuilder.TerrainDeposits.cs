using System;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameBuilder
{
    private static TerrainDepositsSaveData BuildTerrainDeposits(
        TerrainDepositState deposits)
    {
        if (deposits is null)
        {
            throw new ArgumentNullException(nameof(deposits));
        }

        TerrainDepositSaveSnapshot snapshot = deposits.CaptureSaveSnapshot();
        TerrainDepositsSaveData data = new TerrainDepositsSaveData
        {
            FormatVersion = snapshot.FormatVersion,
            GeneratorVersion = snapshot.GeneratorVersion,
        };
        foreach (TerrainDepositSaveEntry deposit in snapshot.Deposits)
        {
            data.Deposits.Add(new TerrainDepositSaveData
            {
                InstanceId = deposit.InstanceId,
                DefinitionId = deposit.DefinitionId,
                DefinitionVersion = deposit.DefinitionVersion,
                X = deposit.Cell.X,
                Y = deposit.Cell.Y,
                Z = deposit.Cell.Z,
                IsRevealed = deposit.IsRevealed,
                RemainingYield = deposit.RemainingYield,
                Version = deposit.Version,
            });
        }

        return data;
    }
}

}
