using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.Buildings;
using Dig.Application.Tunnels;
using Dig.Application.World;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Combat;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Dig.Domain.World;
using Dig.Domain.WorldObjects;

namespace Dig.Application.Saving
{

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
            terrainDeposits: null)
    {
    }

    public SaveGameContext(
        SaveMetadataData metadata,
        WorldState world,
        InventoryState inventory,
        JobSystem jobs,
        BuildingsState buildings,
        IReadOnlyCollection<AgentState> agents,
        IReadOnlyCollection<TerrainDepositInstance>? terrainDeposits = null,
        PackableBuildingExecutionRegistry? packableBuildingExecutions = null,
        MiningOutputCommitState? miningOutputCommits = null,
        MushroomState? mushrooms = null,
        ProductionState? production = null,
        BuildingSupplyState? buildingSupply = null,
        BarrelState? barrels = null,
        CombatState? combat = null,
        int? terrainDepositGeneratorVersion = null,
        LivingMaterialEcologyState? livingMaterials = null,
        VukerEcologyState? vukers = null,
        TunnelInfrastructureRuntimeSnapshot? tunnelInfrastructure = null)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        World = world ?? throw new ArgumentNullException(nameof(world));
        Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        Jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        Buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
        Agents = new ReadOnlyCollection<AgentState>(
            (agents ?? throw new ArgumentNullException(nameof(agents))).ToList());
        if (terrainDeposits != null)
        {
            int generatorVersion = terrainDepositGeneratorVersion
                ?? metadata.GeneratorVersion;
            World.ReplaceTerrainDeposits(terrainDeposits, generatorVersion);
        }

        TerrainDeposits = new ReadOnlyCollection<TerrainDepositInstance>(
            World.TerrainDeposits.Snapshot().ToList());
        TerrainDepositGeneratorVersion = World.TerrainDeposits.GeneratorVersion;
        PackableBuildingExecutions = packableBuildingExecutions
            ?? new PackableBuildingExecutionRegistry();
        MiningOutputCommits = miningOutputCommits ?? new MiningOutputCommitState();
        Mushrooms = mushrooms ?? new MushroomState(
            new MushroomCatalog(Array.Empty<MushroomDefinition>()));
        Production = production ?? new ProductionState();
        BuildingSupply = buildingSupply ?? new BuildingSupplyState();
        Barrels = barrels ?? new BarrelState(
            new BarrelCatalog(Array.Empty<BarrelDefinition>()));
        Combat = combat;
        LivingMaterials = livingMaterials ?? new LivingMaterialEcologyState(metadata.WorldSeed);
        Vukers = vukers ?? new VukerEcologyState(metadata.WorldSeed);
        TunnelInfrastructure = tunnelInfrastructure
            ?? TunnelInfrastructureRuntimeSnapshot.Empty();
    }

    public SaveMetadataData Metadata { get; }
    public WorldState World { get; }
    public InventoryState Inventory { get; }
    public JobSystem Jobs { get; }
    public BuildingsState Buildings { get; }
    public IReadOnlyList<AgentState> Agents { get; }
    public IReadOnlyList<TerrainDepositInstance> TerrainDeposits { get; }
    public int TerrainDepositGeneratorVersion { get; }
    public PackableBuildingExecutionRegistry PackableBuildingExecutions { get; }
    public MiningOutputCommitState MiningOutputCommits { get; }
    public MushroomState Mushrooms { get; }
    public ProductionState Production { get; }
    public BuildingSupplyState BuildingSupply { get; }
    public BarrelState Barrels { get; }
    public CombatState? Combat { get; }
    public LivingMaterialEcologyState LivingMaterials { get; }
    public VukerEcologyState Vukers { get; }
    public TunnelInfrastructureRuntimeSnapshot TunnelInfrastructure { get; }
}

}
