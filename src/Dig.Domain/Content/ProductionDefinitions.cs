using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Domain.Buildings;
using Dig.Domain.Inventory;

namespace Dig.Domain.Content
{

public readonly struct RecipeId : IEquatable<RecipeId>, IComparable<RecipeId>
{
    private readonly string? _value;

    public RecipeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Recipe id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public int CompareTo(RecipeId other)
    {
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public bool Equals(RecipeId other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is RecipeId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);
    }

    public override string ToString()
    {
        return _value ?? string.Empty;
    }

    public static bool operator ==(RecipeId left, RecipeId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RecipeId left, RecipeId right)
    {
        return !left.Equals(right);
    }
}

public readonly struct TechnologyId : IEquatable<TechnologyId>, IComparable<TechnologyId>
{
    private readonly string? _value;

    public TechnologyId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Technology id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public int CompareTo(TechnologyId other)
    {
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public bool Equals(TechnologyId other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is TechnologyId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);
    }

    public override string ToString()
    {
        return _value ?? string.Empty;
    }

    public static bool operator ==(TechnologyId left, TechnologyId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TechnologyId left, TechnologyId right)
    {
        return !left.Equals(right);
    }
}

public readonly struct ContentItemQuantity
{
    public ContentItemQuantity(ItemId itemId, int quantity)
    {
        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Item id cannot be empty.", nameof(itemId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        ItemId = itemId;
        Quantity = quantity;
    }

    public ItemId ItemId { get; }

    public int Quantity { get; }
}

public sealed class RecipeDefinition
{
    private readonly ContentItemQuantity[] _inputs;
    private readonly ContentItemQuantity[] _outputs;

    public RecipeDefinition(
        RecipeId id,
        string displayName,
        BuildingDefinitionId workstationId,
        IEnumerable<ContentItemQuantity> inputs,
        IEnumerable<ContentItemQuantity> outputs,
        int requiredWork,
        int energyPerWorkTick,
        ItemId? requiredToolItemId = null,
        TechnologyId? requiredTechnologyId = null)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Recipe id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Recipe display name is required.", nameof(displayName));
        }

        if (workstationId.IsEmpty)
        {
            throw new ArgumentException("Workstation id cannot be empty.", nameof(workstationId));
        }

        if (requiredWork <= 0 || energyPerWorkTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredWork));
        }

        Id = id;
        DisplayName = displayName.Trim();
        WorkstationId = workstationId;
        _inputs = NormalizeQuantities(inputs, nameof(inputs));
        _outputs = NormalizeQuantities(outputs, nameof(outputs));
        RequiredWork = requiredWork;
        EnergyPerWorkTick = energyPerWorkTick;
        RequiredToolItemId = requiredToolItemId;
        RequiredTechnologyId = requiredTechnologyId;
    }

    public RecipeId Id { get; }

    public string DisplayName { get; }

    public BuildingDefinitionId WorkstationId { get; }

    public IReadOnlyList<ContentItemQuantity> Inputs =>
        new ReadOnlyCollection<ContentItemQuantity>(_inputs);

    public IReadOnlyList<ContentItemQuantity> Outputs =>
        new ReadOnlyCollection<ContentItemQuantity>(_outputs);

    public int RequiredWork { get; }

    public int EnergyPerWorkTick { get; }

    public ItemId? RequiredToolItemId { get; }

    public TechnologyId? RequiredTechnologyId { get; }

    private static ContentItemQuantity[] NormalizeQuantities(
        IEnumerable<ContentItemQuantity> values,
        string parameterName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        ContentItemQuantity[] normalized = values
            .GroupBy(value => value.ItemId)
            .Select(group => new ContentItemQuantity(
                group.Key,
                checked(group.Sum(value => value.Quantity))))
            .OrderBy(value => value.ItemId)
            .ToArray();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one item quantity is required.", parameterName);
        }

        return normalized;
    }
}

public sealed class TechnologyDefinition
{
    private readonly TechnologyId[] _prerequisites;
    private readonly RecipeId[] _unlockedRecipes;
    private readonly ContentItemQuantity[] _researchItems;

    public TechnologyDefinition(
        TechnologyId id,
        string displayName,
        IEnumerable<TechnologyId>? prerequisites,
        IEnumerable<RecipeId>? unlockedRecipes,
        IEnumerable<ContentItemQuantity> researchItems)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Technology id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Technology display name is required.", nameof(displayName));
        }

        Id = id;
        DisplayName = displayName.Trim();
        _prerequisites = (prerequisites ?? Array.Empty<TechnologyId>())
            .Distinct()
            .OrderBy(value => value)
            .ToArray();
        _unlockedRecipes = (unlockedRecipes ?? Array.Empty<RecipeId>())
            .Distinct()
            .OrderBy(value => value)
            .ToArray();
        _researchItems = NormalizeResearchItems(researchItems);
    }

    public TechnologyId Id { get; }

    public string DisplayName { get; }

    public IReadOnlyList<TechnologyId> Prerequisites =>
        new ReadOnlyCollection<TechnologyId>(_prerequisites);

    public IReadOnlyList<RecipeId> UnlockedRecipes =>
        new ReadOnlyCollection<RecipeId>(_unlockedRecipes);

    public IReadOnlyList<ContentItemQuantity> ResearchItems =>
        new ReadOnlyCollection<ContentItemQuantity>(_researchItems);

    private static ContentItemQuantity[] NormalizeResearchItems(
        IEnumerable<ContentItemQuantity> values)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        ContentItemQuantity[] result = values
            .GroupBy(value => value.ItemId)
            .Select(group => new ContentItemQuantity(
                group.Key,
                checked(group.Sum(value => value.Quantity))))
            .OrderBy(value => value.ItemId)
            .ToArray();
        if (result.Length == 0)
        {
            throw new ArgumentException("Technology requires at least one research item.");
        }

        return result;
    }
}
}
