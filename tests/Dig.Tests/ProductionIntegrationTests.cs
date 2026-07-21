using System.Linq;
using Dig.Application.Production;
using Dig.Domain.Agents;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Dig.Domain.Technology;
using Xunit;

namespace Dig.Tests
{

public sealed class ProductionIntegrationTests
{
    private static readonly RecipeId PlateRecipe =
        new RecipeId("recipe.copper_plate");
    private static readonly TechnologyId Smithing =
        new TechnologyId("technology.smithing");
    private static readonly EntityId FirstOrderId =
        EntityId.Parse("84000000000000000000000000000001");
    private static readonly EntityId SecondOrderId =
        EntityId.Parse("84000000000000000000000000000002");
    private static readonly EntityId FirstJobId =
        EntityId.Parse("85000000000000000000000000000001");
    private static readonly EntityId SecondJobId =
        EntityId.Parse("85000000000000000000000000000002");
    private static readonly EntityId OutputStackId =
        EntityId.Parse("86000000000000000000000000000001");

    [Fact]
    public void Order_consumes_inputs_and_creates_output_exactly_once()
    {
        ProductionTestHarness harness = CreateHarness();
        Assert.True(harness.Enqueue(FirstOrderId, PlateRecipe, tick: 1).IsSuccess);
        Assert.True(harness.Prepare(FirstJobId, tick: 2).IsSuccess);
        harness.AssignAndBegin(FirstOrderId, FirstJobId, tick: 3);
        Assert.True(harness.ApplyWork(FirstOrderId, FirstJobId, tick: 6).IsSuccess);
        ProductionWorkApplied work = Assert.Single(
            harness.Journal.Events.OfType<ProductionWorkApplied>());
        Assert.Equal(ProductionTestHarness.BuildingId, work.BuildingId);
        Assert.Equal(work.RequiredWork, work.CompletedWork);
        Assert.Equal(ProductionOrderStatus.ReadyToComplete, harness.Production.Get(FirstOrderId)!.Status);
        Assert.Equal(JobStageKind.Finalize, harness.Jobs.Get(FirstJobId)!.Stage);

        Assert.True(harness.Complete(
            FirstOrderId,
            FirstJobId,
            OutputStackId,
            tick: 7).IsSuccess);

        Assert.Equal(8, harness.Inventory.GetTotal(ProductionTestHarness.Ore));
        Assert.Equal(1, harness.Inventory.GetTotal(ProductionTestHarness.Plate));
        Assert.Equal(ProductionOrderStatus.Completed, harness.Production.Get(FirstOrderId)!.Status);
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(FirstJobId)!.Status);
        Assert.Empty(harness.Jobs.GetReservations());
        Assert.Equal(0, harness.Inventory.GetStack(ProductionTestHarness.OreStackId)!.ReservedQuantity);

        Assert.True(harness.Complete(
            FirstOrderId,
            FirstJobId,
            EntityId.Parse("86000000000000000000000000000002"),
            tick: 8).IsFailure);
        Assert.Equal(1, harness.Inventory.GetTotal(ProductionTestHarness.Plate));
    }

    [Fact]
    public void Output_validation_fails_before_reserved_inputs_are_consumed()
    {
        ProductionTestHarness harness = CreateHarness();
        Assert.True(harness.Enqueue(FirstOrderId, PlateRecipe, tick: 1).IsSuccess);
        Assert.True(harness.Prepare(FirstJobId, tick: 2).IsSuccess);
        harness.AssignAndBegin(FirstOrderId, FirstJobId, tick: 3);
        Assert.True(harness.ApplyWork(FirstOrderId, FirstJobId, tick: 6).IsSuccess);

        Result invalid = harness.Complete(
            FirstOrderId,
            FirstJobId,
            ProductionTestHarness.OreStackId,
            tick: 7);

        Assert.Equal(InventoryErrors.StackAlreadyExists, invalid.Error);
        Assert.Equal(10, harness.Inventory.GetTotal(ProductionTestHarness.Ore));
        Assert.Equal(2, harness.Inventory.GetStack(ProductionTestHarness.OreStackId)!.ReservedQuantity);
        Assert.Equal(ProductionOrderStatus.ReadyToComplete, harness.Production.Get(FirstOrderId)!.Status);
        Assert.True(harness.Complete(
            FirstOrderId,
            FirstJobId,
            OutputStackId,
            tick: 8).IsSuccess);
    }

