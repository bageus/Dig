using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Inventory;
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

public sealed class CampfireFoodCompletionPlayModeTests
{
    [Test]
    public void Full_output_ring_defers_then_retry_commits_exactly_once()
    {
        Harness harness = new Harness();
        EntityId orderId = Id(100);
        EntityId jobId = Id(101);
        EntityId outputId = Id(102);
        harness.AddBuildingStock(
            CampfireProductionContent.MushroomCapItemId,
            quantity: 1,
            id: 103);
        harness.ReadyOrder(orderId, jobId, tick: 1);
        MaterialId air = new MaterialId("terrain.air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(air, isSolid: false, hardness: 0),
        });
        WorldState world = WorldState.CreateFilled(
            new WorldSize(12, 12),
            chunkSize: 4,
            materials,
            air,
            explored: true).Value;
        BuildingSnapshot building = harness.Buildings.Get(Harness.BuildingId)!;
        CellId[] candidates = ProductionOutputPlacement
            .CreateCandidates(building, maximumLateralDistance: 0)
            .ToArray();
        for (int index = 0; index < candidates.Length; index++)
        {
            AssertSuccess(harness.Inventory.AddStack(
                Id(200 + index),
                CampfireProductionContent.StoneItemId,
                1,
                ItemLocation.InWorld(candidates[index]),
                tick: 6));
        }

        Result<CellId> blocked = ProductionOutputPlacement.Resolve(
            building,
            world.CreateSnapshot(),
            building.Footprint,
            harness.Inventory.CreateSnapshot().Stacks,
            maximumLateralDistance: 0);

        Assert.That(blocked.IsFailure, Is.True);
        Assert.That(blocked.Error, Is.EqualTo(ProductionErrors.OutputSpaceUnavailable));
        Assert.That(
            harness.Production.Get(orderId)!.Status,
            Is.EqualTo(ProductionOrderStatus.ReadyToComplete));
        Assert.That(
            harness.Inventory.GetTotal(CampfireProductionContent.GrilledMushroomItemId),
            Is.EqualTo(0));

        EntityId firstBlocker = Id(200);
        EntityId removalOwner = Id(299);
        AssertSuccess(harness.Inventory.ReserveQuantity(
            firstBlocker,
            removalOwner,
            1,
            tick: 7));
        AssertSuccess(harness.Inventory.ConsumeReserved(
            removalOwner,
            firstBlocker,
            1,
            tick: 7));
        Result<CellId> available = ProductionOutputPlacement.Resolve(
            building,
            world.CreateSnapshot(),
            building.Footprint,
            harness.Inventory.CreateSnapshot().Stacks,
            maximumLateralDistance: 0);
        Assert.That(available.IsSuccess, Is.True, available.Error?.ToString());
        Assert.That(available.Value, Is.EqualTo(candidates[0]));

        AssertSuccess(harness.Complete(
            orderId,
            jobId,
            outputId,
            available.Value,
            tick: 8));
        Result duplicate = harness.Complete(
            orderId,
            jobId,
            Id(300),
            available.Value,
            tick: 9);

