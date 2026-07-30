using System;
using Dig.Application.Saving;

namespace Dig.Infrastructure.Saving
{

public static class SaveGameCompositionRoot
{
    private static readonly Type[] ExpectedJobDefinitionTypes =
    {
        typeof(Dig.Domain.Jobs.BarrelAttackJobDefinition),
        typeof(Dig.Domain.Jobs.BuildingBoxAssemblyJobDefinition),
        typeof(Dig.Domain.Jobs.BuildingBoxPackingJobDefinition),
        typeof(Dig.Domain.Jobs.BuildingBoxPickupJobDefinition),
        typeof(Dig.Domain.Jobs.BuildingSupplyJobDefinition),
        typeof(Dig.Domain.Jobs.BuildingWorkJobDefinition),
        typeof(Dig.Domain.Jobs.DigJobDefinition),
        typeof(Dig.Domain.Jobs.HaulJobDefinition),
        typeof(Dig.Domain.Jobs.HealingJobDefinition),
        typeof(Dig.Domain.Jobs.MushroomChopJobDefinition),
        typeof(Dig.Domain.Jobs.ProductionWorkJobDefinition),
        typeof(Dig.Domain.Jobs.ResidentInventoryPlacementJobDefinition),
        typeof(Dig.Domain.Jobs.SpatialDigJobDefinition),
        typeof(Dig.Domain.Jobs.StrategicExecutionJobDefinition),
        typeof(Dig.Domain.Jobs.WorldItemPickupJobDefinition),
    };

    public static SaveGameService Create(string saveDirectory)
    {
        JobDefinitionSaveRegistry jobs = CreateJobDefinitionRegistry();
        return new SaveGameService(
            new SaveGameBuilder(jobs),
            new SaveGameLoader(CreateMigrationPipeline(), jobs),
            new FileSaveSlotStore(
                saveDirectory,
                new DataContractJsonSaveCodec()));
    }

    public static JobDefinitionSaveRegistry CreateJobDefinitionRegistry()
    {
        JobDefinitionSaveRegistry registry = new JobDefinitionSaveRegistry(
            new JobDefinitionSaveRegistration[]
            {
                Registration<Dig.Domain.Jobs.BarrelAttackJobDefinition>(
                    new BarrelAttackJobSaveCodec()),
                Registration<Dig.Domain.Jobs.BuildingBoxAssemblyJobDefinition>(
                    new BuildingBoxAssemblyJobSaveCodec()),
                Registration<Dig.Domain.Jobs.BuildingBoxPackingJobDefinition>(
                    new BuildingBoxPackingJobSaveCodec()),
                Registration<Dig.Domain.Jobs.BuildingBoxPickupJobDefinition>(
                    new BuildingBoxPickupJobSaveCodec()),
                Registration<Dig.Domain.Jobs.BuildingSupplyJobDefinition>(
                    new BuildingSupplyJobSaveCodec()),
                Registration<Dig.Domain.Jobs.BuildingWorkJobDefinition>(
                    new BuildingWorkJobSaveCodec()),
                Registration<Dig.Domain.Jobs.DigJobDefinition>(
                    new DigJobDefinitionSaveCodec()),
                Registration<Dig.Domain.Jobs.HaulJobDefinition>(
                    new HaulJobDefinitionSaveCodec()),
                Registration<Dig.Domain.Jobs.HealingJobDefinition>(
                    new HealingJobSaveCodec()),
                Registration<Dig.Domain.Jobs.MushroomChopJobDefinition>(
                    new MushroomChopJobSaveCodec()),
                Registration<Dig.Domain.Jobs.ProductionWorkJobDefinition>(
                    new ProductionWorkJobSaveCodec()),
                Registration<Dig.Domain.Jobs.ResidentInventoryPlacementJobDefinition>(
                    new ResidentInventoryPlacementJobSaveCodec()),
                Registration<Dig.Domain.Jobs.SpatialDigJobDefinition>(
                    new SpatialDigJobSaveCodec()),
                Registration<Dig.Domain.Jobs.StrategicExecutionJobDefinition>(
                    new StrategicExecutionJobSaveCodec()),
                Registration<Dig.Domain.Jobs.WorldItemPickupJobDefinition>(
                    new WorldItemPickupJobSaveCodec()),
            });
        registry.ValidateCoverage(ExpectedJobDefinitionTypes);
        return registry;
    }

    public static SaveMigrationPipeline CreateMigrationPipeline()
    {
        return new SaveMigrationPipeline(new ISaveMigration[]
        {
            new LegacySaveVersionZeroMigration(),
            new SaveVersionOneBuildingsMigration(),
            new SaveVersionTwoPackingMigration(),
            new SaveVersionThreeAgentSkillsMigration(),
            new SaveVersionFourAuthoritativeCoordinatesMigration(),
            new SaveVersionFiveMushroomsMigration(),
            new SaveVersionSixBuildingProductionMigration(),
            new SaveVersionSevenWorldExcavationProgressMigration(),
            new SaveVersionEightAgentRuntimeMigration(),
            new SaveVersionNineCombatSpatialMigration(),
            new SaveVersionTenTerrainDepositContractMigration(),
            new SaveVersionElevenLivingMaterialsMigration(),
        });
    }

    private static JobDefinitionSaveRegistration Registration<TDefinition>(
        IJobDefinitionSaveCodec codec)
        where TDefinition : Dig.Domain.Jobs.JobDefinition
    {
        return new JobDefinitionSaveRegistration(typeof(TDefinition), codec);
    }
}

}