    [Fact]
    public void Workstation_queue_serializes_orders_and_cancellation_releases_inputs()
    {
        ProductionTestHarness harness = CreateHarness();
        Assert.True(harness.Enqueue(FirstOrderId, PlateRecipe, tick: 1).IsSuccess);
        Assert.True(harness.Enqueue(SecondOrderId, PlateRecipe, tick: 1).IsSuccess);
        Assert.True(harness.Prepare(FirstJobId, tick: 2).IsSuccess);

        Result blocked = harness.Prepare(SecondJobId, tick: 3);

        Assert.Equal(ProductionErrors.QueueBlocked, blocked.Error);
        Assert.Equal(2, harness.Inventory.GetStack(ProductionTestHarness.OreStackId)!.ReservedQuantity);
        CancelProductionOrderHandler cancel = new CancelProductionOrderHandler(
            harness.ProductionRepository,
            harness.InventoryRepository,
            harness.JobRepository,
            harness.Journal);
        Assert.True(cancel.Handle(new CancelProductionOrderCommand(
            FirstOrderId,
            FirstJobId,
            "Priority changed.",
            tick: 4)).IsSuccess);
        Assert.Equal(0, harness.Inventory.GetStack(ProductionTestHarness.OreStackId)!.ReservedQuantity);
        Assert.True(harness.Prepare(
            EntityId.Parse("85000000000000000000000000000003"),
            tick: 5).IsSuccess);
    }

    [Fact]
    public void Tool_energy_and_technology_are_explicit_gates()
    {
        RecipeDefinition unlockedRecipe =
            ProductionContentCatalogTests.CreateRecipe(requiredTechnology: null);
        ProductionTestHarness noTool = new ProductionTestHarness(
            new[] { unlockedRecipe },
            includeTool: false);
        Assert.True(noTool.Enqueue(FirstOrderId, PlateRecipe, tick: 1).IsSuccess);
        Assert.Equal(ProductionErrors.ToolUnavailable, noTool.Prepare(FirstJobId, tick: 2).Error);

        ProductionTestHarness noEnergy = new ProductionTestHarness(
            new[] { unlockedRecipe },
            energyAvailable: false);
        Assert.True(noEnergy.Enqueue(FirstOrderId, PlateRecipe, tick: 1).IsSuccess);
        Assert.Equal(ProductionErrors.EnergyUnavailable, noEnergy.Prepare(FirstJobId, tick: 2).Error);

        RecipeDefinition lockedRecipe =
            ProductionContentCatalogTests.CreateRecipe(requiredTechnology: Smithing);
        TechnologyDefinition technology = new TechnologyDefinition(
            Smithing,
            "Smithing",
            prerequisites: null,
            unlockedRecipes: new[] { PlateRecipe },
            researchItems: new[]
            {
                new ContentItemQuantity(ProductionTestHarness.Plate, 1),
            });
        ProductionTestHarness locked = new ProductionTestHarness(
            new[] { lockedRecipe },
            new[] { technology });
        Assert.True(locked.Enqueue(FirstOrderId, PlateRecipe, tick: 1).IsSuccess);
        Assert.Equal(ProductionErrors.TechnologyLocked, locked.Prepare(FirstJobId, tick: 2).Error);
    }

    [Fact]
    public void Produced_item_can_unlock_technology_and_enable_next_recipe()
    {
        RecipeId advancedRecipeId = new RecipeId("recipe.precision_plate");
        RecipeDefinition baseRecipe =
            ProductionContentCatalogTests.CreateRecipe(requiredTechnology: null);
        RecipeDefinition advancedRecipe = new RecipeDefinition(
            advancedRecipeId,
            "Precision plate",
            ProductionTestHarness.Workshop,
            new[] { new ContentItemQuantity(ProductionTestHarness.Ore, 1) },
            new[] { new ContentItemQuantity(ProductionTestHarness.Plate, 2) },
            requiredWork: 6,
            energyPerWorkTick: 5,
            requiredToolItemId: ProductionTestHarness.Tool,
            requiredTechnologyId: Smithing);
        TechnologyDefinition technology = new TechnologyDefinition(
            Smithing,
            "Smithing",
            prerequisites: null,
            unlockedRecipes: new[] { advancedRecipeId },
            researchItems: new[]
            {
                new ContentItemQuantity(ProductionTestHarness.Plate, 1),
            });
        ProductionTestHarness harness = new ProductionTestHarness(
            new[] { baseRecipe, advancedRecipe },
            new[] { technology });
        Assert.True(harness.Enqueue(FirstOrderId, PlateRecipe, tick: 1).IsSuccess);
        Assert.True(harness.Prepare(FirstJobId, tick: 2).IsSuccess);
        harness.AssignAndBegin(FirstOrderId, FirstJobId, tick: 3);
        Assert.True(harness.ApplyWork(FirstOrderId, FirstJobId, tick: 6).IsSuccess);
        Assert.True(harness.Complete(FirstOrderId, FirstJobId, OutputStackId, tick: 7).IsSuccess);
        UnlockTechnologyHandler unlock = new UnlockTechnologyHandler(
            harness.Content,
            harness.TechnologyRepository,
            harness.InventoryRepository,
            harness.Journal);

        Assert.True(unlock.Handle(new UnlockTechnologyCommand(
            Smithing,
            ItemLocation.InBuilding(ProductionTestHarness.BuildingId),
            tick: 8)).IsSuccess);

        Assert.True(harness.Technology.IsUnlocked(Smithing));
        Assert.Equal(0, harness.Inventory.GetTotal(ProductionTestHarness.Plate));
        Assert.True(harness.Enqueue(SecondOrderId, advancedRecipeId, tick: 9).IsSuccess);
        Assert.True(harness.Prepare(SecondJobId, tick: 10).IsSuccess);
    }

