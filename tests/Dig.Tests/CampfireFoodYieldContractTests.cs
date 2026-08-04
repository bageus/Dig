using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Content;
using Dig.Domain.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class CampfireFoodYieldContractTests
{
    [Theory]
    [InlineData(
        "recipe.campfire.food.grilled_mushroom",
        "material.mushroom_cap",
        "food.grilled_mushroom")]
    [InlineData(
        "recipe.campfire.food.roasted_hamster",
        "creature.hamster",
        "food.roasted_hamster")]
    public void Food_order_consumes_one_matching_ingredient_and_produces_two_units(
        string recipeId,
        string inputItemId,
        string outputItemId)
    {
        RecipeDefinition recipe = CampfireProductionContent.CreateRecipes(
                CampfireProductionContent.ProductionMaterialTicks,
                CampfireProductionContent.CookingMaterialTicks)
            .Single(value => value.Id == new RecipeId(recipeId));

        ContentItemQuantity input = Assert.Single(recipe.Inputs);
        ContentItemQuantity output = Assert.Single(recipe.Outputs);
        RecipeMaterialStepDefinition step = Assert.Single(recipe.MaterialSteps);

        Assert.Equal(new ItemId(inputItemId), input.ItemId);
        Assert.Equal(1, input.Quantity);
        Assert.Equal(new ItemId(outputItemId), output.ItemId);
        Assert.Equal(2, output.Quantity);
        Assert.Equal(input.ItemId, step.ItemId);
        Assert.Equal(AgentSkillCatalog.Cooking, step.SkillId);
        Assert.Equal(
            CampfireProductionContent.CookingMaterialTicks,
            step.BaseDurationTicks);
        Assert.Equal(ProductionSkillGrantScale.PerOrder, recipe.SkillGrantScale);
    }
}

}
