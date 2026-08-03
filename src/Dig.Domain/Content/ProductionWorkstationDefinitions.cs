using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Inventory;

namespace Dig.Domain.Content
{

public enum ProductionSkillGrantScale
{
    PerOutputUnit = 0,
    PerOrder = 1,
}

public readonly struct RecipeMaterialStepDefinition
{
    public RecipeMaterialStepDefinition(
        ItemId itemId,
        AgentSkillId skillId,
        long baseDurationTicks)
    {
        if (itemId.IsEmpty || skillId.IsEmpty)
        {
            throw new ArgumentException("Material step ids are required.");
        }

        if (baseDurationTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseDurationTicks));
        }

        ItemId = itemId;
        SkillId = skillId;
        BaseDurationTicks = baseDurationTicks;
    }

    public ItemId ItemId { get; }

    public AgentSkillId SkillId { get; }

    public long BaseDurationTicks { get; }
}

public readonly struct InternalStockRuleDefinition
{
    public InternalStockRuleDefinition(
        ItemId itemId,
        int capacity,
        bool defaultDeliveryEnabled,
        int priority)
    {
        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Stock item id is required.", nameof(itemId));
        }

        if (capacity <= 0 || priority < 0 || priority > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        ItemId = itemId;
        Capacity = capacity;
        DefaultDeliveryEnabled = defaultDeliveryEnabled;
        Priority = priority;
    }

    public ItemId ItemId { get; }

    public int Capacity { get; }

    public bool DefaultDeliveryEnabled { get; }

    public int Priority { get; }
}

public sealed class ProductionWorkstationDefinition
{
    private readonly RecipeId[] _recipeIds;
    private readonly InternalStockRuleDefinition[] _stockRules;

    public ProductionWorkstationDefinition(
        BuildingDefinitionId buildingId,
        string animationProfileId,
        IEnumerable<RecipeId> recipeIds,
        IEnumerable<InternalStockRuleDefinition> stockRules)
    {
        if (buildingId.IsEmpty)
        {
            throw new ArgumentException("Building id is required.", nameof(buildingId));
        }

        if (string.IsNullOrWhiteSpace(animationProfileId))
        {
            throw new ArgumentException(
                "Animation profile id is required.",
                nameof(animationProfileId));
        }

        if (recipeIds is null || stockRules is null)
        {
            throw new ArgumentNullException(nameof(recipeIds));
        }

        _recipeIds = recipeIds.Distinct().OrderBy(value => value).ToArray();
        _stockRules = stockRules
            .OrderByDescending(value => value.Priority)
            .ThenBy(value => value.ItemId)
            .ToArray();
        if (_recipeIds.Length == 0 || _stockRules.Length == 0)
        {
            throw new ArgumentException(
                "A production workstation needs recipes and stock rules.");
        }

        if (_stockRules.Select(value => value.ItemId).Distinct().Count()
            != _stockRules.Length)
        {
            throw new ArgumentException("Stock rule item ids must be unique.");
        }

        BuildingId = buildingId;
        AnimationProfileId = animationProfileId.Trim();
    }

    public BuildingDefinitionId BuildingId { get; }

    public string AnimationProfileId { get; }

    public IReadOnlyList<RecipeId> RecipeIds =>
        new ReadOnlyCollection<RecipeId>(_recipeIds);

    public IReadOnlyList<InternalStockRuleDefinition> StockRules =>
        new ReadOnlyCollection<InternalStockRuleDefinition>(_stockRules);

    public InternalStockRuleDefinition GetStockRule(ItemId itemId)
    {
        foreach (InternalStockRuleDefinition rule in _stockRules)
        {
            if (rule.ItemId == itemId)
            {
                return rule;
            }
        }

        throw new KeyNotFoundException(
            $"Workstation '{BuildingId}' has no stock rule for '{itemId}'.");
    }
}

public static class ProductionStepTiming
{
    public const int MaximumSkillSpeedupPercent = 50;

    public static long ResolveDurationTicks(long baseDurationTicks, int skillUnits)
    {
        if (baseDurationTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseDurationTicks));
        }

        int points = Math.Max(
            0,
            Math.Min(
                MaximumSkillSpeedupPercent,
                skillUnits / AgentSkillCatalog.UnitsPerPoint));
        decimal scaled = baseDurationTicks * (100m - points) / 100m;
        return Math.Max(1L, decimal.ToInt64(decimal.Round(
            scaled,
            0,
            MidpointRounding.AwayFromZero)));
    }
}

}