    [Fact]
    public void Committed_output_quantity_multiplies_data_driven_skill_grant_once()
    {
        RecipeDefinition recipe = new RecipeDefinition(
            PlateRecipe,
            "Skilled plate",
            ProductionTestHarness.Workshop,
            new[] { new ContentItemQuantity(ProductionTestHarness.Ore, 2) },
            new[] { new ContentItemQuantity(ProductionTestHarness.Plate, 2) },
            requiredWork: 6,
            energyPerWorkTick: 5,
            requiredToolItemId: ProductionTestHarness.Tool,
            requiredTechnologyId: null,
            skillGrantProfile: SkillGrantProfile.Single(
                AgentSkillCatalog.Metallurgy,
                units: 40));
        ProductionTestHarness harness = new ProductionTestHarness(new[] { recipe });
        Assert.True(harness.Enqueue(FirstOrderId, PlateRecipe, tick: 1).IsSuccess);
        Assert.True(harness.Prepare(FirstJobId, tick: 2).IsSuccess);
        harness.AssignAndBegin(FirstOrderId, FirstJobId, tick: 3);
        Assert.True(harness.ApplyWork(FirstOrderId, FirstJobId, tick: 6).IsSuccess);

        Assert.True(harness.Complete(
            FirstOrderId,
            FirstJobId,
            OutputStackId,
            tick: 7).IsSuccess);

        Assert.Equal(80, harness.Agents.Get(ProductionTestHarness.WorkerId)!
            .CreateSnapshot(7)
            .GetSkillLevel(AgentSkillCatalog.Metallurgy));
        Assert.True(harness.Complete(
            FirstOrderId,
            FirstJobId,
            EntityId.Parse("86000000000000000000000000000009"),
            tick: 8).IsFailure);
        Assert.Equal(80, harness.Agents.Get(ProductionTestHarness.WorkerId)!
            .CreateSnapshot(8)
            .GetSkillLevel(AgentSkillCatalog.Metallurgy));
    }

    [Fact]
    public void Cooking_skill_changes_work_duration_but_not_recipe_output_or_effect()
    {
        RecipeDefinition recipe = new RecipeDefinition(
            PlateRecipe,
            "Cooked test output",
            ProductionTestHarness.Workshop,
            new[] { new ContentItemQuantity(ProductionTestHarness.Ore, 1) },
            new[] { new ContentItemQuantity(ProductionTestHarness.Plate, 2) },
            requiredWork: 100,
            energyPerWorkTick: 1,
            skillGrantProfile: SkillGrantProfile.Single(
                AgentSkillCatalog.Cooking,
                units: 10),
            workSpeedCurve: new SkillWorkSpeedCurve(
                AgentSkillCatalog.Cooking,
                minimumEfficiencyBasisPoints: 5_000,
                maximumEfficiencyBasisPoints: 15_000));
        var novice = AgentTestFactory.CreateAgent();
        var expert = AgentTestFactory.CreateAgent(
            id: EntityId.Parse("83000000000000000000000000000009"));
        Assert.True(novice.SetSkillLevel(new AgentSkillId("general.work"), 0).IsSuccess);
        Assert.True(expert.ApplySkillGrant(new SkillGrantBundle(
            expert.Id,
            SkillGrantSourceKind.TrainingCompleted,
            "cooking-training",
            tick: 1,
            new[] { new SkillGrant(AgentSkillCatalog.Cooking, 10_000) })).IsSuccess);

        int noviceWork = ProductionEfficiency.CalculateEffectiveWork(
            10,
            ProductionWorkContext.ForRecipe(recipe, novice.CreateSnapshot(1)));
        int expertWork = ProductionEfficiency.CalculateEffectiveWork(
            10,
            ProductionWorkContext.ForRecipe(recipe, expert.CreateSnapshot(1)));

        Assert.True(expertWork > noviceWork);
        Assert.Equal(2, recipe.Outputs.Single().Quantity);
        Assert.Equal(ProductionTestHarness.Plate, recipe.Outputs.Single().ItemId);
    }

    private static ProductionTestHarness CreateHarness()
    {
        return new ProductionTestHarness(new[]
        {
            ProductionContentCatalogTests.CreateRecipe(requiredTechnology: null),
        });
    }
}
}
