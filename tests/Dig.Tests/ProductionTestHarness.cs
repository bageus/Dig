using Dig.Application.Buildings;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Production;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Dig.Domain.Technology;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests;

internal sealed class ProductionTestHarness
{
    public static readonly ItemId Ore = new ItemId("ore.copper");
    public static readonly ItemId Plate = new ItemId("plate.copper");
    public static readonly ItemId Tool = new ItemId("tool.hammer");
    public static readonly BuildingDefinitionId Workshop =
        new BuildingDefinitionId("workshop.smithy");
    public static readonly EntityId BuildingId =
        EntityId.Parse("81000000000000000000000000000001");
    public static readonly EntityId OreStackId =
        EntityId.Parse("82000000000000000000000000000001");
    public static readonly EntityId ToolStackId =
        EntityId.Parse("82000000000000000000000000000002");
    public static readonly EntityId WorkerId =
        EntityId.Parse("83000000000000000000000000000001");

    public ProductionTestHarness(
        IEnumerable<RecipeDefinition> recipes,
        IEnumerable<TechnologyDefinition>? technologies = null,
        bool includeTool = true,
        bool energyAvailable = true)
    {
        Items = ProductionContentCatalogTests.CreateItems();
        BuildingCatalog buildingCatalog = ProductionContentCatalogTests.CreateBuildings();
        ContentValidationResult content = ProductionContentCatalog.ValidateAndCreate(
            Items,
            buildingCatalog,
            recipes,
            technologies ?? Array.Empty<TechnologyDefinition>());
        Assert.True(content.Succeeded, string.Join("\n", content.Issues));
        Content = content.Catalog!;
        Inventory = new InventoryState(Items);
        ItemLocation location = ItemLocation.InBuilding(BuildingId);
        Assert.True(Inventory.AddStack(OreStackId, Ore, 10, location, tick: 0).IsSuccess);
        if (includeTool)
        {
            Assert.True(Inventory.AddStack(ToolStackId, Tool, 1, location, tick: 0).IsSuccess);
        }

        Buildings = CreateCompletedBuilding(buildingCatalog.Get(Workshop));
        Production = new ProductionState();
        Technology = new TechnologyState();
        Jobs = new JobSystem();
        InventoryRepository = new InMemoryInventoryRepository(Inventory);
        BuildingsRepository = new InMemoryBuildingsRepository(Buildings);
        ProductionRepository = new InMemoryProductionRepository(Production);
        TechnologyRepository = new InMemoryTechnologyRepository(Technology);
        JobRepository = new InMemoryJobRepository(Jobs);
        Candidates = new InMemoryJobCandidateProvider();
        Energy = new FixedEnergyAvailability(energyAvailable);
        Journal = new InMemoryExecutionJournal();
    }

    public ItemCatalog Items { get; }

    public ProductionContentCatalog Content { get; }

    public InventoryState Inventory { get; }

    public BuildingsState Buildings { get; }

    public ProductionState Production { get; }

    public TechnologyState Technology { get; }

    public JobSystem Jobs { get; }

    public InMemoryInventoryRepository InventoryRepository { get; }

    public InMemoryBuildingsRepository BuildingsRepository { get; }

    public InMemoryProductionRepository ProductionRepository { get; }

    public InMemoryTechnologyRepository TechnologyRepository { get; }

    public InMemoryJobRepository JobRepository { get; }

    public InMemoryJobCandidateProvider Candidates { get; }

    public FixedEnergyAvailability Energy { get; }

    public InMemoryExecutionJournal Journal { get; }

    public Result Enqueue(EntityId orderId, RecipeId recipeId, long tick)
    {
        return new EnqueueProductionOrderHandler(Content, ProductionRepository).Handle(
            new EnqueueProductionOrderCommand(orderId, recipeId, BuildingId, tick));
    }

    public Result Prepare(EntityId jobId, long tick)
    {
        return new PrepareProductionOrderHandler(
            ProductionRepository,
            TechnologyRepository,
            BuildingsRepository,
            InventoryRepository,
            JobRepository,
            Energy,
            Journal).Handle(new PrepareProductionOrderCommand(
                jobId,
                BuildingId,
                new[] { new CellId(2, 2) },
                priority: 500,
                tick));
    }

    public void AssignAndBegin(EntityId orderId, EntityId jobId, long tick)
    {
        Candidates.SetCandidates(jobId, new[]
        {
            new JobCandidate(WorkerId, 5000, distanceCost: 1, isAvailable: true),
        });
        JobAssignmentReport report = new AssignAvailableJobsHandler(
            JobRepository,
            Candidates,
            Journal).Handle(new AssignAvailableJobsCommand(tick));
        Assert.Single(report.Assignments);
        Assert.True(new BeginProductionWorkHandler(
            ProductionRepository,
            JobRepository,
            Journal).Handle(new BeginProductionWorkCommand(
                orderId,
                jobId,
                tick + 1)).IsSuccess);
        Assert.True(new AdvanceJobHandler(JobRepository, Journal).Handle(
            new AdvanceJobCommand(jobId, tick + 2)).IsSuccess);
        Assert.Equal(JobStageKind.PerformWork, Jobs.Get(jobId)!.Stage);
    }

    public Result ApplyWork(EntityId orderId, EntityId jobId, long tick)
    {
        return new ApplyProductionWorkHandler(
            ProductionRepository,
            JobRepository,
            Journal).Handle(new ApplyProductionWorkCommand(
                orderId,
                jobId,
                baseWork: 8,
                new ProductionWorkContext(15_000, 10_000),
                tick));
    }

    public Result Complete(
        EntityId orderId,
        EntityId jobId,
        EntityId outputStackId,
        long tick)
    {
        return new CompleteProductionOrderHandler(
            ProductionRepository,
            InventoryRepository,
            JobRepository,
            Journal).Handle(new CompleteProductionOrderCommand(
                orderId,
                jobId,
                new[] { outputStackId },
                tick));
    }

    private static BuildingsState CreateCompletedBuilding(BuildingDefinition definition)
    {
        BuildingsState buildings = new BuildingsState();
        EntityId buildingId = BuildingId;
        CellId cell = new CellId(2, 2);
        Assert.True(buildings.Place(
            buildingId,
            definition,
            cell,
            BuildingOrientation.North,
            BuildingPlacementResult.Success(new[] { cell }, cell),
            tick: 0).IsSuccess);
        Assert.True(buildings.MarkMaterialsReady(buildingId).IsSuccess);
        Assert.True(buildings.StartConstruction(buildingId).IsSuccess);
        Assert.True(buildings.AddConstructionWork(
            buildingId,
            definition.RequiredWork).IsSuccess);
        Assert.True(buildings.Complete(buildingId, tick: 0).IsSuccess);
        buildings.DequeueUncommittedEvents();
        return buildings;
    }
}