        Assert.That(duplicate.IsFailure, Is.True);
        Assert.That(
            harness.Inventory.GetStack(outputId)!.Quantity,
            Is.EqualTo(2));
        Assert.That(
            harness.Inventory.GetTotal(CampfireProductionContent.GrilledMushroomItemId),
            Is.EqualTo(2));
        Assert.That(
            harness.Production.Get(orderId)!.Status,
            Is.EqualTo(ProductionOrderStatus.Completed));
    }

    [Test]
    public void Repeated_orders_complete_and_cancelled_use_pickup_keeps_food()
    {
        Harness harness = new Harness();
        harness.AddBuildingStock(
            CampfireProductionContent.MushroomCapItemId,
            quantity: 2,
            id: 400);
        EntityId firstOrder = Id(401);
        EntityId secondOrder = Id(402);
        AssertSuccess(harness.Enqueue(firstOrder, tick: 1));
        AssertSuccess(harness.Enqueue(secondOrder, tick: 2));
        harness.ReadyQueuedOrder(firstOrder, Id(403), tick: 3);
        AssertSuccess(harness.Complete(
            firstOrder,
            Id(403),
            Id(404),
            new CellId(4, 2, 0),
            tick: 10));
        harness.ReadyQueuedOrder(secondOrder, Id(405), tick: 11);
        AssertSuccess(harness.Complete(
            secondOrder,
            Id(405),
            Id(406),
            new CellId(3, 3, 0),
            tick: 18));

        Assert.That(
            harness.Inventory.GetTotal(CampfireProductionContent.GrilledMushroomItemId),
            Is.EqualTo(4));
        Assert.That(
            harness.Production.Get(firstOrder)!.Status,
            Is.EqualTo(ProductionOrderStatus.Completed));
        Assert.That(
            harness.Production.Get(secondOrder)!.Status,
            Is.EqualTo(ProductionOrderStatus.Completed));

        EntityId foodId = Id(407);
        EntityId pickupJobId = Id(408);
        CellId foodCell = new CellId(1, 1, 0);
        AssertSuccess(harness.Inventory.AddStack(
            foodId,
            CampfireProductionContent.GrilledMushroomItemId,
            1,
            ItemLocation.InWorld(foodCell),
            tick: 19));
        CreateWorldItemPickupHandler create = new CreateWorldItemPickupHandler(
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal);
        AssertSuccess(create.Handle(new CreateWorldItemPickupCommand(
            pickupJobId,
            foodId,
            Harness.WorkerId,
            foodCell,
            priority: 700,
            tick: 20,
            completionAction: WorldItemPickupCompletionAction.UseConsumable)));
        Assert.That(
            ((WorldItemPickupJobDefinition)harness.Jobs.Get(pickupJobId)!.Definition)
                .CompletionAction,
            Is.EqualTo(WorldItemPickupCompletionAction.UseConsumable));

        AssertSuccess(new CancelWorldItemPickupHandler(
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal).Handle(new CancelWorldItemPickupCommand(
                pickupJobId,
                "player_cancelled",
                tick: 21)));

        Assert.That(harness.Jobs.Get(pickupJobId)!.Status, Is.EqualTo(JobStatus.Cancelled));
        Assert.That(harness.Inventory.GetStack(foodId)!.AvailableQuantity, Is.EqualTo(1));
        Assert.That(harness.Inventory.GetResidentSlotClaims(pickupJobId), Is.Empty);
    }

    private sealed class Harness
    {
        internal static readonly EntityId BuildingId = Id(1);
        internal static readonly EntityId WorkerId = Id(2);

        internal Harness()
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
                CampfireProductionContent.CreateRecipes(materialStepTicks: 1),
                Array.Empty<TechnologyDefinition>(),
                new[] { CampfireProductionContent.CreateWorkstation() }).Catalog!;
            Inventory = new InventoryState(Items);
            Jobs = new JobSystem();
            Buildings = CreateCompletedCampfire(buildings);
            Production = new ProductionState();
            Agents = new InMemoryAgentRepository();
            AssertSuccess(Agents.Add(CreateWorker()));
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
            AssertSuccess(Inventory.AddStack(
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
            AssertSuccess(Enqueue(orderId, tick));
            ReadyQueuedOrder(orderId, jobId, tick + 1);
        }

        internal void ReadyQueuedOrder(EntityId orderId, EntityId jobId, long tick)
        {
            AssertSuccess(new PrepareProductionOrderHandler(
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
            AssertSuccess(Jobs.Claim(jobId, WorkerId, tick + 1));
            AssertSuccess(new BeginProductionWorkHandler(
                ProductionRepository,
                JobsRepository,
                Agents,
                Journal).Handle(new BeginProductionWorkCommand(
                    orderId,
                    jobId,
                    tick + 2)));
            AssertSuccess(Jobs.AdvanceStage(jobId, tick + 3));
            AssertSuccess(new ApplyProductionWorkHandler(
                ProductionRepository,
                InventoryRepository,
                JobsRepository,
                Agents,
                Journal).Handle(new ApplyProductionWorkCommand(
                    orderId,
                    jobId,
                    elapsedTicks: 1,
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
            return new CompleteProductionOrderHandler(
                ProductionRepository,
                InventoryRepository,
                JobsRepository,
                Journal,
                SkillGrants).Handle(new CompleteProductionOrderCommand(
                    orderId,
                    jobId,
                    new[] { outputId },
                    tick,
                    ItemLocation.InWorld(outputCell)));
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
            BuildingBoxAssemblyJobDefinition assembly =
                new BuildingBoxAssemblyJobDefinition(
                    assemblyJobId,
                    BuildingId,
                    sourceBoxId,
                    origin,
                    work,
                    priority: 500,
                    createdTick: 0,
                    retryPolicy: JobRetryPolicy.Default);
            AssertSuccess(Jobs.Add(assembly));
            AssertSuccess(Jobs.MakeAvailable(assemblyJobId, 0));
            AssertSuccess(Jobs.Claim(assemblyJobId, WorkerId, 0));
            AssertSuccess(Jobs.Start(assemblyJobId, 0));
            AssertSuccess(buildings.PlaceBoxPlan(
                BuildingId,
                sourceBoxId,
                assemblyJobId,
                definition,
                origin,
                BuildingOrientation.North,
                BuildingPlacementResult.Success(new[] { origin }, work),
                0));
            AssertSuccess(Jobs.AdvanceStage(assemblyJobId, 1));
            AssertSuccess(Jobs.AdvanceStage(assemblyJobId, 2));
            AssertSuccess(buildings.MarkBoxAtSite(BuildingId, 2));
            AssertSuccess(Jobs.AdvanceStage(assemblyJobId, 3));
            AssertSuccess(buildings.StartConstruction(BuildingId));
            AssertSuccess(buildings.AddConstructionWork(
                BuildingId,
                definition.RequiredWork,
                4));
            AssertSuccess(Jobs.AdvanceStage(assemblyJobId, 5));
            AssertSuccess(buildings.CompleteBoxConstruction(BuildingId, 5));
            AssertSuccess(Jobs.Complete(assemblyJobId, 5));
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

    private static void AssertSuccess(Result result)
    {
        Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
    }

    private static EntityId Id(int value) =>
        EntityId.Parse(value.ToString("x32"));
}

}
