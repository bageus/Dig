using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentInventoryExpansionContentTests
{
    [Fact]
    public void Four_stable_expansion_items_have_exact_capacity_and_penalties()
    {
        ResidentInventoryExpansionContent content =
            new ResidentInventoryExpansionContent();
        Dictionary<ItemId, ItemDefinition> items = content.Items
            .ToDictionary(item => item.Id);

        AssertExpansion(
            items[ResidentInventoryExpansionContent.BasketItemId],
            InventoryExpansionGroup.Cargo,
            tier: 1,
            slots: 4,
            speed: 0.75d,
            "visual.resident.basket");
        AssertExpansion(
            items[ResidentInventoryExpansionContent.LargeBasketItemId],
            InventoryExpansionGroup.Cargo,
            tier: 2,
            slots: 6,
            speed: 0.65d,
            "visual.resident.large_basket");
        AssertExpansion(
            items[ResidentInventoryExpansionContent.SheathItemId],
            InventoryExpansionGroup.Weapon,
            tier: 1,
            slots: 2,
            speed: 1d,
            "visual.resident.sheath");
        AssertExpansion(
            items[ResidentInventoryExpansionContent.WeaponHarnessItemId],
            InventoryExpansionGroup.Weapon,
            tier: 2,
            slots: 4,
            speed: 1d,
            "visual.resident.weapon_harness");
    }

    [Fact]
    public void Sheath_and_harness_recipes_match_design_inputs_and_workstations()
    {
        ResidentInventoryExpansionContent content =
            new ResidentInventoryExpansionContent();
        RecipeDefinition sheath = Assert.Single(
            content.Recipes,
            recipe => recipe.Id
                == ResidentInventoryExpansionContent.SheathRecipeId);
        RecipeDefinition harness = Assert.Single(
            content.Recipes,
            recipe => recipe.Id
                == ResidentInventoryExpansionContent.WeaponHarnessRecipeId);

        Assert.Equal(ResidentInventoryExpansionContent.ForgeBuildingId, sheath.WorkstationId);
        AssertQuantities(sheath.Inputs, new Dictionary<ItemId, int>
        {
            [ResidentInventoryExpansionContent.IronItemId] = 2,
            [ResidentInventoryExpansionContent.HamsterItemId] = 2,
        });
        Assert.Equal(
            ResidentInventoryExpansionContent.SheathItemId,
            Assert.Single(sheath.Outputs).ItemId);

        Assert.Equal(ResidentInventoryExpansionContent.ArsenalBuildingId, harness.WorkstationId);
        AssertQuantities(harness.Inputs, new Dictionary<ItemId, int>
        {
            [ResidentInventoryExpansionContent.IronItemId] = 3,
            [ResidentInventoryExpansionContent.HamsterItemId] = 2,
            [ResidentInventoryExpansionContent.GoldItemId] = 1,
            [ResidentInventoryExpansionContent.LegItemId] = 2,
        });
        Assert.Equal(
            ResidentInventoryExpansionContent.WeaponHarnessItemId,
            Assert.Single(harness.Outputs).ItemId);
    }

    [Fact]
    public void Complete_content_validates_against_items_categories_and_workstations()
    {
        ResidentInventoryExpansionContent content =
            new ResidentInventoryExpansionContent();

        ResidentInventoryExpansionContentValidationResult validation =
            ResidentInventoryExpansionContentValidator.Validate(
                content,
                CreateItems(content),
                CreateBuildings(includeArsenal: true));

        Assert.True(
            validation.Succeeded,
            string.Join(System.Environment.NewLine, validation.Issues));
        Assert.NotNull(validation.Production);
    }

    [Fact]
    public void Missing_hamster_and_leg_are_explicit_content_errors()
    {
        ResidentInventoryExpansionContent content =
            new ResidentInventoryExpansionContent();
        ItemDefinition[] items = CreateItems(content).Definitions
            .Where(item => item.Id != ResidentInventoryExpansionContent.HamsterItemId)
            .Where(item => item.Id != ResidentInventoryExpansionContent.LegItemId)
            .ToArray();

        ResidentInventoryExpansionContentValidationResult validation =
            ResidentInventoryExpansionContentValidator.Validate(
                content,
                new ItemCatalog(items),
                CreateBuildings(includeArsenal: true));

        Assert.Contains(validation.Issues, issue =>
            issue.Code == "content.missing_item"
            && issue.Path.Contains(
                ResidentInventoryExpansionContent.HamsterItemId.ToString()));
        Assert.Contains(validation.Issues, issue =>
            issue.Code == "content.missing_item"
            && issue.Path.Contains(
                ResidentInventoryExpansionContent.LegItemId.ToString()));
    }

    [Fact]
    public void Missing_arsenal_is_an_explicit_workstation_error()
    {
        ResidentInventoryExpansionContent content =
            new ResidentInventoryExpansionContent();

        ResidentInventoryExpansionContentValidationResult validation =
            ResidentInventoryExpansionContentValidator.Validate(
                content,
                CreateItems(content),
                CreateBuildings(includeArsenal: false));

        Assert.Contains(validation.Issues, issue =>
            issue.Code == "content.missing_building"
            && issue.Path.Contains(
                ResidentInventoryExpansionContent.WeaponHarnessRecipeId.ToString()));
    }

    private static ItemCatalog CreateItems(
        ResidentInventoryExpansionContent content)
    {
        ItemDefinition[] baseItems =
        {
            Item(
                ResidentInventoryExpansionContent.IronItemId,
                ResidentInventoryExpansionContent.RawMaterialCategoryId),
            Item(
                ResidentInventoryExpansionContent.GoldItemId,
                ResidentInventoryExpansionContent.RawMaterialCategoryId),
            Item(
                ResidentInventoryExpansionContent.HamsterItemId,
                ResidentInventoryExpansionContent.GeneralItemCategoryId),
            Item(
                ResidentInventoryExpansionContent.LegItemId,
                ResidentInventoryExpansionContent.GeneralItemCategoryId),
            Item(
                new ItemId("weapon.test_sword"),
                ResidentInventoryExpansionContent.WeaponCategoryId),
            Item(
                new ItemId("shield.test_round"),
                ResidentInventoryExpansionContent.ShieldCategoryId),
        };
        return new ItemCatalog(baseItems.Concat(content.Items));
    }

    private static BuildingCatalog CreateBuildings(bool includeArsenal)
    {
        List<BuildingDefinition> buildings = new List<BuildingDefinition>
        {
            Building(ResidentInventoryExpansionContent.ForgeBuildingId),
        };
        if (includeArsenal)
        {
            buildings.Add(Building(ResidentInventoryExpansionContent.ArsenalBuildingId));
        }

        return new BuildingCatalog(buildings);
    }

    private static BuildingDefinition Building(BuildingDefinitionId id)
    {
        return new BuildingDefinition(
            id,
            id.ToString(),
            new[] { new CellOffset(0, 0) },
            new[] { new CellOffset(1, 0) },
            new[]
            {
                new BuildingMaterialRequirement(
                    ResidentInventoryExpansionContent.IronItemId,
                    1),
            },
            requiredWork: 1,
            maximumDurability: 100);
    }

    private static ItemDefinition Item(ItemId id, ItemCategoryId category)
    {
        return new ItemDefinition(id, id.ToString(), 100, false, new[] { category });
    }

    private static void AssertExpansion(
        ItemDefinition item,
        InventoryExpansionGroup group,
        int tier,
        int slots,
        double speed,
        string visual)
    {
        Assert.Equal(1, item.MaximumStackSize);
        Assert.False(item.IsTool);
        Assert.NotNull(item.InventoryExpansion);
        InventoryExpansionDefinition expansion = item.InventoryExpansion!;
        Assert.Equal(group, expansion.Group);
        Assert.Equal(tier, expansion.Tier);
        Assert.Equal(slots, expansion.AddedSlots);
        Assert.Equal(speed, expansion.MoveSpeedMultiplierWhenOccupied);
        Assert.Equal(visual, expansion.VisualAttachmentId);
        Assert.True(expansion.IsMainCompartmentOnly);
    }

    private static void AssertQuantities(
        IEnumerable<ContentItemQuantity> actual,
        IReadOnlyDictionary<ItemId, int> expected)
    {
        Dictionary<ItemId, int> values = actual.ToDictionary(
            value => value.ItemId,
            value => value.Quantity);
        Assert.Equal(expected.Count, values.Count);
        foreach (KeyValuePair<ItemId, int> pair in expected)
        {
            Assert.Equal(pair.Value, values[pair.Key]);
        }
    }
}

}