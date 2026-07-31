using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
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
using NUnit.Framework;

namespace Dig.Unity.Tests
{

internal sealed class CampfireFoodProductionPlayModeHarness
{
    internal static readonly EntityId BuildingId = Id(1);
    internal static readonly EntityId WorkerId = Id(2);

    internal CampfireFoodProductionPlayModeHarness()
    {
        List<ItemDefinition> definitions =
            CampfireProductionContent.CreateItems().ToList();
        definitions.Add(CampfireBuildingBoxContent.Definition.BoxItem);
        Items = new ItemCatalog(definitions);
        BuildingCatalog buildings = new BuildingCatalog(
            CampfireProductionContent.CreateBuildings());
        Content = ProductionContentCatalog.ValidateAndCreate(
            Items,
            buildings,
            CampfireProductionContent.CreateRecipes(baseDurationTicks: 1),
            Array.Empty<TechnologyDefinition>(),
            new[] { CampfireProductionContent.CreateWorkstation() }).Catalog!;
        Inventory = new InventoryState(Items);
        Jobs = new JobSystem();
        Buildings = CreateCompletedCampfire(buildings);
        Production = new ProductionState();
        Agents = new InMemoryAgentRepository();
        Require(Agents.Add(CreateWorker()));
        Journal = new InMemoryExecutionJournal();
        ProductionRepository = new InMemoryProductionRepository(Production);
        InventoryRepository = new InMemoryInventoryRepository(Inventory);
        BuildingsRepository = new InMemoryBuildingsRepository(Buildings);
        JobsRepository = new InMemoryJobRepository(Jobs);
        TechnologyRepository = new InMemoryTechnologyRepository(new TechnologyState());
        SkillGrants = new AgentSkillGrantService(Agents, Journal);
    }

    internal ItemCatalog Items { get; }
    internal ProductionContentCatalog Content { get; }
    internal InventoryState Inventory { get; }
    internal JobSystem Jobs { get; }
    internal BuildingsState Buildings { get; }
    internal ProductionState Production { get; }
    internal InMemoryAgentRepository Agents { get; }
    internal InMemoryExecutionJournal Journal { get; }
    internal InMemoryProductionRepository ProductionRepository { get; }
    internal InMemoryInventoryRepository InventoryRepository { get; }
    internal InMemoryBuildingsRepository BuildingsRepository { get; }
    internal InMemoryJobRepository JobsRepository { get; }
    internal InMemoryTechnologyRepository TechnologyRepository { get; }
    internal AgentSkillGrantService SkillGrants { get; }

    internal void AddBuildingStock(ItemId itemId, int quantity, int id)
    {
        Require(Inventory.AddStack(
            Id(id),
            itemId,
            quantity,
            ItemLocation.InBuilding(BuildingId),
            tick: 0));
    }

    internal Result Enqueue(EntityId orderId, long tick)
    {
        return new EnqueueProductionOrderHandler(Content, ProductionRepository).Handle(
            new EnqueueProductionOrderCommand(
                orderId,
                CampfireProductionContent.GrilledMushroomRecipeId,
                BuildingId,
                tick));
    }

    internal void ReadyOrder(EntityId orderId, EntityId jobId, long tick)
    {
        Require(Enqueue(orderId, tick));
        ReadyQueuedOrder(orderId, jobId, tick + 1);
    }

    internal void ReadyQueuedOrder(EntityId orderId, EntityId jobId, long tick)
    {
        Require(new PrepareProductionOrderHandler(
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
                tick)));
        Require(Jobs.Claim(jobId, WorkerId, tick + 1));
        Require(new BeginProductionWorkHandler(
            ProductionRepository,
            JobsRepository,
            Agents,
            Journal).Handle(new BeginProductionWorkCommand(
                orderId,
                jobId,
                tick + 2)));
        Require(Jobs.AdvanceStage(jobId, tick + 3));
        Require(new ApplyProductionWorkHandler(
            ProductionRepository,
            InventoryRepository,
            JobsRepository,
            Agents,
            Journal).Handle(new ApplyProductionWorkCommand(
                orderId,
                jobId,
                baseWork: 1,
                conditionEfficiencyBasisPoints: 10_000,
                tick + 4)));
        Assert.That(
            Production.Get(orderId)!.Status,
            Is.EqualTo(ProductionOrderStatus.ReadyToComplete));
        Assert.That(Jobs.Get(jobId)!.Stage, Is.EqualTo(JobStageKind.Finalize));
    }

    internal Result Complete(
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

    internal Result Complete(
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

    internal static EntityId Id(int value) =>
        EntityId.Parse(value.ToString("x32"));

    internal static void Require(Result result)
    {
        Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
    }

    private BuildingsState CreateCompletedCampfire(BuildingCatalog catalog)
    {
        BuildingDefinition definition = catalog.Get(
            CampfireBuildingBoxContent.CampfireBuildingId);
        CellId origin = new CellId(4, 4, 0);
        CellId work = new CellId(4, 3, 0);
        EntityId sourceBoxId = Id(900);
        EntityId assemblyJobId = Id(901);
        BuildingsState buildings = new BuildingsState();
        BuildingBoxAssemblyJobDefinition assembly = new BuildingBoxAssemblyJobDefinition(
            assemblyJobId,
            BuildingId,
            sourceBoxId,
            origin,
            work,
            priority: 500,
            createdTick: 0,
            retryPolicy: JobRetryPolicy.Default);
        Require(Jobs.Add(assembly));
        Require(Jobs.MakeAvailable(assemblyJobId, 0));
        Require(Jobs.Claim(assemblyJobId, WorkerId, 0));
        Require(Jobs.Start(assemblyJobId, 0));
        Require(buildings.PlaceBoxPlan(
            BuildingId,
            sourceBoxId,
            assemblyJobId,
            definition,
            origin,
            BuildingOrientation.North,
            BuildingPlacementResult.Success(new[] { origin }, work),
            0));
        Require(Jobs.AdvanceStage(assemblyJobId, 1));
        Require(Jobs.AdvanceStage(assemblyJobId, 2));
        Require(buildings.MarkBoxAtSite(BuildingId, 2));
        Require(Jobs.AdvanceStage(assemblyJobId, 3));
        Require(buildings.StartConstruction(BuildingId));
        Require(buildings.AddConstructionWork(
            BuildingId,
            definition.RequiredWork,
            4));
        Require(Jobs.AdvanceStage(assemblyJobId, 5));
        Require(buildings.CompleteBoxConstruction(BuildingId, 5));
        Require(Jobs.Complete(assemblyJobId, 5));
        buildings.DequeueUncommittedEvents();
        Jobs.DequeueUncommittedEvents();
        return buildings;
    }

    private static AgentState CreateWorker()
    {
        return new AgentState(
            WorkerId,
            "Cook",
            new AgentNeedsSnapshot(
                new NeedValue(8_000),
                new NeedValue(8_000),
                new NeedValue(8_000),
                new NeedValue(10_000)),
            new DailySchedule(
                ticksPerDay: 24,
                new[] { new ScheduleSegment(0, 24, ScheduleActivity.Work) }));
    }
}

}