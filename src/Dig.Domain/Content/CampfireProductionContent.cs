using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Inventory;

namespace Dig.Domain.Content
{

public static class CampfireProductionContent
{
    public const long ProductionMaterialTicks = 15 * 60;
    public const long TestProductionMaterialTicks = 1;
    public const string AnimationProfileId = "production.animation.campfire";

    public static readonly ItemCategoryId FoodCategoryId = new ItemCategoryId("food");
    public static readonly BuildingDefinitionId TentBuildingId =
        new BuildingDefinitionId("building.tent");
    public static readonly BuildingDefinitionId StoneMasonBuildingId =
        new BuildingDefinitionId("building.stone_mason");
    public static readonly BuildingDefinitionId WoodWorkshopBuildingId =
        new BuildingDefinitionId("building.wood_workshop");

    public static readonly ItemId TentBoxItemId = new ItemId("building_box.tent");
    public static readonly ItemId StoneMasonBoxItemId =
        new ItemId("building_box.stone_mason");
    public static readonly ItemId WoodWorkshopBoxItemId =
        new ItemId("building_box.wood_workshop");
    public static readonly ItemId MushroomCapItemId =
        new ItemId("material.mushroom_cap");
    public static readonly ItemId MushroomLegItemId =
        new ItemId("material.mushroom_leg");
    public static readonly ItemId StoneItemId = new ItemId("material.stone");
    public static readonly ItemId HamsterItemId = new ItemId("creature.hamster");
    public static readonly ItemId GrilledMushroomItemId =
        new ItemId("food.grilled_mushroom");
    public static readonly ItemId RoastedHamsterItemId =
        new ItemId("food.roasted_hamster");

    public static readonly RecipeId TentRecipeId =
        new RecipeId("recipe.campfire.building.tent");
    public static readonly RecipeId StoneMasonRecipeId =
        new RecipeId("recipe.campfire.building.stone_mason");
    public static readonly RecipeId WoodWorkshopRecipeId =
        new RecipeId("recipe.campfire.building.wood_workshop");
    public static readonly RecipeId CampfireRecipeId =
        new RecipeId("recipe.campfire.building.campfire");
    public static readonly RecipeId GrilledMushroomRecipeId =
        new RecipeId("recipe.campfire.food.grilled_mushroom");
    public static readonly RecipeId RoastedHamsterRecipeId =
        new RecipeId("recipe.campfire.food.roasted_hamster");

    public static IReadOnlyList<ItemDefinition> CreateItems()
    {
        ItemCategoryId box = CampfireBuildingBoxContent.BuildingBoxCategoryId;
        return ProductionPackageContent.CreateItems().Concat(new[]
        {
            new ItemDefinition(MushroomCapItemId, "Mushroom cap", 100, false),
            new ItemDefinition(MushroomLegItemId, "Mushroom leg", 100, false),
            new ItemDefinition(StoneItemId, "Stone", 100, false),
            new ItemDefinition(HamsterItemId, "Hamster", 1, false),
            new ItemDefinition(
                GrilledMushroomItemId,
                "Grilled mushroom",
                100,
                false,
                new[] { FoodCategoryId }),
            new ItemDefinition(
                RoastedHamsterItemId,
                "Grilled hamster",
                100,
                false,
                new[] { FoodCategoryId }),
            CreateBox(TentBoxItemId, "Packed tent", box),
            CreateBox(StoneMasonBoxItemId, "Packed stone mason workshop", box),
            CreateBox(WoodWorkshopBoxItemId, "Packed wooden workshop", box),
        }).ToArray();
    }

    public static IReadOnlyList<BuildingDefinition> CreateBuildings()
    {
        return new[]
        {
            CampfireBuildingBoxContent.Definition.Building,
            CreateBuilding(TentBuildingId, TentBoxItemId, "Tent", 2, 2),
            CreateBuilding(
                StoneMasonBuildingId,
                StoneMasonBoxItemId,
                "Stone mason workshop",
                3,
                3),
            CreateBuilding(
                WoodWorkshopBuildingId,
                WoodWorkshopBoxItemId,
                "Wooden workshop",
                3,
                3),
        };
    }

