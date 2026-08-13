using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Inventory;

namespace Dig.Domain.Content
{

public static class WorkshopProductionContent
{
    public const string WoodAnimationProfileId = "production.animation.wood_workshop";
    public const string StoneAnimationProfileId = "production.animation.stone_workshop";

    public static readonly ItemId SlingshotItemId = new ItemId("weapon.slingshot");
    public static readonly ItemId WoodenDoorBoxItemId =
        new ItemId("building_box.wooden_door");
    public static readonly ItemId LadderBoxItemId = new ItemId("building_box.ladder");
    public static readonly ItemId FarmBoxItemId = new ItemId("building_box.farm");
    public static readonly ItemId BorderStoneBoxItemId =
        new ItemId("building_box.border_stone");
    public static readonly ItemId PressTrapBoxItemId =
        new ItemId("building_box.press_trap");
    public static readonly ItemId StoneDoorBoxItemId =
        new ItemId("building_box.stone_door");

    public static readonly BuildingDefinitionId WoodenDoorBuildingId =
        new BuildingDefinitionId("building.wooden_door");
    public static readonly BuildingDefinitionId LadderBuildingId =
        new BuildingDefinitionId("building.ladder");
    public static readonly BuildingDefinitionId FarmBuildingId =
        new BuildingDefinitionId("building.farm");
    public static readonly BuildingDefinitionId BorderStoneBuildingId =
        new BuildingDefinitionId("building.border_stone");
    public static readonly BuildingDefinitionId PressTrapBuildingId =
        new BuildingDefinitionId("building.press_trap");
    public static readonly BuildingDefinitionId StoneDoorBuildingId =
        new BuildingDefinitionId("building.stone_door");

    public static readonly RecipeId BasketRecipeId =
        new RecipeId("recipe.wood_workshop.basket");
    public static readonly RecipeId WoodenDoorRecipeId =
        new RecipeId("recipe.wood_workshop.wooden_door");
    public static readonly RecipeId LadderRecipeId =
        new RecipeId("recipe.wood_workshop.ladder");
    public static readonly RecipeId FarmRecipeId =
        new RecipeId("recipe.wood_workshop.farm");
    public static readonly RecipeId BorderStoneRecipeId =
        new RecipeId("recipe.stone_workshop.border_stone");
    public static readonly RecipeId PressTrapRecipeId =
        new RecipeId("recipe.stone_workshop.press_trap");
    public static readonly RecipeId ClubRecipeId =
        new RecipeId("recipe.stone_workshop.club");
    public static readonly RecipeId SlingshotRecipeId =
        new RecipeId("recipe.stone_workshop.slingshot");
    public static readonly RecipeId StoneDoorRecipeId =
        new RecipeId("recipe.stone_workshop.stone_door");

    public static IReadOnlyList<ItemDefinition> CreateItems()
    {
        ItemCategoryId box = CampfireBuildingBoxContent.BuildingBoxCategoryId;
        return new[]
        {
            Box(WoodenDoorBoxItemId, "Packed wooden door", box),
            Box(LadderBoxItemId, "Packed ladder", box),
            Box(FarmBoxItemId, "Packed farm", box),
            Box(BorderStoneBoxItemId, "Packed border stone", box),
            Box(PressTrapBoxItemId, "Packed press trap", box),
            Box(StoneDoorBoxItemId, "Packed stone door", box),
            new ItemDefinition(
                SlingshotItemId,
                "Slingshot",
                maximumStackSize: 1,
                isTool: true,
                new[] { ResidentInventoryExpansionContent.WeaponCategoryId }),
        };
    }

    public static IReadOnlyList<BuildingDefinition> CreateBuildings()
    {
        return new[]
        {
            Building(WoodenDoorBuildingId, WoodenDoorBoxItemId, "Wooden door", 2),
            Building(LadderBuildingId, LadderBoxItemId, "Ladder", 2),
            Building(FarmBuildingId, FarmBoxItemId, "Farm", 4),
            Building(BorderStoneBuildingId, BorderStoneBoxItemId, "Border stone", 1),
            Building(PressTrapBuildingId, PressTrapBoxItemId, "Press trap", 4),
            Building(StoneDoorBuildingId, StoneDoorBoxItemId, "Stone door", 3),
        };
    }

    public static IReadOnlyList<RecipeDefinition> CreateRecipes(long durationTicks)
    {
        if (durationTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationTicks));
        }

