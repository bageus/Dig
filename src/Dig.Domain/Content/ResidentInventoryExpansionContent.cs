using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Inventory;

namespace Dig.Domain.Content
{

public sealed class ResidentInventoryExpansionContent
{
    public static readonly ItemId BasketItemId = new ItemId("inventory.basket");
    public static readonly ItemId LargeBasketItemId =
        new ItemId("inventory.large_basket");
    public static readonly ItemId SheathItemId = new ItemId("inventory.sheath");
    public static readonly ItemId WeaponHarnessItemId =
        new ItemId("inventory.weapon_harness");

    public static readonly ItemId IronItemId = new ItemId("material.iron");
    public static readonly ItemId GoldItemId = new ItemId("material.gold");
    public static readonly ItemId HamsterItemId = new ItemId("creature.hamster");
    public static readonly ItemId LegItemId = new ItemId("creature.leg");

    public static readonly ItemCategoryId ExpansionCategoryId =
        new ItemCategoryId("inventory.expansion");
    public static readonly ItemCategoryId GeneralItemCategoryId =
        new ItemCategoryId("item.general");
    public static readonly ItemCategoryId RawMaterialCategoryId =
        new ItemCategoryId("material.raw");
    public static readonly ItemCategoryId WeaponCategoryId =
        new ItemCategoryId("equipment.weapon");
    public static readonly ItemCategoryId ShieldCategoryId =
        new ItemCategoryId("equipment.shield");

    public static readonly BuildingDefinitionId ForgeBuildingId =
        new BuildingDefinitionId("building.forge");
    public static readonly BuildingDefinitionId ArsenalBuildingId =
        new BuildingDefinitionId("building.arsenal");

    public static readonly RecipeId SheathRecipeId =
        new RecipeId("recipe.inventory.sheath");
    public static readonly RecipeId WeaponHarnessRecipeId =
        new RecipeId("recipe.inventory.weapon_harness");

    private readonly ItemDefinition[] _items;
    private readonly RecipeDefinition[] _recipes;

    public ResidentInventoryExpansionContent()
    {
        _items = new[]
        {
            Expansion(
                BasketItemId,
                "Basket",
                InventoryExpansionGroup.Cargo,
                tier: 1,
                slots: 4,
                speed: 0.75d,
                "visual.resident.basket"),
            Expansion(
                LargeBasketItemId,
                "Large basket",
                InventoryExpansionGroup.Cargo,
                tier: 2,
                slots: 6,
                speed: 0.65d,
                "visual.resident.large_basket"),
            Expansion(
                SheathItemId,
                "Sheath",
                InventoryExpansionGroup.Weapon,
                tier: 1,
                slots: 2,
                speed: 1d,
                "visual.resident.sheath"),
            Expansion(
                WeaponHarnessItemId,
                "Weapon harness",
                InventoryExpansionGroup.Weapon,
                tier: 2,
                slots: 4,
                speed: 1d,
                "visual.resident.weapon_harness"),
        };
        _recipes = new[]
        {
            new RecipeDefinition(
                SheathRecipeId,
                "Forge sheath",
                ForgeBuildingId,
                new[]
                {
                    new ContentItemQuantity(IronItemId, 2),
                    new ContentItemQuantity(HamsterItemId, 2),
                },
                new[] { new ContentItemQuantity(SheathItemId, 1) },
                requiredWork: 12,
                energyPerWorkTick: 0,
                skillGrantProfile: DefaultSkillProgressionContent.Catalog.GetProfile(
                    DefaultSkillGrantProfileIds.Metallurgy)),
            new RecipeDefinition(
                WeaponHarnessRecipeId,
                "Assemble weapon harness",
                ArsenalBuildingId,
                new[]
                {
                    new ContentItemQuantity(IronItemId, 3),
                    new ContentItemQuantity(HamsterItemId, 2),
                    new ContentItemQuantity(GoldItemId, 1),
                    new ContentItemQuantity(LegItemId, 2),
                },
                new[] { new ContentItemQuantity(WeaponHarnessItemId, 1) },
                requiredWork: 20,
                energyPerWorkTick: 0,
                skillGrantProfile: DefaultSkillProgressionContent.Catalog.GetProfile(
                    DefaultSkillGrantProfileIds.Metallurgy)),
        };
    }

    public IReadOnlyList<ItemDefinition> Items =>
        new ReadOnlyCollection<ItemDefinition>(_items);

    public IReadOnlyList<RecipeDefinition> Recipes =>
        new ReadOnlyCollection<RecipeDefinition>(_recipes);

    private static ItemDefinition Expansion(
        ItemId itemId,
        string name,
        InventoryExpansionGroup group,
        int tier,
        int slots,
        double speed,
        string visualAttachmentId)
    {
        ItemCategoryId[] accepted = group == InventoryExpansionGroup.Cargo
            ? new[] { GeneralItemCategoryId, RawMaterialCategoryId }
            : new[] { WeaponCategoryId, ShieldCategoryId };
        return new ItemDefinition(
            itemId,
            name,
            maximumStackSize: 1,
            isTool: false,
            new[] { ExpansionCategoryId },
            new InventoryExpansionDefinition(
                group,
                tier,
                slots,
                accepted,
                speed,
                visualAttachmentId));
    }
}

}
