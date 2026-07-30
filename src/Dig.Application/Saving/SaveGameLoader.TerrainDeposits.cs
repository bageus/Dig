using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameLoader
{
    private sealed class UnknownTerrainDepositDefinitionException : Exception
    {
    }

    private sealed class UnsupportedTerrainDepositDefinitionVersionException : Exception
    {
    }

    private static IReadOnlyCollection<TerrainDepositInstance> BuildTerrainDeposits(
        TerrainDepositsSaveData data,
        WorldSaveData world,
        TerrainDepositCatalog? catalog)
    {
        if (data is null || data.Deposits is null)
        {
            throw new InvalidOperationException("Terrain deposits save data is missing.");
        }

        if (data.FormatVersion != TerrainDepositSaveSnapshot.CurrentFormatVersion
            || data.GeneratorVersion <= 0)
        {
            throw new InvalidOperationException(
                "Terrain deposit save metadata is invalid or unsupported.");
        }

        if (data.Deposits.Count == 0)
        {
            return Array.Empty<TerrainDepositInstance>();
        }

        if (catalog is null)
        {
            throw new UnknownTerrainDepositDefinitionException();
        }

        WorldSize size = new WorldSize(world.Width, world.Height, world.Depth);
        HashSet<string> instanceIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<CellId> occupiedCells = new HashSet<CellId>();
        List<TerrainDepositInstance> result = new List<TerrainDepositInstance>();
        foreach (TerrainDepositSaveData saved in data.Deposits
            .OrderBy(value => value.Z)
            .ThenBy(value => value.Y)
            .ThenBy(value => value.X)
            .ThenBy(value => value.InstanceId, StringComparer.Ordinal))
        {
            if (saved is null
                || string.IsNullOrWhiteSpace(saved.InstanceId)
                || string.IsNullOrWhiteSpace(saved.DefinitionId))
            {
                throw new InvalidOperationException(
                    "Terrain deposit entry is incomplete.");
            }

            TerrainDepositDefinition definition = catalog.Get(saved.DefinitionId)
                ?? throw new UnknownTerrainDepositDefinitionException();
            if (definition.Version != saved.DefinitionVersion)
            {
                throw new UnsupportedTerrainDepositDefinitionVersionException();
            }

            CellId cell = new CellId(saved.X, saved.Y, saved.Z);
            if (!size.Contains(cell)
                || !instanceIds.Add(saved.InstanceId)
                || !occupiedCells.Add(cell))
            {
                throw new InvalidOperationException(
                    "Terrain deposit position or identity is invalid.");
            }

            result.Add(new TerrainDepositInstance(
                saved.InstanceId,
                cell,
                definition,
                saved.IsRevealed,
                saved.RemainingYield,
                saved.Version));
        }

        return new ReadOnlyCollection<TerrainDepositInstance>(result);
    }


}

}
