using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class WorkshopProductionContentTests
{
    [Theory]
    [InlineData("recipe.wood_workshop.basket", "inventory.basket", "material.mushroom_leg", 1)]
    [InlineData("recipe.wood_workshop.ladder", "building_box.ladder", "material.mushroom_leg", 2)]
    [InlineData("recipe.stone_workshop.border_stone", "building_box.border_stone", "material.stone", 1)]
    public void Single_material_recipes_match_requested_costs(
        string recipeId,
        string outputId,
        string inputId,
        int quantity)
    {
        RecipeDefinition recipe = Recipe(recipeId);

        Assert.Equal(new ItemId(outputId), Assert.Single(recipe.Outputs).ItemId);
        ContentItemQuantity input = Assert.Single(recipe.Inputs);
        Assert.Equal(new ItemId(inputId), input.ItemId);
        Assert.Equal(quantity, input.Quantity);
    }

    [Theory]
    [InlineData("recipe.wood_workshop.wooden_door", "building_box.wooden_door", 2, 1, 1)]
    [InlineData("recipe.wood_workshop.farm", "building_box.farm", 5, 2, 0)]
    [InlineData("recipe.stone_workshop.press_trap", "building_box.press_trap", 1, 0, 4)]
    [InlineData("recipe.stone_workshop.club", "weapon.club", 1, 0, 1)]
    [InlineData("recipe.stone_workshop.slingshot", "weapon.slingshot", 1, 0, 1)]
    [InlineData("recipe.stone_workshop.stone_door", "building_box.stone_door", 1, 1, 2)]
    public void Multi_material_recipes_match_requested_costs(
        string recipeId,
        string outputId,
        int legs,
        int caps,
        int stones)
    {
        RecipeDefinition recipe = Recipe(recipeId);

        Assert.Equal(new ItemId(outputId), Assert.Single(recipe.Outputs).ItemId);
        AssertQuantity(recipe, CampfireProductionContent.MushroomLegItemId, legs);
        AssertQuantity(recipe, CampfireProductionContent.MushroomCapItemId, caps);
        AssertQuantity(recipe, CampfireProductionContent.StoneItemId, stones);
        Assert.Equal(legs + caps + stones, recipe.MaterialSteps.Count);
    }

    [Fact]
    public void Workshop_catalog_validates_and_stock_capacities_are_exact()
    {
        ItemCatalog items = Items();
        BuildingCatalog buildings = new BuildingCatalog(
            CampfireProductionContent.CreateBuildings());
        ContentValidationResult result = ProductionContentCatalog.ValidateAndCreate(
            items,
            buildings,
            CampfireProductionContent.CreateRecipes(1)
                .Concat(WorkshopProductionContent.CreateRecipes(1)),
            Array.Empty<TechnologyDefinition>(),
            new[] { CampfireProductionContent.CreateWorkstation() }
                .Concat(WorkshopProductionContent.CreateWorkstations()));

        Assert.True(result.Succeeded, string.Join("\n", result.Issues));
        AssertStock(
            result.Catalog!.GetWorkstation(
                CampfireProductionContent.WoodWorkshopBuildingId),
            legs: 5,
            caps: 2,
            stones: 2,
            recipeCount: 4);
        AssertStock(
            result.Catalog.GetWorkstation(
                CampfireProductionContent.StoneMasonBuildingId),
            legs: 2,
            caps: 2,
            stones: 5,
            recipeCount: 5);
    }

    [Fact]
    public void Constructed_outputs_are_packable_buildings_and_slingshot_is_a_weapon()
    {
        BuildingCatalog buildings = new BuildingCatalog(
            CampfireProductionContent.CreateBuildings());
        foreach (BuildingDefinitionId id in new[]
        {
            WorkshopProductionContent.WoodenDoorBuildingId,
            WorkshopProductionContent.LadderBuildingId,
            WorkshopProductionContent.FarmBuildingId,
            WorkshopProductionContent.BorderStoneBuildingId,
            WorkshopProductionContent.PressTrapBuildingId,
            WorkshopProductionContent.StoneDoorBuildingId,
        })
        {
            Assert.NotNull(buildings.Get(id).BoxPolicy);
        }

        Assert.Equal(
            CaveEncounterCombatContent.SlingshotProfileId,
            CaveEncounterCombatContent.FindResidentWeapon(
                WorkshopProductionContent.SlingshotItemId)!.ProfileId);
    }

    private static RecipeDefinition Recipe(string id) =>
        WorkshopProductionContent.CreateRecipes(1)
            .Single(value => value.Id == new RecipeId(id));

    private static void AssertQuantity(RecipeDefinition recipe, ItemId id, int expected)
    {
        int actual = recipe.Inputs
            .Where(value => value.ItemId == id)
            .Select(value => value.Quantity)
            .SingleOrDefault();
        Assert.Equal(expected, actual);
    }

    private static void AssertStock(
        ProductionWorkstationDefinition workstation,
        int legs,
        int caps,
        int stones,
        int recipeCount)
    {
        Assert.Equal(recipeCount, workstation.RecipeIds.Count);
        Assert.Equal(legs, workstation.GetStockRule(
            CampfireProductionContent.MushroomLegItemId).Capacity);
        Assert.Equal(caps, workstation.GetStockRule(
            CampfireProductionContent.MushroomCapItemId).Capacity);
        Assert.Equal(stones, workstation.GetStockRule(
            CampfireProductionContent.StoneItemId).Capacity);
        Assert.All(workstation.StockRules, value => Assert.True(value.DefaultDeliveryEnabled));
    }

    private static ItemCatalog Items()
    {
        List<ItemDefinition> items = CampfireProductionContent.CreateItems().ToList();
        items.Add(CampfireBuildingBoxContent.Definition.BoxItem);
        items.AddRange(new ResidentInventoryExpansionContent().Items);
        items.AddRange(CombatEquipmentContent.CreateItems());
        items.AddRange(WorkshopProductionContent.CreateItems());
        return new ItemCatalog(items.GroupBy(value => value.Id).Select(value => value.First()));
    }
}

}
