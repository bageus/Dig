using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class CampfireProductionContentTests
{
    [Fact]
    public void Campfire_workstation_is_fully_data_driven()
    {
        ItemCatalog items = CreateItems();
        BuildingCatalog buildings = new BuildingCatalog(
            CampfireProductionContent.CreateBuildings());
        ContentValidationResult result = ProductionContentCatalog.ValidateAndCreate(
            items,
            buildings,
            CampfireProductionContent.CreateRecipes(
                CampfireProductionContent.TestProductionMaterialTicks),
            Array.Empty<TechnologyDefinition>(),
            new[] { CampfireProductionContent.CreateWorkstation() });

        Assert.True(result.Succeeded, string.Join("\n", result.Issues));
        ProductionContentCatalog catalog = result.Catalog!;
        ProductionWorkstationDefinition workstation = catalog.GetWorkstation(
            CampfireBuildingBoxContent.CampfireBuildingId);
        Assert.Equal(CampfireProductionContent.AnimationProfileId, workstation.AnimationProfileId);
        Assert.Equal(6, workstation.RecipeIds.Count);
        Assert.Equal(new[] { 4, 4, 4, 2 }, workstation.StockRules
            .OrderByDescending(value => value.Priority)
            .Select(value => value.Capacity));

        InternalStockRuleDefinition hamster = workstation.StockRules.Single(value =>
            value.ItemId == CampfireProductionContent.HamsterItemId);
        Assert.Equal(2, hamster.Capacity);
        Assert.False(hamster.DefaultDeliveryEnabled);
        Assert.All(
            workstation.StockRules.Where(value =>
                value.ItemId != CampfireProductionContent.HamsterItemId),
            value => Assert.True(value.DefaultDeliveryEnabled));
    }

    [Theory]
    [InlineData("recipe.campfire.building.tent", "building_box.tent", 1, 3)]
    [InlineData("recipe.campfire.building.stone_mason", "building_box.stone_mason", 1, 6)]
    [InlineData("recipe.campfire.building.wood_workshop", "building_box.wood_workshop", 1, 5)]
    [InlineData("recipe.campfire.building.campfire", "building_box.campfire", 1, 4)]
    [InlineData("recipe.campfire.food.grilled_mushroom", "food.grilled_mushroom", 2, 1)]
    [InlineData("recipe.campfire.food.roasted_hamster", "food.roasted_hamster", 2, 1)]
    public void Campfire_recipe_outputs_and_steps_match_content(
        string recipeId,
        string outputItemId,
        int outputQuantity,
        int stepCount)
    {
        RecipeDefinition recipe = CampfireProductionContent.CreateRecipes(1)
            .Single(value => value.Id == new RecipeId(recipeId));

        Assert.Equal(new ItemId(outputItemId), Assert.Single(recipe.Outputs).ItemId);
        Assert.Equal(outputQuantity, Assert.Single(recipe.Outputs).Quantity);
        Assert.Equal(stepCount, recipe.MaterialSteps.Count);
        Assert.Equal(ProductionSkillGrantScale.PerOrder, recipe.SkillGrantScale);
    }

    [Fact]
    public void Grilled_mushroom_consumes_one_cap_creates_two_food_units()
    {
        RecipeDefinition recipe = CampfireProductionContent.CreateRecipes(
                CampfireProductionContent.ProductionMaterialTicks)
            .Single(value => value.Id == CampfireProductionContent.GrilledMushroomRecipeId);
        ContentItemQuantity input = Assert.Single(recipe.Inputs);
        ContentItemQuantity output = Assert.Single(recipe.Outputs);
        ItemDefinition item = CreateItems().Get(output.ItemId);

        Assert.Equal(CampfireProductionContent.MushroomCapItemId, input.ItemId);
        Assert.Equal(1, input.Quantity);
        Assert.Equal(CampfireProductionContent.GrilledMushroomItemId, output.ItemId);
        Assert.Equal(2, output.Quantity);
        Assert.Contains(CampfireProductionContent.FoodCategoryId, item.Categories);
    }

    [Fact]
    public void Material_skills_and_order_grants_match_balance()
    {
        IReadOnlyList<RecipeDefinition> recipes =
            CampfireProductionContent.CreateRecipes(100);
        RecipeDefinition stoneMason = recipes.Single(value =>
            value.Id == CampfireProductionContent.StoneMasonRecipeId);
        Assert.Equal(2, stoneMason.MaterialSteps.Count(value =>
            value.SkillId == AgentSkillCatalog.Woodworking));
        Assert.Equal(4, stoneMason.MaterialSteps.Count(value =>
            value.SkillId == AgentSkillCatalog.Stonework));
        Assert.Equal(100, stoneMason.SkillGrantProfile!.PerUnit.Single(value =>
            value.SkillId == AgentSkillCatalog.Woodworking).RequestedUnits);
        Assert.Equal(300, stoneMason.SkillGrantProfile.PerUnit.Single(value =>
            value.SkillId == AgentSkillCatalog.Stonework).RequestedUnits);

        RecipeDefinition grilled = recipes.Single(value =>
            value.Id == CampfireProductionContent.GrilledMushroomRecipeId);
        Assert.Equal(AgentSkillCatalog.Cooking, Assert.Single(grilled.MaterialSteps).SkillId);
        Assert.Equal(120, Assert.Single(grilled.SkillGrantProfile!.PerUnit).RequestedUnits);
    }

    [Theory]
    [InlineData(0, 900)]
    [InlineData(25, 675)]
    [InlineData(50, 450)]
    [InlineData(100, 1)]
    public void Grilled_mushroom_duration_is_fifteen_minutes_minus_cooking_percent(
        int skillPoints,
        long expectedTicks)
    {
        Assert.Equal(
            expectedTicks,
            ProductionStepTiming.ResolveDurationTicks(
                CampfireProductionContent.ProductionMaterialTicks,
                skillPoints * AgentSkillCatalog.UnitsPerPoint));
    }

    internal static ItemCatalog CreateItems()
    {
        List<ItemDefinition> values = CampfireProductionContent.CreateItems().ToList();
        values.Add(CampfireBuildingBoxContent.Definition.BoxItem);
        return new ItemCatalog(values);
    }
}
