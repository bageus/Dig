using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Buildings;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Production;
using Dig.Domain.Agents;
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

namespace Dig.Tests
{

internal sealed class CampfireProductionTestHarness
{
    public static readonly EntityId BuildingId = Id(1);
    public static readonly EntityId WorkerId = Id(2);

    public CampfireProductionTestHarness(long materialTicks = 100)
    {
        Items = CampfireProductionContentTests.CreateItems();
        BuildingsCatalog = new BuildingCatalog(CampfireProductionContent.CreateBuildings());
        Content = ProductionContentCatalog.ValidateAndCreate(
            Items,
            BuildingsCatalog,
            CampfireProductionContent.CreateRecipes(materialTicks),
            Array.Empty<TechnologyDefinition>(),
            new[] { CampfireProductionContent.CreateWorkstation() }).Catalog!;
        Inventory = new InventoryState(Items);
        Jobs = new JobSystem();
        Buildings = CreateCompletedCampfire();
        Production = new ProductionState();
        Supply = new BuildingSupplyState();
        Supply.Register(BuildingId, Content.GetWorkstation(
            CampfireBuildingBoxContent.CampfireBuildingId), 0);
        Agents = new InMemoryAgentRepository();
        Assert.True(Agents.Add(AgentTestFactory.CreateAgent(id: WorkerId)).IsSuccess);
        Journal = new InMemoryExecutionJournal();
        ProductionRepository = new InMemoryProductionRepository(Production);
        SupplyRepository = new InMemoryBuildingSupplyRepository(Supply);
        InventoryRepository = new InMemoryInventoryRepository(Inventory);
        BuildingsRepository = new InMemoryBuildingsRepository(Buildings);
        JobsRepository = new InMemoryJobRepository(Jobs);
        TechnologyRepository = new InMemoryTechnologyRepository(new TechnologyState());
        SkillGrants = new AgentSkillGrantService(Agents, Journal);
    }

    public ItemCatalog Items { get; }
    public BuildingCatalog BuildingsCatalog { get; }
    public ProductionContentCatalog Content { get; }
    public InventoryState Inventory { get; }
    public BuildingsState Buildings { get; }
    public ProductionState Production { get; }
    public BuildingSupplyState Supply { get; }
    public JobSystem Jobs { get; }
    public InMemoryAgentRepository Agents { get; }
    public InMemoryExecutionJournal Journal { get; }
    public InMemoryProductionRepository ProductionRepository { get; }
    public InMemoryBuildingSupplyRepository SupplyRepository { get; }
    public InMemoryInventoryRepository InventoryRepository { get; }
    public InMemoryBuildingsRepository BuildingsRepository { get; }
    public InMemoryJobRepository JobsRepository { get; }
    public InMemoryTechnologyRepository TechnologyRepository { get; }
    public AgentSkillGrantService SkillGrants { get; }

    public void AddBuildingStock(ItemId itemId, int quantity, int id)
    {
        Assert.True(Inventory.AddStack(
            Id(id),
            itemId,
            quantity,
            ItemLocation.InBuilding(BuildingId),
            0).IsSuccess);
    }

