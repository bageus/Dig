using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using Dig.Application.Buildings;
using Dig.Application.World;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Combat;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Domain.Production;
using Dig.Domain.World;
using Dig.Domain.WorldObjects;

namespace Dig.Application.Saving
{

public static class SaveFormat
{
    public const int CurrentVersion = 14;
}

public static class SaveSlotNames
{
    public const string Autosave = "autosave";
}

[DataContract]
public sealed class SaveMetadataData
{
    [DataMember(Order = 1)] public string SlotId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string DisplayName { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string SavedAtUtc { get; set; } = string.Empty;
    [DataMember(Order = 4)] public long SimulationTick { get; set; }
    [DataMember(Order = 5)] public ulong WorldSeed { get; set; }
    [DataMember(Order = 6)] public int GeneratorVersion { get; set; }
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
    [DataMember(Order = 1)] public int FormatVersion { get; set; }
    [DataMember(Order = 2)] public SaveMetadataData Metadata { get; set; } = new SaveMetadataData();
    [DataMember(Order = 3)] public WorldSaveData World { get; set; } = new WorldSaveData();
    [DataMember(Order = 4)] public InventorySaveData Inventory { get; set; } = new InventorySaveData();
    [DataMember(Order = 5)] public JobsSaveData Jobs { get; set; } = new JobsSaveData();
    [DataMember(Order = 6)] public BuildingsSaveData Buildings { get; set; } = new BuildingsSaveData();
    [DataMember(Order = 7)] public AgentSkillsSaveData AgentSkills { get; set; } = new AgentSkillsSaveData();
    [DataMember(Order = 8)] public AgentPositionsSaveData AgentPositions { get; set; } = new AgentPositionsSaveData();
    [DataMember(Order = 9)] public TerrainDepositsSaveData TerrainDeposits { get; set; } = new TerrainDepositsSaveData();
    [DataMember(Order = 10)] public MiningOutputCommitsSaveData MiningOutput { get; set; } = new MiningOutputCommitsSaveData();
    [DataMember(Order = 11)] public PackableBuildingExecutionsSaveData PackableBuildingExecutions { get; set; } = new PackableBuildingExecutionsSaveData();
    [DataMember(Order = 12)] public MushroomSaveData Mushrooms { get; set; } = new MushroomSaveData();
    [DataMember(Order = 13)] public BuildingProductionSaveData BuildingProduction { get; set; } = new BuildingProductionSaveData();
    [DataMember(Order = 14)] public BarrelSaveData Barrels { get; set; } = new BarrelSaveData();
    [DataMember(Order = 15)] public AgentRuntimeSaveData AgentRuntime { get; set; } = new AgentRuntimeSaveData();
    [DataMember(Order = 16)] public CombatSaveData Combat { get; set; } = new CombatSaveData();
    [DataMember(Order = 17)] public LivingMaterialEcologySaveData LivingMaterials { get; set; } = new LivingMaterialEcologySaveData();
    [DataMember(Order = 18)] public VukerEcologySaveData Vukers { get; set; } = new VukerEcologySaveData();
}

public sealed class LoadedGameState
{
    public LoadedGameState(
        SaveMetadataData metadata,
        WorldState world,
        InventoryState inventory,
        Domain.Jobs.JobSystem jobs,
        BuildingsState buildings,
        SaveMigrationReport migrationReport,
        IReadOnlyDictionary<EntityId, AgentSkillProgressionSnapshot>? agentSkills = null,
        IReadOnlyDictionary<EntityId, bool>? agentAutomaticPlanning = null,
        IReadOnlyDictionary<EntityId, CellId>? agentPositions = null,
        IReadOnlyCollection<TerrainDepositInstance>? terrainDeposits = null,
        PackableBuildingExecutionRegistry? packableBuildingExecutions = null,
        RestoredMiningOutputState? miningOutput = null,
        MushroomState? mushrooms = null,
        ProductionState? production = null,
        BuildingSupplyState? buildingSupply = null,
        BarrelState? barrels = null,
        IReadOnlyDictionary<EntityId, AgentRuntimeSnapshot>? agentRuntime = null,
        CombatState? combat = null,
        int? terrainDepositGeneratorVersion = null,
        LivingMaterialEcologyState? livingMaterials = null,
        VukerEcologyState? vukers = null)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        World = world ?? throw new ArgumentNullException(nameof(world));
        Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        Jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        Buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
        MigrationReport = migrationReport ?? throw new ArgumentNullException(nameof(migrationReport));
        AgentSkills = Copy(agentSkills);
        AgentAutomaticPlanning = Copy(agentAutomaticPlanning);
        AgentPositions = Copy(agentPositions);
        AgentRuntime = Copy(agentRuntime);
        if (terrainDeposits != null)
        {
            int generatorVersion = terrainDepositGeneratorVersion
                ?? metadata.GeneratorVersion;
            World.ReplaceTerrainDeposits(terrainDeposits, generatorVersion);
        }

