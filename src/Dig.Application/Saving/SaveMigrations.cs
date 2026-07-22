using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Application.Saving
{

public static class SaveErrors
{
    public static readonly DomainError InvalidDocument = new DomainError(
        "save.document.invalid",
        "The save document is malformed or inconsistent.");

    public static readonly DomainError UnsupportedVersion = new DomainError(
        "save.version.unsupported",
        "The save format version cannot be loaded by this build.");

    public static readonly DomainError UnknownJobType = new DomainError(
        "save.job_type.unknown",
        "The save references a job type without a registered codec.");

    public static readonly DomainError UnknownBuildingDefinition = new DomainError(
        "save.building_definition.unknown",
        "The save references a building definition missing from the current catalog.");

    public static readonly DomainError UnknownTerrainDepositDefinition = new DomainError(
        "save.terrain_deposit_definition.unknown",
        "The save references a terrain deposit definition missing from the current catalog.");
}

public sealed class SaveMigrationPipeline
{
    private readonly Dictionary<int, ISaveMigration> _bySourceVersion;

    public SaveMigrationPipeline(IEnumerable<ISaveMigration> migrations)
    {
        if (migrations is null)
        {
            throw new ArgumentNullException(nameof(migrations));
        }

        ISaveMigration[] values = migrations.ToArray();
        if (values.Any(item => item is null
            || string.IsNullOrWhiteSpace(item.Id)
            || item.ToVersion != item.FromVersion + 1))
        {
            throw new ArgumentException(
                "Save migrations must have ids and advance exactly one version.",
                nameof(migrations));
        }

        if (values.Select(item => item.FromVersion).Distinct().Count() != values.Length)
        {
            throw new ArgumentException(
                "Only one migration may start from each save version.",
                nameof(migrations));
        }

        _bySourceVersion = values.ToDictionary(item => item.FromVersion);
    }

    public Result<SaveMigrationReport> Apply(SaveGameDocument document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (document.FormatVersion < 0
            || document.FormatVersion > SaveFormat.CurrentVersion)
        {
            return Result<SaveMigrationReport>.Failure(SaveErrors.UnsupportedVersion);
        }

        List<string> applied = new List<string>();
        while (document.FormatVersion < SaveFormat.CurrentVersion)
        {
            if (!_bySourceVersion.TryGetValue(
                document.FormatVersion,
                out ISaveMigration? migration))
            {
                return Result<SaveMigrationReport>.Failure(SaveErrors.UnsupportedVersion);
            }

            int previousVersion = document.FormatVersion;
            migration.Apply(document);
            if (document.FormatVersion != migration.ToVersion
                || previousVersion != migration.FromVersion)
            {
                return Result<SaveMigrationReport>.Failure(SaveErrors.InvalidDocument);
            }

            applied.Add(migration.Id);
        }

        return Result<SaveMigrationReport>.Success(new SaveMigrationReport(applied));
    }
}

public sealed class LegacySaveVersionZeroMigration : ISaveMigration
{
    public string Id => "save.v0_to_v1.metadata";
    public int FromVersion => 0;
    public int ToVersion => 1;

    public void Apply(SaveGameDocument document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (document.FormatVersion != FromVersion)
        {
            throw new InvalidOperationException("Migration received the wrong source version.");
        }

        document.Metadata ??= new SaveMetadataData();
        if (document.Metadata.GeneratorVersion <= 0)
        {
            document.Metadata.GeneratorVersion = 1;
        }

        if (string.IsNullOrWhiteSpace(document.Metadata.DisplayName))
        {
            document.Metadata.DisplayName = document.Metadata.SlotId;
        }

        document.FormatVersion = ToVersion;
    }
}

public sealed class SaveVersionOneBuildingsMigration : ISaveMigration
{
    public string Id => "save.v1_to_v2.buildings";
    public int FromVersion => 1;
    public int ToVersion => 2;

    public void Apply(SaveGameDocument document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (document.FormatVersion != FromVersion)
        {
            throw new InvalidOperationException("Migration received the wrong source version.");
        }

        document.Buildings ??= new BuildingsSaveData();
        document.FormatVersion = ToVersion;
    }
}

public sealed class SaveVersionTwoPackingMigration : ISaveMigration
{
    public string Id => "save.v2_to_v3.packing";
    public int FromVersion => 2;
    public int ToVersion => 3;

    public void Apply(SaveGameDocument document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (document.FormatVersion != FromVersion)
        {
            throw new InvalidOperationException("Migration received the wrong source version.");
        }

        document.Buildings ??= new BuildingsSaveData();
        document.FormatVersion = ToVersion;
    }
}

public sealed class SaveVersionThreeAgentSkillsMigration : ISaveMigration
{
    public string Id => "save.v3_to_v4.agent_skills";
    public int FromVersion => 3;
    public int ToVersion => 4;

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

        document.AgentSkills ??= new AgentSkillsSaveData();
        document.FormatVersion = ToVersion;
    }
}
public sealed class SaveVersionFourAuthoritativeCoordinatesMigration : ISaveMigration
{
    public string Id => "save.v4_to_v5.authoritative_xyz";
    public int FromVersion => 4;
    public int ToVersion => 5;

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

        document.Inventory ??= new InventorySaveData();
        document.Inventory.Stacks ??= new List<ItemStackSaveData>();
        foreach (ItemStackSaveData stack in document.Inventory.Stacks)
        {
            if (stack?.Location is not null && stack.Location.CellX.HasValue)
            {
                stack.Location.CellZ = 0;
            }
        }

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

        document.AgentPositions ??= new AgentPositionsSaveData();
        document.TerrainDeposits ??= new TerrainDepositsSaveData();
        document.FormatVersion = ToVersion;
    }
}

}
