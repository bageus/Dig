using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace Dig.Domain.Inventory
{

public readonly struct ItemId : IEquatable<ItemId>, IComparable<ItemId>
{
    private readonly string? _value;

    public ItemId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Item id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public int CompareTo(ItemId other)
    {
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public bool Equals(ItemId other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is ItemId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);
    }

    public override string ToString()
    {
        return _value ?? string.Empty;
    }

    public static bool operator ==(ItemId left, ItemId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ItemId left, ItemId right)
    {
        return !left.Equals(right);
    }
}

public readonly struct ItemCategoryId : IEquatable<ItemCategoryId>, IComparable<ItemCategoryId>
{
    private readonly string? _value;

    public ItemCategoryId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Item category id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public int CompareTo(ItemCategoryId other)
    {
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public bool Equals(ItemCategoryId other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is ItemCategoryId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);
    }

    public override string ToString()
    {
        return _value ?? string.Empty;
    }
}

public sealed class ItemDefinition
{
    private readonly ItemCategoryId[] _categories;

    public ItemDefinition(
        ItemId id,
        string displayName,
        int maximumStackSize,
        bool isTool,
        IEnumerable<ItemCategoryId>? categories = null)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Item id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Item display name is required.", nameof(displayName));
        }

        if (maximumStackSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumStackSize));
        }

        Id = id;
        DisplayName = displayName.Trim();
        MaximumStackSize = maximumStackSize;
        IsTool = isTool;
        _categories = (categories ?? Array.Empty<ItemCategoryId>())
            .Distinct()
            .OrderBy(category => category)
            .ToArray();
    }

    public ItemId Id { get; }

    public string DisplayName { get; }

    public int MaximumStackSize { get; }

    public bool IsTool { get; }

    public IReadOnlyList<ItemCategoryId> Categories =>
        new ReadOnlyCollection<ItemCategoryId>(_categories);

    public bool HasCategory(ItemCategoryId categoryId)
    {
        return Array.BinarySearch(_categories, categoryId) >= 0;
    }
}

public sealed class ItemCatalog
{
    private readonly Dictionary<ItemId, ItemDefinition> _definitions;

    public ItemCatalog(IEnumerable<ItemDefinition> definitions)
    {
        if (definitions is null)
        {
            throw new ArgumentNullException(nameof(definitions));
        }

        ItemDefinition[] values = definitions.OrderBy(item => item.Id).ToArray();
        if (values.Length == 0)
        {
            throw new ArgumentException("Item catalog cannot be empty.", nameof(definitions));
        }

        if (values.Select(item => item.Id).Distinct().Count() != values.Length)
        {
            throw new ArgumentException("Item catalog ids must be unique.", nameof(definitions));
        }

        _definitions = values.ToDictionary(item => item.Id);
        Definitions = new ReadOnlyCollection<ItemDefinition>(values);
    }

    public IReadOnlyList<ItemDefinition> Definitions { get; }

    public ItemDefinition Get(ItemId itemId)
    {
        return _definitions.TryGetValue(itemId, out ItemDefinition? definition)
            ? definition
            : throw new KeyNotFoundException($"Item '{itemId}' is not in the catalog.");
    }

    public bool Contains(ItemId itemId)
    {
        return _definitions.ContainsKey(itemId);
    }
}
}