        TerrainDeposits = new ReadOnlyCollection<TerrainDepositInstance>(
            World.TerrainDeposits.Snapshot().ToArray());
        TerrainDepositGeneratorVersion = World.TerrainDeposits.GeneratorVersion;
        PackableBuildingExecutions = packableBuildingExecutions
            ?? new PackableBuildingExecutionRegistry();
        if (miningOutput is null)
        {
            MiningOutputCommitState emptyCommits = new MiningOutputCommitState();
            miningOutput = new RestoredMiningOutputState(
                emptyCommits,
                new MiningOutputIntegrityDiagnostics().Inspect(emptyCommits, inventory));
        }

        MiningOutput = miningOutput;
        Mushrooms = mushrooms ?? new MushroomState(
            new MushroomCatalog(Array.Empty<MushroomDefinition>()));
        Production = production ?? new ProductionState();
        BuildingSupply = buildingSupply ?? new BuildingSupplyState();
        Barrels = barrels ?? new BarrelState(
            new BarrelCatalog(Array.Empty<BarrelDefinition>()));
        Combat = combat;
        LivingMaterials = livingMaterials ?? new LivingMaterialEcologyState(metadata.WorldSeed);
        Vukers = vukers ?? new VukerEcologyState(metadata.WorldSeed);
    }

    public SaveMetadataData Metadata { get; }
    public WorldState World { get; }
    public InventoryState Inventory { get; }
    public Domain.Jobs.JobSystem Jobs { get; }
    public BuildingsState Buildings { get; }
    public SaveMigrationReport MigrationReport { get; }
    public IReadOnlyDictionary<EntityId, AgentSkillProgressionSnapshot> AgentSkills { get; }
    public IReadOnlyDictionary<EntityId, bool> AgentAutomaticPlanning { get; }
    public IReadOnlyDictionary<EntityId, CellId> AgentPositions { get; }
    public IReadOnlyDictionary<EntityId, AgentRuntimeSnapshot> AgentRuntime { get; }
    public IReadOnlyList<TerrainDepositInstance> TerrainDeposits { get; }
    public int TerrainDepositGeneratorVersion { get; }
    public PackableBuildingExecutionRegistry PackableBuildingExecutions { get; }
    public RestoredMiningOutputState MiningOutput { get; }
    public MushroomState Mushrooms { get; }
    public ProductionState Production { get; }
    public BuildingSupplyState BuildingSupply { get; }
    public BarrelState Barrels { get; }
    public CombatState? Combat { get; }
    public LivingMaterialEcologyState LivingMaterials { get; }
    public VukerEcologyState Vukers { get; }

    private static IReadOnlyDictionary<TKey, TValue> Copy<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue>? values) where TKey : notnull
    {
        return new ReadOnlyDictionary<TKey, TValue>(values is null
            ? new Dictionary<TKey, TValue>()
            : values.ToDictionary(value => value.Key, value => value.Value));
    }
}

public sealed class SaveMigrationReport
{
    public SaveMigrationReport(IEnumerable<string> appliedSteps)
    {
        if (appliedSteps is null)
        {
            throw new ArgumentNullException(nameof(appliedSteps));
        }

        AppliedSteps = new ReadOnlyCollection<string>(new List<string>(appliedSteps));
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
    bool CanEncode(Domain.Jobs.JobDefinition definition);
    JobDefinitionSaveData Encode(Domain.Jobs.JobDefinition definition);
    Domain.Jobs.JobDefinition Decode(JobDefinitionSaveData data);
}

}
