using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public static class SaveFormat
{
    public const int CurrentVersion = 5;
}

public static class SaveSlotNames
{
    public const string Autosave = "autosave";
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

public sealed class SaveSlotInfo
{
    public SaveSlotInfo(
        string slotId,
        SaveMetadataData? metadata,
        bool isCorrupted,
        string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(slotId))
        {
            throw new ArgumentException("Save slot id is required.", nameof(slotId));
        }

        if (isCorrupted && string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException(
                "A corrupted save slot requires an error message.",
                nameof(errorMessage));
        }

        if (!isCorrupted && metadata is null)
        {
            throw new ArgumentException(
                "A healthy save slot requires metadata.",
                nameof(metadata));
        }

        SlotId = slotId;
        Metadata = metadata;
        IsCorrupted = isCorrupted;
        ErrorMessage = errorMessage;
    }

    public string SlotId { get; }
    public SaveMetadataData? Metadata { get; }
    public bool IsCorrupted { get; }
    public string? ErrorMessage { get; }
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

    [DataMember(Order = 6)]
    public BuildingsSaveData Buildings { get; set; } = new BuildingsSaveData();

    [DataMember(Order = 7)]
    public AgentSkillsSaveData AgentSkills { get; set; } = new AgentSkillsSaveData();

    [DataMember(Order = 8)]
    public AgentPositionsSaveData AgentPositions { get; set; } = new AgentPositionsSaveData();

    [DataMember(Order = 9)]
    public TerrainDepositsSaveData TerrainDeposits { get; set; } =
        new TerrainDepositsSaveData();

    [DataMember(Order = 10)]
    public MiningOutputCommitsSaveData MiningOutput { get; set; } =
        new MiningOutputCommitsSaveData();
}

public sealed class LoadedGameState
{
    public LoadedGameState(
        SaveMetadataData metadata,
        WorldState world,
        InventoryState inventory,
        JobSystem jobs,
        BuildingsState buildings,
        SaveMigrationReport migrationReport,
        IReadOnlyDictionary<EntityId, AgentSkillProgressionSnapshot>? agentSkills = null,
        IReadOnlyDictionary<EntityId, bool>? agentAutomaticPlanning = null,
        IReadOnlyDictionary<EntityId, CellId>? agentPositions = null,
        IReadOnlyCollection<TerrainDepositInstance>? terrainDeposits = null)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        World = world ?? throw new ArgumentNullException(nameof(world));
        Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        Jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        Buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
        MigrationReport = migrationReport ?? throw new ArgumentNullException(nameof(migrationReport));
        Dictionary<EntityId, AgentSkillProgressionSnapshot> skillCopy = agentSkills is null
            ? new Dictionary<EntityId, AgentSkillProgressionSnapshot>()
            : agentSkills.ToDictionary(value => value.Key, value => value.Value);
        AgentSkills = new ReadOnlyDictionary<EntityId, AgentSkillProgressionSnapshot>(
            skillCopy);
        Dictionary<EntityId, bool> planningCopy = agentAutomaticPlanning is null
            ? new Dictionary<EntityId, bool>()
            : agentAutomaticPlanning.ToDictionary(value => value.Key, value => value.Value);
        AgentAutomaticPlanning = new ReadOnlyDictionary<EntityId, bool>(planningCopy);
        Dictionary<EntityId, CellId> positionCopy = agentPositions is null
            ? new Dictionary<EntityId, CellId>()
            : agentPositions.ToDictionary(value => value.Key, value => value.Value);
        AgentPositions = new ReadOnlyDictionary<EntityId, CellId>(positionCopy);
        TerrainDeposits = new ReadOnlyCollection<TerrainDepositInstance>(
            (terrainDeposits ?? Array.Empty<TerrainDepositInstance>())
                .OrderBy(value => value.Cell)
                .ThenBy(value => value.InstanceId, StringComparer.Ordinal)
                .ToArray());
    }

    public SaveMetadataData Metadata { get; }
    public WorldState World { get; }
    public InventoryState Inventory { get; }
    public JobSystem Jobs { get; }
    public BuildingsState Buildings { get; }
    public SaveMigrationReport MigrationReport { get; }
    public IReadOnlyDictionary<EntityId, AgentSkillProgressionSnapshot> AgentSkills { get; }
    public IReadOnlyDictionary<EntityId, bool> AgentAutomaticPlanning { get; }
    public IReadOnlyDictionary<EntityId, CellId> AgentPositions { get; }
    public IReadOnlyList<TerrainDepositInstance> TerrainDeposits { get; }
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

    IReadOnlyList<SaveSlotInfo> List();
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
        : this(metadata, world, inventory, jobs, new BuildingsState())
    {
    }

    public SaveGameContext(
        SaveMetadataData metadata,
        WorldState world,
        InventoryState inventory,
        JobSystem jobs,
        BuildingsState buildings)
        : this(
            metadata,
            world,
            inventory,
            jobs,
            buildings,
            Array.Empty<AgentState>(),
            Array.Empty<TerrainDepositInstance>())
    {
    }

    public SaveGameContext(
        SaveMetadataData metadata,
        WorldState world,
        InventoryState inventory,
        JobSystem jobs,
        BuildingsState buildings,
        IReadOnlyCollection<AgentState> agents,
        IReadOnlyCollection<TerrainDepositInstance>? terrainDeposits = null)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        World = world ?? throw new ArgumentNullException(nameof(world));
        Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        Jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        Buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
        Agents = new ReadOnlyCollection<AgentState>(
            (agents ?? throw new ArgumentNullException(nameof(agents))).ToList());
        TerrainDeposits = new ReadOnlyCollection<TerrainDepositInstance>(
            (terrainDeposits ?? Array.Empty<TerrainDepositInstance>())
                .OrderBy(value => value.Cell)
                .ThenBy(value => value.InstanceId, StringComparer.Ordinal)
                .ToList());
    }

    public SaveMetadataData Metadata { get; }
    public WorldState World { get; }
    public InventoryState Inventory { get; }
    public JobSystem Jobs { get; }
    public BuildingsState Buildings { get; }
    public IReadOnlyList<AgentState> Agents { get; }
    public IReadOnlyList<TerrainDepositInstance> TerrainDeposits { get; }
}

}