    public void GrantSkill(AgentSkillId skillId, int points, long tick = 0)
    {
        Result result = Agents.Get(WorkerId)!.ApplySkillGrant(new SkillGrantBundle(
            WorkerId,
            SkillGrantSourceKind.TrainingCompleted,
            $"test:{skillId}:{points}",
            tick,
            new[] { new SkillGrant(skillId, points * AgentSkillCatalog.UnitsPerPoint) }));
        Assert.True(result.IsSuccess, result.Error?.ToString());
    }

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
            JobsRepository,
            new FixedEnergyAvailability(true),
            Journal).Handle(new PrepareProductionOrderCommand(
                jobId,
                BuildingId,
                new[] { Buildings.Get(BuildingId)!.WorkPosition },
                priority: 500,
                tick));
    }

    public void ClaimBeginAndReachWork(EntityId orderId, EntityId jobId, long tick)
    {
        Assert.True(Jobs.Claim(jobId, WorkerId, tick).IsSuccess);
        Assert.True(new BeginProductionWorkHandler(
            ProductionRepository,
            JobsRepository,
            Agents,
            Journal).Handle(new BeginProductionWorkCommand(
                orderId,
                jobId,
                tick + 1)).IsSuccess);
        Assert.True(Jobs.AdvanceStage(jobId, tick + 2).IsSuccess);
    }

    public Result Work(EntityId orderId, EntityId jobId, int elapsedTicks, long tick)
    {
        return new ApplyProductionWorkHandler(
            ProductionRepository,
            InventoryRepository,
            JobsRepository,
            Agents,
            Journal).Handle(new ApplyProductionWorkCommand(
                orderId,
                jobId,
                elapsedTicks,
                conditionEfficiencyBasisPoints: 10_000,
                tick));
    }

    public Result Complete(
        EntityId orderId,
        EntityId jobId,
        EntityId outputId,
        CellId outputCell,
        long tick)
    {
        return Complete(
            orderId,
            jobId,
            new[] { outputId },
            new[] { outputCell },
            tick);
    }

    public Result Complete(
        EntityId orderId,
        EntityId jobId,
        IReadOnlyCollection<EntityId> outputIds,
        IReadOnlyCollection<CellId> outputCells,
        long tick)
    {
        return new CompleteProductionOrderHandler(
            ProductionRepository,
            InventoryRepository,
            JobsRepository,
            Journal,
            SkillGrants).Handle(new CompleteProductionOrderCommand(
                orderId,
                jobId,
                outputIds,
                tick,
                outputLocations: outputCells
                    .Select(ItemLocation.InWorld)
                    .ToArray()));
    }

    public static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }

    private BuildingsState CreateCompletedCampfire()
    {
        BuildingDefinition definition = BuildingsCatalog.Get(
            CampfireBuildingBoxContent.CampfireBuildingId);
        CellId origin = new CellId(4, 4, 0);
        CellId work = new CellId(4, 3, 0);
        EntityId sourceBoxId = Id(900);
        EntityId assemblyJobId = Id(901);
        BuildingsState buildings = new BuildingsState();
        BuildingBoxAssemblyJobDefinition jobDefinition =
            new BuildingBoxAssemblyJobDefinition(
                assemblyJobId,
                BuildingId,
                sourceBoxId,
                origin,
                work,
                priority: 500,
                createdTick: 0,
                retryPolicy: JobRetryPolicy.Default);
        Assert.True(Jobs.Add(jobDefinition).IsSuccess);
        Assert.True(Jobs.MakeAvailable(assemblyJobId, 0).IsSuccess);
        Assert.True(Jobs.Claim(assemblyJobId, WorkerId, 0).IsSuccess);
        Assert.True(Jobs.Start(assemblyJobId, 0).IsSuccess);

        Result placed = buildings.PlaceBoxPlan(
            BuildingId,
            sourceBoxId,
            assemblyJobId,
            definition,
            origin,
            BuildingOrientation.North,
            BuildingPlacementResult.Success(new[] { origin }, work),
            0);
        Assert.True(placed.IsSuccess, placed.Error?.ToString());
        Assert.True(Jobs.AdvanceStage(assemblyJobId, 1).IsSuccess);
        Assert.True(Jobs.AdvanceStage(assemblyJobId, 2).IsSuccess);
        Result atSite = buildings.MarkBoxAtSite(BuildingId, 2);
        Assert.True(atSite.IsSuccess, atSite.Error?.ToString());
        Assert.True(Jobs.AdvanceStage(assemblyJobId, 3).IsSuccess);
        Result started = buildings.StartConstruction(BuildingId);
        Assert.True(started.IsSuccess, started.Error?.ToString());
        Result progressed = buildings.AddConstructionWork(
            BuildingId,
            definition.RequiredWork,
            4);
        Assert.True(progressed.IsSuccess, progressed.Error?.ToString());
        Assert.True(Jobs.AdvanceStage(assemblyJobId, 5).IsSuccess);
        Result completed = buildings.CompleteBoxConstruction(BuildingId, 5);
        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        Assert.True(Jobs.Complete(assemblyJobId, 5).IsSuccess);
        buildings.DequeueUncommittedEvents();
        Jobs.DequeueUncommittedEvents();
        return buildings;
    }
}

}
