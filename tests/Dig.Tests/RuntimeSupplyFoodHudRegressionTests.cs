using System;
using System.IO;
using System.Linq;
using Dig.Application.Production;
using Dig.Domain.Agents;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class RuntimeSupplyFoodHudRegressionTests
{
    [Fact]
    public void Cancelling_acquired_supply_drops_units_and_allows_replacement_batch()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness();
        CellId sourceCell = new CellId(1, 1, 0);
        CellId recoveryCell = new CellId(4, 3, 0);
        EntityId sourceId = CampfireProductionTestHarness.Id(610);
        EntityId jobId = CampfireProductionTestHarness.Id(611);
        EntityId transitId = CampfireProductionTestHarness.Id(612);
        ItemId cap = CampfireProductionContent.MushroomCapItemId;
        Assert.True(harness.Inventory.AddUnit(
            sourceId,
            cap,
            ItemLocation.InWorld(sourceCell),
            tick: 0).IsSuccess);

        CreateBuildingSupplyJobHandler create = CreateSupplyHandler(harness);
        Assert.True(create.Handle(new CreateBuildingSupplyJobCommand(
            jobId,
            CampfireProductionTestHarness.BuildingId,
            CampfireProductionTestHarness.WorkerId,
            new[] { sourceCell },
            new[]
            {
                sourceCell,
                harness.Buildings.Get(
                    CampfireProductionTestHarness.BuildingId)!.WorkPosition,
            },
            new[] { transitId },
            new[] { CampfireProductionTestHarness.Id(613) },
            priority: 500,
            tick: 1)).IsSuccess);
        Assert.True(harness.Jobs.Start(jobId, tick: 2).IsSuccess);
        Assert.True(harness.Jobs.AdvanceStage(jobId, tick: 2).IsSuccess);
        Assert.True(new AcquireBuildingSupplySourceHandler(
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal).Handle(new AcquireBuildingSupplySourceCommand(
                jobId,
                sourceId,
                tick: 3)).IsSuccess);
        Assert.Contains(harness.Inventory.CreateSnapshot().Stacks, value =>
            value.Location.Kind == ItemLocationKind.AgentInventory
            && value.Location.OwnerId == CampfireProductionTestHarness.WorkerId
            && value.Reservations.Any(reservation => reservation.JobId == jobId));

        Result cancelled = new CancelBuildingSupplyHandler(
            harness.SupplyRepository,
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal).Handle(new CancelBuildingSupplyCommand(
                jobId,
                "direct_command_replaced",
                tick: 4,
                recoveryCell));

        Assert.True(cancelled.IsSuccess, cancelled.Error?.ToString());
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(jobId)!.Status);
        Assert.DoesNotContain(harness.Inventory.CreateSnapshot().Stacks, value =>
            value.Location.Kind == ItemLocationKind.AgentInventory
            && value.Location.OwnerId == CampfireProductionTestHarness.WorkerId
            && value.ItemId == cap);
        ItemStackSnapshot recovered = harness.Inventory.CreateSnapshot().Stacks.Single(value =>
            value.ItemId == cap
            && value.Location == ItemLocation.InWorld(recoveryCell));
        Assert.Equal(1, recovered.AvailableQuantity);
        Assert.Empty(recovered.Reservations);
        Assert.False(harness.Supply.Get(
            CampfireProductionTestHarness.BuildingId,
            harness.Inventory.CreateSnapshot())!.HasActiveSupply);

        EntityId replacementJob = CampfireProductionTestHarness.Id(614);
        Result replacement = create.Handle(new CreateBuildingSupplyJobCommand(
            replacementJob,
            CampfireProductionTestHarness.BuildingId,
            CampfireProductionTestHarness.WorkerId,
            new[] { recoveryCell },
            new[]
            {
                recoveryCell,
                harness.Buildings.Get(
                    CampfireProductionTestHarness.BuildingId)!.WorkPosition,
            },
            new[] { CampfireProductionTestHarness.Id(615) },
            new[] { CampfireProductionTestHarness.Id(616) },
            priority: 500,
            tick: 5));
        Assert.True(replacement.IsSuccess, replacement.Error?.ToString());
    }

    [Fact]
    public void Interrupting_carried_raw_material_drops_it_before_order_reset()
    {
        RecipeId recipeId = new RecipeId("recipe.interrupt_carried_raw");
        EntityId orderId = EntityId.Parse("8b000000000000000000000000000001");
        EntityId jobId = EntityId.Parse("8b000000000000000000000000000002");
        EntityId transitId = EntityId.Parse("8b000000000000000000000000000003");
        CellId recoveryCell = new CellId(5, 3, 0);
        RecipeDefinition recipe = new RecipeDefinition(
            recipeId,
            "Interrupt carried raw",
            ProductionTestHarness.Workshop,
            new[] { new ContentItemQuantity(ProductionTestHarness.Ore, 1) },
            new[] { new ContentItemQuantity(ProductionTestHarness.Plate, 1) },
            requiredWork: 1,
            energyPerWorkTick: 0,
            materialSteps: new[]
            {
                new RecipeMaterialStepDefinition(
                    ProductionTestHarness.Ore,
                    AgentSkillCatalog.Metallurgy,
                    baseDurationTicks: 3),
            });
        ProductionTestHarness harness = new ProductionTestHarness(new[] { recipe });
        Assert.True(harness.Enqueue(orderId, recipeId, tick: 1).IsSuccess);
        Assert.True(harness.Prepare(jobId, tick: 2).IsSuccess);
        harness.AssignAndBegin(orderId, jobId, tick: 3);
        Assert.True(new AcquireProductionMaterialHandler(
            harness.ProductionRepository,
            harness.InventoryRepository,
            harness.JobRepository,
            harness.Journal).Handle(new AcquireProductionMaterialCommand(
                orderId,
                jobId,
                transitId,
                tick: 4)).IsSuccess);

        Result interrupted = new InterruptProductionOrderHandler(
            harness.ProductionRepository,
            harness.InventoryRepository,
            harness.JobRepository,
            harness.Journal).Handle(new InterruptProductionOrderCommand(
                orderId,
                jobId,
                "direct_command_replaced",
                tick: 5,
                recoveryCell));

        Assert.True(interrupted.IsSuccess, interrupted.Error?.ToString());
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(jobId)!.Status);
        Assert.Equal(ProductionOrderStatus.Queued, harness.Production.Get(orderId)!.Status);
        Assert.DoesNotContain(harness.Inventory.CreateSnapshot().Stacks, value =>
            value.Location.Kind == ItemLocationKind.AgentInventory
            && value.Location.OwnerId == ProductionTestHarness.WorkerId
            && value.ItemId == ProductionTestHarness.Ore);
        ItemStackSnapshot recovered = harness.Inventory.CreateSnapshot().Stacks.Single(value =>
            value.ItemId == ProductionTestHarness.Ore
            && value.Location == ItemLocation.InWorld(recoveryCell));
        Assert.Equal(1, recovered.AvailableQuantity);
        Assert.Empty(recovered.Reservations);
    }

    [Fact]
    public void Unity_routing_recovers_supply_before_food_and_hides_skill_heading()
    {
        string root = FindRepositoryRoot();
        string direct = File.ReadAllText(Path.Combine(
            root,
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime",
            "DigTerrainWorkSession.DirectCommands.cs"));
        string food = File.ReadAllText(Path.Combine(
            root,
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime",
            "DigWorldInteraction.WorldFood.cs"));
        string skills = File.ReadAllText(Path.Combine(
            root,
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime",
            "DigGameHudCanvas.SkillInspector.cs"));

        Assert.Contains("BuildingSupplyJobDefinition", direct);
        Assert.Contains("CancelBuildingSupplyForDirectCommand", direct);
        Assert.Contains("ResolveResidentRecoveryCell", direct);
        Assert.Contains("eatAfterPickup: true", food);
        Assert.DoesNotContain("TOP 5 SKILLS", skills);
        Assert.Contains("foreach (ResidentSkillViewModel skill in skills.TopFive)", skills);
    }

    private static CreateBuildingSupplyJobHandler CreateSupplyHandler(
        CampfireProductionTestHarness harness)
    {
        return new CreateBuildingSupplyJobHandler(
            harness.Content,
            harness.SupplyRepository,
            harness.ProductionRepository,
            harness.BuildingsRepository,
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Dig.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}

}
