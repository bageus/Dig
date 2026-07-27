using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Inventory;

namespace Dig.Domain.Content
{

public sealed partial class ProductionContentCatalog
{
    private static void ValidateWorkstations(
        ItemCatalog items,
        BuildingCatalog buildings,
        IReadOnlyCollection<RecipeDefinition> recipes,
        IReadOnlyCollection<ProductionWorkstationDefinition> workstations,
        ICollection<ContentValidationIssue> issues)
    {
        Dictionary<RecipeId, RecipeDefinition> recipeById =
            recipes.ToDictionary(value => value.Id);
        foreach (IGrouping<BuildingDefinitionId, ProductionWorkstationDefinition> duplicate
            in workstations.GroupBy(value => value.BuildingId)
                .Where(group => group.Count() > 1))
        {
            issues.Add(new ContentValidationIssue(
                "content.duplicate_workstation",
                $"workstations/{duplicate.Key}",
                "Production workstation building ids must be unique."));
        }

        foreach (ProductionWorkstationDefinition workstation in workstations)
        {
            ValidateWorkstation(items, buildings, recipeById, workstation, issues);
        }
    }

    private static void ValidateWorkstation(
        ItemCatalog items,
        BuildingCatalog buildings,
        IReadOnlyDictionary<RecipeId, RecipeDefinition> recipes,
        ProductionWorkstationDefinition workstation,
        ICollection<ContentValidationIssue> issues)
    {
        string path = $"workstations/{workstation.BuildingId}";
        if (!ContainsBuilding(buildings, workstation.BuildingId))
        {
            issues.Add(new ContentValidationIssue(
                "content.missing_building",
                path,
                $"Unknown building '{workstation.BuildingId}'."));
        }

        foreach (RecipeId recipeId in workstation.RecipeIds)
        {
            if (!recipes.TryGetValue(recipeId, out RecipeDefinition? recipe))
            {
                issues.Add(new ContentValidationIssue(
                    "content.missing_workstation_recipe",
                    $"{path}/recipes/{recipeId}",
                    $"Unknown recipe '{recipeId}'."));
            }
            else if (recipe.WorkstationId != workstation.BuildingId)
            {
                issues.Add(new ContentValidationIssue(
                    "content.workstation_recipe_mismatch",
                    $"{path}/recipes/{recipeId}",
                    "Recipe workstation must match its workstation definition."));
            }
        }

        foreach (InternalStockRuleDefinition rule in workstation.StockRules)
        {
            if (!items.Contains(rule.ItemId))
            {
                issues.Add(new ContentValidationIssue(
                    "content.missing_item",
                    $"{path}/stock/{rule.ItemId}",
                    $"Unknown item '{rule.ItemId}'."));
            }
        }
    }
}

}
