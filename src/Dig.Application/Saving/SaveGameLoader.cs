using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.Buildings;
using Dig.Application.Tunnels;
using Dig.Application.World;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Dig.Domain.World;
using Dig.Domain.WorldObjects;
using Dig.Domain.Society;
using Dig.Domain.Exploration;
namespace Dig.Application.Saving
{
public sealed partial class SaveGameLoader
{
    private readonly SaveMigrationPipeline _migrations;
    private readonly JobDefinitionSaveRegistry _jobDefinitions;
    public SaveGameLoader(
        SaveMigrationPipeline migrations,
        JobDefinitionSaveRegistry jobDefinitions)
    {
        _migrations = migrations ?? throw new ArgumentNullException(nameof(migrations));
        _jobDefinitions = jobDefinitions
            ?? throw new ArgumentNullException(nameof(jobDefinitions));
    }
    public Result<LoadedGameState> Load(
        SaveGameDocument document,
        MaterialCatalog materials,
        ItemCatalog items)
    {
        return Load(
            document,
            materials,
            items,
            buildingCatalog: null,
            terrainDepositCatalog: null,
            mushroomCatalog: null,
            productionContent: null,
            barrelCatalog: null);
    }
    public Result<LoadedGameState> Load(
        SaveGameDocument document,
        MaterialCatalog materials,
        ItemCatalog items,
        BuildingCatalog? buildingCatalog)
    {
        return Load(
            document,
            materials,
            items,
            buildingCatalog,
            terrainDepositCatalog: null,
            mushroomCatalog: null,
            productionContent: null,
            barrelCatalog: null);
    }
    public Result<LoadedGameState> Load(
        SaveGameDocument document,
        MaterialCatalog materials,
        ItemCatalog items,
        BuildingCatalog? buildingCatalog,
        TerrainDepositCatalog? terrainDepositCatalog)
    {
        return Load(
            document,
            materials,
            items,
            buildingCatalog,
            terrainDepositCatalog,
            mushroomCatalog: null,
            productionContent: null,
            barrelCatalog: null);
    }
    public Result<LoadedGameState> Load(
        SaveGameDocument document,
        MaterialCatalog materials,
        ItemCatalog items,
        BuildingCatalog? buildingCatalog,
        TerrainDepositCatalog? terrainDepositCatalog,
        MushroomCatalog? mushroomCatalog,
        ProductionContentCatalog? productionContent = null,
        BarrelCatalog? barrelCatalog = null,
        WeaponCatalog? combatWeapons = null)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        try
        {
            Result<SaveMigrationReport> migration = _migrations.Apply(document);
            if (migration.IsFailure)
            {
                return Result<LoadedGameState>.Failure(migration.Error!);
            }

            List<string> migrationSteps = migration.Value.AppliedSteps.ToList();
            migrationSteps.AddRange(
                AgentSkillSaveDataMigrator.Apply(document.AgentSkills));
            SaveMigrationReport migrationReport = new SaveMigrationReport(
                migrationSteps);

            ValidateMetadata(document.Metadata);
            Result<WorldState> world = WorldState.Restore(
                BuildWorldSnapshot(document.World, materials),
                materials);
            if (world.IsFailure)
            {
                return Result<LoadedGameState>.Failure(world.Error!);
            }
            Result<InventoryState> inventory = InventoryState.Restore(
                BuildInventorySnapshot(document.Inventory),
                items);
            if (inventory.IsFailure)
            {
                return Result<LoadedGameState>.Failure(inventory.Error!);
            }
            Result<JobSystem> jobs = BuildJobSystem(document.Jobs);
            if (jobs.IsFailure)
            {
                return Result<LoadedGameState>.Failure(jobs.Error!);
            }
            Result<RestoredInfrastructureRuntime> infrastructure =
                RestoreInfrastructure(
                    document, inventory.Value, jobs.Value, world.Value.Size);
            if (infrastructure.IsFailure)
            {
                return Result<LoadedGameState>.Failure(infrastructure.Error!);
            }
            Result<BuildingsState> buildings = BuildBuildingsState(
                document.Buildings,
                buildingCatalog);
            if (buildings.IsFailure)
            {
                return Result<LoadedGameState>.Failure(buildings.Error!);
            }
            Result<MushroomState> mushrooms = BuildMushroomState(
                document.Mushrooms,
                mushroomCatalog,
                jobs.Value);
            if (mushrooms.IsFailure)
            {
                return Result<LoadedGameState>.Failure(mushrooms.Error!);
            }
            Result<BarrelState> barrels = BuildBarrelState(
                document.Barrels,
                barrelCatalog);
            if (barrels.IsFailure)
            {
                return Result<LoadedGameState>.Failure(barrels.Error!);
            }
            RestoredBuildingProductionState buildingProduction;
            bool hasProduction = document.BuildingProduction?.Orders?.Count > 0
                || document.BuildingProduction?.Supplies?.Count > 0;
            if (!hasProduction)
            {
                buildingProduction = new RestoredBuildingProductionState(
                    new ProductionState(),
                    new BuildingSupplyState());
            }
            else
            {
                if (productionContent is null)
                {
                    return Result<LoadedGameState>.Failure(SaveErrors.InvalidDocument);
                }

                Result<RestoredBuildingProductionState> restoredProduction =
                    BuildingProductionSaveAdapter.Decode(
                        document.BuildingProduction,
                        productionContent,
                        inventory.Value);
                if (restoredProduction.IsFailure)
                {
                    return Result<LoadedGameState>.Failure(restoredProduction.Error!);
                }

                buildingProduction = restoredProduction.Value;
            }
            Result references = ValidateCrossReferences(
                inventory.Value,
                jobs.Value,
                buildingProduction.Production);
            if (references.IsFailure)
            {
                return Result<LoadedGameState>.Failure(references.Error!);
            }
            references = ValidateBuildingReferences(
                inventory.Value,
                jobs.Value,
                buildings.Value);
            if (references.IsFailure)
            {
                return Result<LoadedGameState>.Failure(references.Error!);
            }
            Result<PackableBuildingExecutionRegistry> packableExecutions =
                RestorePackableBuildingExecutions(
                    document.PackableBuildingExecutions,
                    jobs.Value,
                    buildings.Value);
            if (packableExecutions.IsFailure)
            {
                return Result<LoadedGameState>.Failure(packableExecutions.Error!);
            }
            IReadOnlyDictionary<EntityId, Dig.Domain.Agents.AgentSkillProgressionSnapshot>
                agentSkills = BuildAgentSkills(document.AgentSkills);
            IReadOnlyDictionary<EntityId, bool> agentAutomaticPlanning =
                BuildAgentAutomaticPlanning(document.AgentSkills);
            IReadOnlyDictionary<EntityId, CellId> agentPositions =
                BuildAgentPositions(document.AgentPositions, document.World);
            var agentSurfacePoses = BuildAgentSurfacePoses(document.AgentPositions, document.World);
            IReadOnlyDictionary<EntityId, Dig.Domain.Agents.AgentRuntimeSnapshot>
                agentRuntime = BuildAgentRuntime(
                    document.AgentRuntime,
                    document.Metadata.SimulationTick);
            IReadOnlyCollection<TerrainDepositInstance> terrainDeposits =
                BuildTerrainDeposits(
                    document.TerrainDeposits,
                    document.World,
                    terrainDepositCatalog);
            world.Value.ReplaceTerrainDeposits(
                terrainDeposits,
                document.TerrainDeposits.GeneratorVersion);
            CombatState? combat = null;
            bool hasCombat = document.Combat?.Intents?.Count > 0
                || document.Combat?.Executions?.Count > 0
                || document.Combat?.Resolutions?.Count > 0
                || document.Combat?.Cooldowns?.Count > 0
                || document.Combat?.Statuses?.Count > 0;
            if (hasCombat)
            {
                if (combatWeapons is null)
                {
                    return Result<LoadedGameState>.Failure(SaveErrors.InvalidDocument);
                }
                Result<CombatState> restoredCombat = CombatSaveAdapter.Decode(
                    document.Combat,
                    combatWeapons);
                if (restoredCombat.IsFailure)
                {
                    return Result<LoadedGameState>.Failure(restoredCombat.Error!);
                }
                combat = restoredCombat.Value;
            }
            Result<LivingMaterialEcologyState> livingMaterials =
                LivingMaterialEcologySaveAdapter.Decode(
                    document.LivingMaterials,
                    inventory.Value,
                    document.Metadata.WorldSeed);
            if (livingMaterials.IsFailure)
            {
                return Result<LoadedGameState>.Failure(livingMaterials.Error!);
            }
            Result<VukerEcologyState> vukers = VukerEcologySaveAdapter.Decode(
                document.Vukers,
                document.Metadata.WorldSeed);
            if (vukers.IsFailure)
            {
                return Result<LoadedGameState>.Failure(vukers.Error!);
            }
            Result<SocietyState?> society = SocietySaveAdapter.Decode(document.Society);
            if (society.IsFailure)
            {
                return Result<LoadedGameState>.Failure(society.Error!);
            }
            Result<RestoredMiningOutputState> miningOutput = RestoreMiningOutput(
                document,
                inventory.Value,
                world.Value.Size);
            if (miningOutput.IsFailure)
            {
                return Result<LoadedGameState>.Failure(
                    miningOutput.Error ?? MiningOutputSaveErrors.InvalidSnapshot);
            }
            ExplorationState exploration = ExplorationSaveAdapter.Decode(
                document.Exploration, world.Value.Size);
            Result<Dig.Application.Farming.InMemoryFarmRepository> farms =
                FarmSaveAdapter.Decode(document.Farms);
            if (farms.IsFailure)
            {
                return Result<LoadedGameState>.Failure(farms.Error!);
            }
            return Result<LoadedGameState>.Success(new LoadedGameState(
                CopyMetadata(document.Metadata),
                world.Value,
                inventory.Value,
                jobs.Value,
                buildings.Value,
                migrationReport,
                agentSkills,
                agentAutomaticPlanning,
                agentPositions,
                terrainDeposits,
                packableExecutions.Value,
                miningOutput.Value,
                mushrooms.Value,
                buildingProduction.Production,
                buildingProduction.Supply,
                barrels.Value,
                agentRuntime,
                combat,
                terrainDepositGeneratorVersion:
                    document.TerrainDeposits.GeneratorVersion,
                livingMaterials: livingMaterials.Value,
                vukers: vukers.Value,
                agentSurfacePoses: agentSurfacePoses,
                tunnelInfrastructure: infrastructure.Value.Tunnel,
                roomInfrastructure: infrastructure.Value.Room,
                society: society.Value,
                exploration: exploration,
                farms: farms.Value));
        }
        catch (UnknownTerrainDepositDefinitionException)
        {
            return Result<LoadedGameState>.Failure(
                SaveErrors.UnknownTerrainDepositDefinition);
        }
        catch (UnsupportedTerrainDepositDefinitionVersionException)
        {
            return Result<LoadedGameState>.Failure(
                SaveErrors.UnsupportedTerrainDepositDefinitionVersion);
        }
        catch (KeyNotFoundException)
        {
            return Result<LoadedGameState>.Failure(SaveErrors.UnknownJobType);
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is FormatException
            || exception is OverflowException)
        {
            return Result<LoadedGameState>.Failure(SaveErrors.InvalidDocument);
        }
    }
}
}
