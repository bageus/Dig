using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public static class SaveFormat
{
    public const int CurrentVersion = 1;
}

[DataContract]
public sealed class SaveMetadataData
{
    [DataMember(Order = 1)]
    public string SlotId { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string SavedAtUtc { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public long SimulationTick { get; set; }

    [DataMember(Order = 5)]
    public ulong WorldSeed { get; set; }

    [DataMember(Order = 6)]
    public int GeneratorVersion { get; set; }
}

[DataContract]
public sealed class SaveGameDocument
{
    [DataMember(Order = 1)]
    public int FormatVersion { get; set; }

    [DataMember(Order = 2)]
    public SaveMetadataData Metadata { get; set; } = new SaveMetadataData();

    [DataMember(Order = 3)]
    public WorldSaveData World { get; set; } = new WorldSaveData();

    [DataMember(Order = 4)]
    public InventorySaveData Inventory { get; set; } = new InventorySaveData();

    [DataMember(Order = 5)]
    public JobsSaveData Jobs { get; set; } = new JobsSaveData();
}

public sealed class LoadedGameState
{
    public LoadedGameState(
        SaveMetadataData metadata,
        WorldState world,
        InventoryState inventory,
        JobSystem jobs,
        SaveMigrationReport migrationReport)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        World = world ?? throw new ArgumentNullException(nameof(world));
        Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        Jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        MigrationReport = migrationReport ?? throw new ArgumentNullException(nameof(migrationReport));
    }

    public SaveMetadataData Metadata { get; }
    public WorldState World { get; }
    public InventoryState Inventory { get; }
    public JobSystem Jobs { get; }
    public SaveMigrationReport MigrationReport { get; }
}

public sealed class SaveMigrationReport
{
    public SaveMigrationReport(IEnumerable<string> appliedSteps)
    {
        if (appliedSteps is null)
        {
            throw new ArgumentNullException(nameof(appliedSteps));
        }

        AppliedSteps = new ReadOnlyCollection<string>(
            new List<string>(appliedSteps));
    }

    public IReadOnlyList<string> AppliedSteps { get; }
    public bool Migrated => AppliedSteps.Count > 0;
}

public interface ISaveGameCodec
{
    byte[] Serialize(SaveGameDocument document);

    SaveGameDocument Deserialize(byte[] bytes);
}

public interface ISaveSlotStore
{
    void Save(string slotId, SaveGameDocument document);

    SaveGameDocument Load(string slotId);

    IReadOnlyList<SaveMetadataData> List();
}

public interface ISaveMigration
{
    string Id { get; }
    int FromVersion { get; }
    int ToVersion { get; }

    void Apply(SaveGameDocument document);
}

public interface IJobDefinitionSaveCodec
{
    string TypeId { get; }

    bool CanEncode(JobDefinition definition);

    JobDefinitionSaveData Encode(JobDefinition definition);

    JobDefinition Decode(JobDefinitionSaveData data);
}

public sealed class SaveGameContext
{
    public SaveGameContext(
        SaveMetadataData metadata,
        WorldState world,
        InventoryState inventory,
        JobSystem jobs)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        World = world ?? throw new ArgumentNullException(nameof(world));
        Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        Jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
    }

    public SaveMetadataData Metadata { get; }
    public WorldState World { get; }
    public InventoryState Inventory { get; }
    public JobSystem Jobs { get; }
}
}
