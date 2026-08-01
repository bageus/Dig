using System.Collections.Generic;
using Dig.Domain.Inventory;
using Dig.Domain.Production;

namespace Dig.Domain.Content
{

public static class ProductionPackageContent
{
    public static readonly ItemCategoryId PackageCategoryId =
        new ItemCategoryId("production.package");
    public static readonly ItemId UnfinishedPackageItemId =
        new ItemId("package.unfinished");
    public static readonly ItemId FoodPackageItemId =
        new ItemId("package.food");
    public static readonly ItemId WeaponPackageItemId =
        new ItemId("package.weapon");
    public static readonly ItemId ToolPackageItemId =
        new ItemId("package.tool");

    public static IReadOnlyList<ItemDefinition> CreateItems()
    {
        return new[]
        {
            Package(UnfinishedPackageItemId, "Unfinished package"),
            Package(FoodPackageItemId, "food"),
            Package(WeaponPackageItemId, "weapon"),
            Package(ToolPackageItemId, "tool"),
        };
    }

    public static ProductionOutputPackageKind ResolveKind(ItemDefinition output)
    {
        if (output.HasCategory(CampfireBuildingBoxContent.BuildingBoxCategoryId))
        {
            return ProductionOutputPackageKind.Building;
        }

        if (output.HasCategory(CampfireProductionContent.FoodCategoryId))
        {
            return ProductionOutputPackageKind.Food;
        }

        if (output.HasCategory(ResidentInventoryExpansionContent.WeaponCategoryId))
        {
            return ProductionOutputPackageKind.Weapon;
        }

        return ProductionOutputPackageKind.Tool;
    }

    public static ItemId GetClosedItemId(ProductionOutputPackageKind kind)
    {
        return kind switch
        {
            ProductionOutputPackageKind.Food => FoodPackageItemId,
            ProductionOutputPackageKind.Weapon => WeaponPackageItemId,
            ProductionOutputPackageKind.Tool => ToolPackageItemId,
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    private static ItemDefinition Package(ItemId id, string displayName)
    {
        return new ItemDefinition(
            id,
            displayName,
            maximumStackSize: 1,
            isTool: false,
            categories: new[] { PackageCategoryId });
    }
}

}
