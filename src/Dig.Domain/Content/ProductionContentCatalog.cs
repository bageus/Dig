using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Domain.Buildings;
using Dig.Domain.Inventory;

namespace Dig.Domain.Content
{

public sealed class ProductionContentCatalog
{
    private readonly Dictionary<RecipeId, RecipeDefinition> _recipes;
    private readonly Dictionary<TechnologyId, TechnologyDefinition> _technologies;

    private ProductionContentCatalog(
        ItemCatalog items,
        BuildingCatalog buildings,
        IReadOnlyCollection<RecipeDefinition> recipes,
        IReadOnlyCollection<TechnologyDefinition> technologies)
    {
        Items = items;
        Buildings = buildings;
        _recipes = recipes.ToDictionary(value => value.Id);
        _technologies = technologies.ToDictionary(value => value.Id);
        Recipes = new ReadOnlyCollection<RecipeDefinition>(
            recipes.OrderBy(value => value.Id).ToArray());
        Technologies = new ReadOnlyCollection<TechnologyDefinition>(
            technologies.OrderBy(value => value.Id).ToArray());
    }

    public ItemCatalog Items { get; }

    public BuildingCatalog Buildings { get; }

    public IReadOnlyList<RecipeDefinition> Recipes { get; }

    public IReadOnlyList<TechnologyDefinition> Technologies { get; }

    public RecipeDefinition GetRecipe(RecipeId id)
    {
        return _recipes.TryGetValue(id, out RecipeDefinition? value)
            ? value
            : throw new KeyNotFoundException($"Unknown recipe '{id}'.");
    }

    public TechnologyDefinition GetTechnology(TechnologyId id)
    {
        return _technologies.TryGetValue(id, out TechnologyDefinition? value)
            ? value
            : throw new KeyNotFoundException($"Unknown technology '{id}'.");
    }

    public bool ContainsRecipe(RecipeId id)
    {
        return _recipes.ContainsKey(id);
    }

    public bool ContainsTechnology(TechnologyId id)
    {
        return _technologies.ContainsKey(id);
    }

