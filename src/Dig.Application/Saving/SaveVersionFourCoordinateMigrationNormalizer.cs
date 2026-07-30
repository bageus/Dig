using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dig.Domain.Jobs;

namespace Dig.Application.Saving
{

internal static class SaveVersionFourCoordinateMigrationNormalizer
{
    internal static void Apply(SaveGameDocument document)
    {
        NormalizeWorld(document);
        NormalizeInventory(document);
        NormalizeBuildings(document);
        NormalizeAgents(document);
        NormalizeDeposits(document);
        NormalizeJobs(document);
    }

    private static void NormalizeWorld(SaveGameDocument document)
    {
        document.World ??= new WorldSaveData();
        document.World.Depth = Dig.Domain.World.WorldSize.RequiredDepth;
        document.World.Chunks ??= new List<WorldChunkSaveData>();
        foreach (WorldChunkSaveData chunk in document.World.Chunks)
        {
            if (chunk is null)
            {
                continue;
            }

            chunk.Z = 0;
            chunk.Cells ??= new List<WorldCellSaveData>();
            foreach (WorldCellSaveData cell in chunk.Cells)
            {
                if (cell is not null)
                {
                    cell.Z = 0;
                }
            }
        }
    }

    private static void NormalizeInventory(SaveGameDocument document)
    {
        document.Inventory ??= new InventorySaveData();
        document.Inventory.Stacks ??= new List<ItemStackSaveData>();
        foreach (ItemStackSaveData stack in document.Inventory.Stacks)
        {
            if (stack?.Location is not null && stack.Location.CellX.HasValue)
            {
                stack.Location.CellZ = 0;
            }
        }
    }

    private static void NormalizeBuildings(SaveGameDocument document)
    {
        document.Buildings ??= new BuildingsSaveData();
        document.Buildings.Buildings ??= new List<BuildingSaveData>();
        foreach (BuildingSaveData building in document.Buildings.Buildings)
        {
            if (building is not null)
            {
                building.OriginZ = 0;
                building.WorkPositionZ = 0;
            }
        }
    }

    private static void NormalizeAgents(SaveGameDocument document)
    {
        document.AgentPositions ??= new AgentPositionsSaveData();
        document.AgentPositions.Agents ??= new List<AgentPositionSaveData>();
        foreach (AgentPositionSaveData agent in document.AgentPositions.Agents)
        {
            if (agent is not null)
            {
                agent.Z = 0;
            }
        }
    }

    private static void NormalizeDeposits(SaveGameDocument document)
    {
        document.TerrainDeposits ??= new TerrainDepositsSaveData();
        document.TerrainDeposits.Deposits ??= new List<TerrainDepositSaveData>();
        foreach (TerrainDepositSaveData deposit in document.TerrainDeposits.Deposits)
        {
            if (deposit is not null)
            {
                deposit.Z = 0;
            }
        }
    }

    private static void NormalizeJobs(SaveGameDocument document)
    {
        document.Jobs ??= new JobsSaveData();
        document.Jobs.Jobs ??= new List<JobSaveData>();
        document.Jobs.Reservations ??= new List<JobReservationSaveData>();
        foreach (JobSaveData job in document.Jobs.Jobs)
        {
            NormalizeJobDefinition(job?.Definition);
        }

        foreach (JobReservationSaveData reservation in document.Jobs.Reservations)
        {
            if (reservation is null
                || (reservation.Kind != (int)ReservationKind.Position
                    && reservation.Kind != (int)ReservationKind.Designation))
            {
                continue;
            }

            reservation.Value = NormalizeCellValue(reservation.Value);
        }
    }

    private static void NormalizeJobDefinition(JobDefinitionSaveData? definition)
    {
        if (definition is null)
        {
            return;
        }

        definition.Properties ??= new List<SavePropertyData>();
        HashSet<string> keys = definition.Properties
            .Where(value => value is not null)
            .Select(value => value.Key)
            .ToHashSet(StringComparer.Ordinal);
        string[] prefixes = keys
            .Where(key => key.EndsWith(".x", StringComparison.Ordinal))
            .Select(key => key.Substring(0, key.Length - 2))
            .Where(prefix => keys.Contains(prefix + ".y")
                && !keys.Contains(prefix + ".z"))
            .OrderBy(prefix => prefix, StringComparer.Ordinal)
            .ToArray();
        foreach (string prefix in prefixes)
        {
            definition.Properties.Add(new SavePropertyData
            {
                Key = prefix + ".z",
                Value = "0",
            });
        }

        definition.Properties = definition.Properties
            .Where(value => value is not null)
            .OrderBy(value => value.Key, StringComparer.Ordinal)
            .ThenBy(value => value.Value, StringComparer.Ordinal)
            .ToList();
    }

    private static string NormalizeCellValue(string value)
    {
        string[] parts = (value ?? string.Empty).Split(',');
        if ((parts.Length != 2 && parts.Length != 3)
            || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)
            || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
        {
            return value;
        }

        return x.ToString(CultureInfo.InvariantCulture)
            + ","
            + y.ToString(CultureInfo.InvariantCulture)
            + ",0";
    }
}

}