    public static IReadOnlyList<RecipeDefinition> CreateRecipes(long baseDurationTicks)
    {
        return new[]
        {
            BuildingRecipe(
                TentRecipeId,
                "Tent",
                new[] { Input(MushroomLegItemId, 2), Input(MushroomCapItemId, 1) },
                TentBoxItemId,
                Steps(baseDurationTicks, (MushroomLegItemId, 2, AgentSkillCatalog.Woodworking),
                    (MushroomCapItemId, 1, AgentSkillCatalog.Woodworking)),
                Grant(AgentSkillCatalog.Woodworking, 200)),
            BuildingRecipe(
                StoneMasonRecipeId,
                "Stone mason workshop",
                new[] { Input(MushroomLegItemId, 2), Input(StoneItemId, 4) },
                StoneMasonBoxItemId,
                Steps(baseDurationTicks, (MushroomLegItemId, 2, AgentSkillCatalog.Woodworking),
                    (StoneItemId, 4, AgentSkillCatalog.Stonework)),
                Grant(AgentSkillCatalog.Woodworking, 100, AgentSkillCatalog.Stonework, 300)),
            BuildingRecipe(
                WoodWorkshopRecipeId,
                "Wooden workshop",
                new[] { Input(MushroomLegItemId, 4), Input(MushroomCapItemId, 1) },
                WoodWorkshopBoxItemId,
                Steps(baseDurationTicks, (MushroomLegItemId, 4, AgentSkillCatalog.Woodworking),
                    (MushroomCapItemId, 1, AgentSkillCatalog.Woodworking)),
                Grant(AgentSkillCatalog.Woodworking, 300)),
            BuildingRecipe(
                CampfireRecipeId,
                "Campfire",
                new[] { Input(MushroomLegItemId, 2), Input(StoneItemId, 2) },
                CampfireBuildingBoxContent.CampfireBoxItemId,
                Steps(baseDurationTicks, (MushroomLegItemId, 2, AgentSkillCatalog.Woodworking),
                    (StoneItemId, 2, AgentSkillCatalog.Stonework)),
                Grant(AgentSkillCatalog.Woodworking, 100, AgentSkillCatalog.Stonework, 100)),
            FoodRecipe(
                GrilledMushroomRecipeId,
                "Grilled mushroom",
                MushroomCapItemId,
                GrilledMushroomItemId,
                baseDurationTicks,
                120),
            FoodRecipe(
                RoastedHamsterRecipeId,
                "Grilled hamster",
                HamsterItemId,
                RoastedHamsterItemId,
                baseDurationTicks,
                180),
        };
    }

    public static ProductionWorkstationDefinition CreateWorkstation()
    {
        return new ProductionWorkstationDefinition(
            CampfireBuildingBoxContent.CampfireBuildingId,
            AnimationProfileId,
            new[]
            {
                TentRecipeId,
                StoneMasonRecipeId,
                WoodWorkshopRecipeId,
                CampfireRecipeId,
                GrilledMushroomRecipeId,
                RoastedHamsterRecipeId,
            },
            new[]
            {
                new InternalStockRuleDefinition(MushroomCapItemId, 4, true, 400),
                new InternalStockRuleDefinition(MushroomLegItemId, 4, true, 300),
                new InternalStockRuleDefinition(StoneItemId, 4, true, 200),
                new InternalStockRuleDefinition(HamsterItemId, 2, false, 100),
            });
    }

    private static RecipeDefinition BuildingRecipe(
        RecipeId id,
        string name,
        ContentItemQuantity[] inputs,
        ItemId output,
        RecipeMaterialStepDefinition[] steps,
        SkillGrantProfile grant)
    {
        return new RecipeDefinition(
            id,
            name,
            CampfireBuildingBoxContent.CampfireBuildingId,
            inputs,
            new[] { Input(output, 1) },
            requiredWork: steps.Length,
            energyPerWorkTick: 0,
            skillGrantProfile: grant,
            materialSteps: steps,
            skillGrantScale: ProductionSkillGrantScale.PerOrder);
    }

    private static RecipeDefinition FoodRecipe(
        RecipeId id,
        string name,
        ItemId input,
        ItemId output,
        long baseDurationTicks,
        int grantUnits)
    {
        return new RecipeDefinition(
            id,
            name,
            CampfireBuildingBoxContent.CampfireBuildingId,
            new[] { Input(input, 1) },
            new[] { Input(output, 2) },
            requiredWork: 1,
            energyPerWorkTick: 0,
            skillGrantProfile: Grant(AgentSkillCatalog.Cooking, grantUnits),
            materialSteps: Steps(baseDurationTicks, (input, 1, AgentSkillCatalog.Cooking)),
            skillGrantScale: ProductionSkillGrantScale.PerOrder);
    }

    private static RecipeMaterialStepDefinition[] Steps(
        long duration,
        params (ItemId ItemId, int Quantity, AgentSkillId SkillId)[] groups)
    {
        return groups.SelectMany(group => Enumerable.Range(0, group.Quantity)
            .Select(_ => new RecipeMaterialStepDefinition(
                group.ItemId,
                group.SkillId,
                duration)))
            .ToArray();
    }

    private static SkillGrantProfile Grant(
        AgentSkillId first,
        int firstUnits,
        AgentSkillId? second = null,
        int secondUnits = 0)
    {
        List<SkillGrant> grants = new List<SkillGrant>
        {
            new SkillGrant(first, firstUnits),
        };
        if (second.HasValue)
        {
            grants.Add(new SkillGrant(second.Value, secondUnits));
        }

        return new SkillGrantProfile(grants);
    }

    private static ContentItemQuantity Input(ItemId itemId, int quantity)
    {
        return new ContentItemQuantity(itemId, quantity);
    }

    private static ItemDefinition CreateBox(
        ItemId itemId,
        string name,
        ItemCategoryId category)
    {
        return new ItemDefinition(itemId, name, 1, false, new[] { category });
    }

    private static BuildingDefinition CreateBuilding(
        BuildingDefinitionId id,
        ItemId boxItemId,
        string name,
        int assemblyWork,
        int packingWork)
    {
        return new BuildingDefinition(
            id,
            name,
            new[] { new CellOffset(0, 0) },
            new[]
            {
                new CellOffset(0, -1),
                new CellOffset(-1, 0),
                new CellOffset(1, 0),
                new CellOffset(0, 1),
            },
            Array.Empty<BuildingMaterialRequirement>(),
            assemblyWork,
            maximumDurability: 100,
            boxPolicy: new BuildingBoxPolicy(boxItemId, packingWork));
    }
}

}
