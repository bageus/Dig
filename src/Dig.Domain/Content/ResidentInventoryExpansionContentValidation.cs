using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Inventory;

namespace Dig.Domain.Content
{

public sealed class ResidentInventoryExpansionContentValidationResult
{
    public ResidentInventoryExpansionContentValidationResult(
        ProductionContentCatalog? production,
        IReadOnlyCollection<ContentValidationIssue> issues)
    {
        Production = production;
        Issues = new ReadOnlyCollection<ContentValidationIssue>(
            (issues ?? throw new ArgumentNullException(nameof(issues)))
                .OrderBy(issue => issue.Path, StringComparer.Ordinal)
                .ThenBy(issue => issue.Code, StringComparer.Ordinal)
                .ToArray());
    }

    public ProductionContentCatalog? Production { get; }

    public IReadOnlyList<ContentValidationIssue> Issues { get; }

    public bool Succeeded => Production is not null && Issues.Count == 0;
}

public static class ResidentInventoryExpansionContentValidator
{
    private static readonly ItemId[] RequiredExpansionIds =
    {
        ResidentInventoryExpansionContent.BasketItemId,
        ResidentInventoryExpansionContent.LargeBasketItemId,
        ResidentInventoryExpansionContent.SheathItemId,
        ResidentInventoryExpansionContent.WeaponHarnessItemId,
    };

    public static ResidentInventoryExpansionContentValidationResult Validate(
        ResidentInventoryExpansionContent content,
        ItemCatalog items,
        BuildingCatalog buildings)
    {
        if (content is null || items is null || buildings is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        List<ContentValidationIssue> issues = new List<ContentValidationIssue>();
        ValidateExpansionItems(items, issues);
        ContentValidationResult production = ProductionContentCatalog.ValidateAndCreate(
            items,
            buildings,
            content.Recipes,
            Array.Empty<TechnologyDefinition>());
        issues.AddRange(production.Issues);
        return new ResidentInventoryExpansionContentValidationResult(
            issues.Count == 0 ? production.Catalog : null,
            issues);
    }

    private static void ValidateExpansionItems(
        ItemCatalog items,
        ICollection<ContentValidationIssue> issues)
    {
        HashSet<ItemCategoryId> categories = items.Definitions
            .SelectMany(item => item.Categories)
            .ToHashSet();
        for (int index = 0; index < RequiredExpansionIds.Length; index++)
        {
            ItemId itemId = RequiredExpansionIds[index];
            if (!items.Contains(itemId))
            {
                issues.Add(new ContentValidationIssue(
                    "content.missing_inventory_expansion",
                    $"items/{itemId}",
                    "Required resident inventory expansion is missing."));
                continue;
            }

            ItemDefinition item = items.Get(itemId);
            InventoryExpansionDefinition? expansion = item.InventoryExpansion;
            if (expansion is null)
            {
                issues.Add(new ContentValidationIssue(
                    "content.item_not_inventory_expansion",
                    $"items/{itemId}/inventoryExpansion",
                    "Required resident inventory item has no expansion definition."));
                continue;
            }

            if (item.MaximumStackSize != 1 || !expansion.IsMainCompartmentOnly)
            {
                issues.Add(new ContentValidationIssue(
                    "content.invalid_inventory_expansion_contract",
                    $"items/{itemId}/inventoryExpansion",
                    "Inventory expansions must be non-stackable and Main-only."));
            }

            foreach (ItemCategoryId accepted in expansion.AcceptedCategories)
            {
                if (!categories.Contains(accepted))
                {
                    issues.Add(new ContentValidationIssue(
                        "content.missing_item_category",
                        $"items/{itemId}/inventoryExpansion/categories/{accepted}",
                        $"Unknown accepted item category '{accepted}'."));
                }
            }
        }
    }
}

}