    public static ContentValidationResult ValidateAndCreate(
        ItemCatalog items,
        BuildingCatalog buildings,
        IEnumerable<RecipeDefinition> recipes,
        IEnumerable<TechnologyDefinition> technologies)
    {
        if (items is null || buildings is null || recipes is null || technologies is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        RecipeDefinition[] recipeValues = recipes.OrderBy(value => value.Id).ToArray();
        TechnologyDefinition[] technologyValues = technologies
            .OrderBy(value => value.Id)
            .ToArray();
        List<ContentValidationIssue> issues = new List<ContentValidationIssue>();
        ValidateUniqueIds(recipeValues, technologyValues, issues);
        ValidateRecipes(items, buildings, recipeValues, technologyValues, issues);
        ValidateTechnologies(items, recipeValues, technologyValues, issues);
        ValidateTechnologyCycles(technologyValues, issues);

        if (issues.Count > 0)
        {
            return new ContentValidationResult(null, issues);
        }

        ProductionContentCatalog catalog = new ProductionContentCatalog(
            items,
            buildings,
            recipeValues,
            technologyValues);
        return new ContentValidationResult(catalog, Array.Empty<ContentValidationIssue>());
    }

    private static void ValidateUniqueIds(
        IReadOnlyCollection<RecipeDefinition> recipes,
        IReadOnlyCollection<TechnologyDefinition> technologies,
        ICollection<ContentValidationIssue> issues)
    {
        foreach (IGrouping<RecipeId, RecipeDefinition> duplicate in recipes
            .GroupBy(value => value.Id)
            .Where(group => group.Count() > 1))
        {
            issues.Add(new ContentValidationIssue(
                "content.duplicate_recipe",
                $"recipes/{duplicate.Key}",
                "Recipe ids must be unique."));
        }

        foreach (IGrouping<TechnologyId, TechnologyDefinition> duplicate in technologies
            .GroupBy(value => value.Id)
            .Where(group => group.Count() > 1))
        {
            issues.Add(new ContentValidationIssue(
                "content.duplicate_technology",
                $"technologies/{duplicate.Key}",
                "Technology ids must be unique."));
        }
    }

    private static void ValidateRecipes(
        ItemCatalog items,
        BuildingCatalog buildings,
        IReadOnlyCollection<RecipeDefinition> recipes,
        IReadOnlyCollection<TechnologyDefinition> technologies,
        ICollection<ContentValidationIssue> issues)
    {
        HashSet<TechnologyId> technologyIds = technologies.Select(value => value.Id).ToHashSet();
        foreach (RecipeDefinition recipe in recipes)
        {
            string path = $"recipes/{recipe.Id}";
            if (!ContainsBuilding(buildings, recipe.WorkstationId))
            {
                issues.Add(new ContentValidationIssue(
                    "content.missing_building",
                    $"{path}/workstation",
                    $"Unknown building '{recipe.WorkstationId}'."));
            }

            ValidateItemQuantities(items, recipe.Inputs, $"{path}/inputs", issues);
            ValidateItemQuantities(items, recipe.Outputs, $"{path}/outputs", issues);
            foreach (ContentItemQuantity output in recipe.Outputs)
            {
                if (items.Contains(output.ItemId)
                    && output.Quantity > items.Get(output.ItemId).MaximumStackSize)
                {
                    issues.Add(new ContentValidationIssue(
                        "content.output_exceeds_stack",
                        $"{path}/outputs/{output.ItemId}",
                        "One production output must fit in one item stack."));
                }
            }

            if (recipe.RequiredToolItemId.HasValue)
            {
                ItemId toolId = recipe.RequiredToolItemId.Value;
                if (!items.Contains(toolId))
                {
                    issues.Add(new ContentValidationIssue(
                        "content.missing_tool",
                        $"{path}/tool",
                        $"Unknown tool item '{toolId}'."));
                }
                else if (!items.Get(toolId).IsTool)
                {
                    issues.Add(new ContentValidationIssue(
                        "content.item_not_tool",
                        $"{path}/tool",
                        $"Item '{toolId}' is not marked as a tool."));
                }
            }

            if (recipe.RequiredTechnologyId.HasValue
                && !technologyIds.Contains(recipe.RequiredTechnologyId.Value))
            {
                issues.Add(new ContentValidationIssue(
                    "content.missing_required_technology",
                    $"{path}/technology",
                    $"Unknown technology '{recipe.RequiredTechnologyId.Value}'."));
            }
        }
    }

    private static void ValidateTechnologies(
        ItemCatalog items,
        IReadOnlyCollection<RecipeDefinition> recipes,
        IReadOnlyCollection<TechnologyDefinition> technologies,
        ICollection<ContentValidationIssue> issues)
    {
        HashSet<RecipeId> recipeIds = recipes.Select(value => value.Id).ToHashSet();
        HashSet<TechnologyId> technologyIds = technologies.Select(value => value.Id).ToHashSet();
        foreach (TechnologyDefinition technology in technologies)
        {
            string path = $"technologies/{technology.Id}";
            ValidateItemQuantities(items, technology.ResearchItems, $"{path}/items", issues);
            foreach (TechnologyId prerequisite in technology.Prerequisites)
            {
                if (prerequisite == technology.Id || !technologyIds.Contains(prerequisite))
                {
                    issues.Add(new ContentValidationIssue(
                        "content.invalid_prerequisite",
                        $"{path}/prerequisites/{prerequisite}",
                        "Technology prerequisite must reference another existing technology."));
                }
            }

            foreach (RecipeId recipeId in technology.UnlockedRecipes)
            {
                if (!recipeIds.Contains(recipeId))
                {
                    issues.Add(new ContentValidationIssue(
                        "content.missing_unlocked_recipe",
                        $"{path}/recipes/{recipeId}",
                        $"Unknown recipe '{recipeId}'."));
                }
            }
        }
    }

    private static void ValidateTechnologyCycles(
        IReadOnlyCollection<TechnologyDefinition> technologies,
        ICollection<ContentValidationIssue> issues)
    {
        Dictionary<TechnologyId, TechnologyDefinition> graph =
            technologies.ToDictionary(value => value.Id);
        HashSet<TechnologyId> visited = new HashSet<TechnologyId>();
        List<TechnologyId> active = new List<TechnologyId>();

        void Visit(TechnologyId id)
        {
            if (active.Contains(id))
            {
                int start = active.IndexOf(id);
                string cycle = string.Join(" -> ", active.Skip(start).Append(id));
                issues.Add(new ContentValidationIssue(
                    "content.technology_cycle",
                    $"technologies/{id}/prerequisites",
                    $"Technology prerequisite cycle: {cycle}."));
                return;
            }

            if (!visited.Add(id) || !graph.TryGetValue(id, out TechnologyDefinition? definition))
            {
                return;
            }

            active.Add(id);
            foreach (TechnologyId prerequisite in definition.Prerequisites)
            {
                Visit(prerequisite);
            }

            active.RemoveAt(active.Count - 1);
        }

        foreach (TechnologyId id in graph.Keys.OrderBy(value => value))
        {
            Visit(id);
        }
    }

    private static void ValidateItemQuantities(
        ItemCatalog items,
        IEnumerable<ContentItemQuantity> quantities,
        string path,
        ICollection<ContentValidationIssue> issues)
    {
        foreach (ContentItemQuantity quantity in quantities)
        {
            if (!items.Contains(quantity.ItemId))
            {
                issues.Add(new ContentValidationIssue(
                    "content.missing_item",
                    $"{path}/{quantity.ItemId}",
                    $"Unknown item '{quantity.ItemId}'."));
            }
        }
    }

    private static bool ContainsBuilding(
        BuildingCatalog buildings,
        BuildingDefinitionId id)
    {
        try
        {
            buildings.Get(id);
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }
}
}
