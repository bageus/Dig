using System;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class ProductionContentCatalogTests
{
    private static readonly ItemId Ore = new ItemId("ore.copper");
    private static readonly ItemId Plate = new ItemId("plate.copper");
    private static readonly ItemId Tool = new ItemId("tool.hammer");
    private static readonly BuildingDefinitionId Workshop =
        new BuildingDefinitionId("workshop.smithy");
    private static readonly RecipeId PlateRecipe = new RecipeId("recipe.copper_plate");
    private static readonly TechnologyId Smithing = new TechnologyId("technology.smithing");

    [Fact]
    public void Valid_catalog_uses_stable_ids_and_resolves_references()
    {
        ItemCatalog items = CreateItems();
        BuildingCatalog buildings = CreateBuildings();
        RecipeDefinition recipe = CreateRecipe(requiredTechnology: Smithing);
        TechnologyDefinition technology = new TechnologyDefinition(
            Smithing,
            "Smithing",
            prerequisites: null,
            unlockedRecipes: new[] { PlateRecipe },
            researchItems: new[] { new ContentItemQuantity(Plate, 1) });

        ContentValidationResult result = ProductionContentCatalog.ValidateAndCreate(
            items,
            buildings,
            new[] { recipe },
            new[] { technology });

        Assert.True(result.Succeeded);
        Assert.Empty(result.Issues);
        Assert.Equal(12, result.Catalog!.GetRecipe(PlateRecipe).RequiredWork);
        Assert.Equal(Smithing, result.Catalog.GetTechnology(Smithing).Id);
    }

    [Fact]
    public void Missing_references_and_non_tool_are_reported_before_simulation()
    {
        ItemCatalog items = new ItemCatalog(new[]
        {
            new ItemDefinition(Ore, "Copper ore", 100, isTool: false),
            new ItemDefinition(Plate, "Copper plate", 100, isTool: false),
        });
        RecipeDefinition recipe = new RecipeDefinition(
            PlateRecipe,
            "Copper plate",
            new BuildingDefinitionId("missing.workstation"),
            new[] { new ContentItemQuantity(new ItemId("missing.input"), 1) },
            new[] { new ContentItemQuantity(Plate, 1) },
            requiredWork: 10,
            energyPerWorkTick: 0,
            requiredToolItemId: Ore,
            requiredTechnologyId: new TechnologyId("missing.technology"));

        ContentValidationResult result = ProductionContentCatalog.ValidateAndCreate(
            items,
            CreateBuildings(),
            new[] { recipe },
            Array.Empty<TechnologyDefinition>());

        Assert.False(result.Succeeded);
        Assert.Contains(result.Issues, issue => issue.Code == "content.missing_building");
        Assert.Contains(result.Issues, issue => issue.Code == "content.missing_item");
        Assert.Contains(result.Issues, issue => issue.Code == "content.item_not_tool");
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "content.missing_required_technology");
    }

    [Fact]
    public void Technology_cycles_are_rejected()
    {
        TechnologyId first = new TechnologyId("technology.first");
        TechnologyId second = new TechnologyId("technology.second");
        TechnologyDefinition firstDefinition = new TechnologyDefinition(
            first,
            "First",
            new[] { second },
            unlockedRecipes: null,
            new[] { new ContentItemQuantity(Plate, 1) });
        TechnologyDefinition secondDefinition = new TechnologyDefinition(
            second,
            "Second",
            new[] { first },
            unlockedRecipes: null,
            new[] { new ContentItemQuantity(Plate, 1) });

        ContentValidationResult result = ProductionContentCatalog.ValidateAndCreate(
            CreateItems(),
            CreateBuildings(),
            new[] { CreateRecipe(requiredTechnology: null) },
            new[] { firstDefinition, secondDefinition });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Issues, issue => issue.Code == "content.technology_cycle");
    }

    [Fact]
    public void Balance_changes_are_data_changes_not_runtime_branches()
    {
        RecipeDefinition fast = new RecipeDefinition(
            PlateRecipe,
            "Copper plate",
            Workshop,
            new[] { new ContentItemQuantity(Ore, 2) },
            new[] { new ContentItemQuantity(Plate, 1) },
            requiredWork: 5,
            energyPerWorkTick: 2);
        RecipeDefinition slow = new RecipeDefinition(
            PlateRecipe,
            "Copper plate",
            Workshop,
            new[] { new ContentItemQuantity(Ore, 3) },
            new[] { new ContentItemQuantity(Plate, 2) },
            requiredWork: 20,
            energyPerWorkTick: 7);

        Assert.Equal(5, fast.RequiredWork);
        Assert.Equal(20, slow.RequiredWork);
        Assert.Equal(2, fast.Inputs.Single().Quantity);
        Assert.Equal(3, slow.Inputs.Single().Quantity);
        Assert.Equal(1, fast.Outputs.Single().Quantity);
        Assert.Equal(2, slow.Outputs.Single().Quantity);
    }

    internal static ItemCatalog CreateItems()
    {
        return new ItemCatalog(new[]
        {
            new ItemDefinition(Ore, "Copper ore", 100, isTool: false),
            new ItemDefinition(Plate, "Copper plate", 100, isTool: false),
            new ItemDefinition(Tool, "Hammer", 1, isTool: true),
        });
    }

    internal static BuildingCatalog CreateBuildings()
    {
        return new BuildingCatalog(new[]
        {
            new BuildingDefinition(
                Workshop,
                "Smithy",
                new[] { new CellOffset(0, 0) },
                new[] { new CellOffset(0, 0) },
                new[] { new BuildingMaterialRequirement(Ore, 1) },
                requiredWork: 1,
                maximumDurability: 100),
        });
    }

    internal static RecipeDefinition CreateRecipe(TechnologyId? requiredTechnology)
    {
        return new RecipeDefinition(
            PlateRecipe,
            "Copper plate",
            Workshop,
            new[] { new ContentItemQuantity(Ore, 2) },
            new[] { new ContentItemQuantity(Plate, 1) },
            requiredWork: 12,
            energyPerWorkTick: 5,
            requiredToolItemId: Tool,
            requiredTechnologyId: requiredTechnology);
    }
}
}