        ItemId leg = CampfireProductionContent.MushroomLegItemId;
        ItemId cap = CampfireProductionContent.MushroomCapItemId;
        ItemId stone = CampfireProductionContent.StoneItemId;
        BuildingDefinitionId wood = CampfireProductionContent.WoodWorkshopBuildingId;
        BuildingDefinitionId mason = CampfireProductionContent.StoneMasonBuildingId;
        return new[]
        {
            Recipe(BasketRecipeId, "Basket", wood,
                Inputs((leg, 1)), ResidentInventoryExpansionContent.BasketItemId,
                durationTicks, AgentSkillCatalog.Woodworking),
            Recipe(WoodenDoorRecipeId, "Wooden door", wood,
                Inputs((leg, 2), (cap, 1), (stone, 1)), WoodenDoorBoxItemId,
                durationTicks, AgentSkillCatalog.Woodworking),
            Recipe(LadderRecipeId, "Ladder", wood,
                Inputs((leg, 2)), LadderBoxItemId,
                durationTicks, AgentSkillCatalog.Woodworking),
            Recipe(FarmRecipeId, "Farm", wood,
                Inputs((leg, 5), (cap, 2)), FarmBoxItemId,
                durationTicks, AgentSkillCatalog.Woodworking),
            Recipe(BorderStoneRecipeId, "Border stone", mason,
                Inputs((stone, 1)), BorderStoneBoxItemId,
                durationTicks, AgentSkillCatalog.Stonework),
            Recipe(PressTrapRecipeId, "Press trap", mason,
                Inputs((stone, 4), (leg, 1)), PressTrapBoxItemId,
                durationTicks, AgentSkillCatalog.Stonework),
            Recipe(ClubRecipeId, "Club", mason,
                Inputs((stone, 1), (leg, 1)), CombatEquipmentContent.ClubItemId,
                durationTicks, AgentSkillCatalog.Stonework),
            Recipe(SlingshotRecipeId, "Slingshot", mason,
                Inputs((stone, 1), (leg, 1)), SlingshotItemId,
                durationTicks, AgentSkillCatalog.Stonework),
            Recipe(StoneDoorRecipeId, "Stone door", mason,
                Inputs((leg, 1), (cap, 1), (stone, 2)), StoneDoorBoxItemId,
                durationTicks, AgentSkillCatalog.Stonework),
        };
    }

    public static IReadOnlyList<ProductionWorkstationDefinition> CreateWorkstations()
    {
        ItemId leg = CampfireProductionContent.MushroomLegItemId;
        ItemId cap = CampfireProductionContent.MushroomCapItemId;
        ItemId stone = CampfireProductionContent.StoneItemId;
        return new[]
        {
            new ProductionWorkstationDefinition(
                CampfireProductionContent.WoodWorkshopBuildingId,
                WoodAnimationProfileId,
                new[] { BasketRecipeId, WoodenDoorRecipeId, LadderRecipeId, FarmRecipeId },
                Stock((leg, 5), (cap, 2), (stone, 2))),
            new ProductionWorkstationDefinition(
                CampfireProductionContent.StoneMasonBuildingId,
                StoneAnimationProfileId,
                new[]
                {
                    BorderStoneRecipeId,
                    PressTrapRecipeId,
                    ClubRecipeId,
                    SlingshotRecipeId,
                    StoneDoorRecipeId,
                },
                Stock((leg, 2), (cap, 2), (stone, 5))),
        };
    }

    private static RecipeDefinition Recipe(
        RecipeId id,
        string name,
        BuildingDefinitionId workstation,
        ContentItemQuantity[] inputs,
        ItemId output,
        long duration,
        AgentSkillId skill)
    {
        RecipeMaterialStepDefinition[] steps = inputs
            .SelectMany(input => Enumerable.Range(0, input.Quantity)
                .Select(_ => new RecipeMaterialStepDefinition(input.ItemId, skill, duration)))
            .ToArray();
        return new RecipeDefinition(
            id,
            name,
            workstation,
            inputs,
            new[] { new ContentItemQuantity(output, 1) },
            requiredWork: steps.Length,
            energyPerWorkTick: 0,
            skillGrantProfile: new SkillGrantProfile(
                new[] { new SkillGrant(skill, steps.Length * 100) }),
            materialSteps: steps,
            skillGrantScale: ProductionSkillGrantScale.PerOrder);
    }

    private static ContentItemQuantity[] Inputs(
        params (ItemId ItemId, int Quantity)[] values) => values
        .Select(value => new ContentItemQuantity(value.ItemId, value.Quantity))
        .ToArray();

    private static InternalStockRuleDefinition[] Stock(
        params (ItemId ItemId, int Capacity)[] values) => values
        .Select((value, index) => new InternalStockRuleDefinition(
            value.ItemId,
            value.Capacity,
            defaultDeliveryEnabled: true,
            priority: 400 - (index * 100)))
        .ToArray();

    private static ItemDefinition Box(ItemId id, string name, ItemCategoryId category) =>
        new ItemDefinition(id, name, 1, false, new[] { category });

    private static BuildingDefinition Building(
        BuildingDefinitionId id,
        ItemId box,
        string name,
        int work) => new BuildingDefinition(
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
        work,
        maximumDurability: 100,
        boxPolicy: new BuildingBoxPolicy(box, packingWork: work));
}

}
