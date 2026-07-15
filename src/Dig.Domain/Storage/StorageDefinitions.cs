using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Domain.Storage
{

public sealed class StorageFilter
{
    private readonly HashSet<ItemId> _allowedItems;
    private readonly HashSet<ItemCategoryId> _allowedCategories;

    public StorageFilter(
        bool acceptsAll,
        IEnumerable<ItemId>? allowedItems = null,
        IEnumerable<ItemCategoryId>? allowedCategories = null)
    {
        AcceptsAll = acceptsAll;
        _allowedItems = new HashSet<ItemId>(allowedItems ?? Array.Empty<ItemId>());
        _allowedCategories = new HashSet<ItemCategoryId>(
            allowedCategories ?? Array.Empty<ItemCategoryId>());

        if (!acceptsAll && _allowedItems.Count == 0 && _allowedCategories.Count == 0)
        {
            throw new ArgumentException(
                "A restrictive storage filter needs at least one item or category.");
        }

        AllowedItems = new ReadOnlyCollection<ItemId>(_allowedItems.OrderBy(item => item).ToArray());
        AllowedCategories = new ReadOnlyCollection<ItemCategoryId>(
            _allowedCategories.OrderBy(category => category).ToArray());
    }

    public bool AcceptsAll { get; }

    public IReadOnlyList<ItemId> AllowedItems { get; }

    public IReadOnlyList<ItemCategoryId> AllowedCategories { get; }

    public bool Accepts(ItemDefinition item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        return AcceptsAll
            || _allowedItems.Contains(item.Id)
            || item.Categories.Any(category => _allowedCategories.Contains(category));
    }

    public static StorageFilter All()
    {
        return new StorageFilter(acceptsAll: true);
    }
}

public sealed class StorageZoneDefinition
{
    public StorageZoneDefinition(
        EntityId id,
        string name,
        int priority,
        int capacity,
        StorageFilter filter)
        : this(id, name, priority, capacity, filter, new CellId(0, 0))
    {
    }

    public StorageZoneDefinition(
        EntityId id,
        string name,
        int priority,
        int capacity,
        StorageFilter filter,
        CellId cell)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Storage id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Storage name is required.", nameof(name));
        }

        if (priority < 0 || priority > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        Id = id;
        Name = name.Trim();
        Priority = priority;
        Capacity = capacity;
        Filter = filter ?? throw new ArgumentNullException(nameof(filter));
        Cell = cell;
    }

    public EntityId Id { get; }

    public string Name { get; }

    public int Priority { get; }

    public int Capacity { get; }

    public StorageFilter Filter { get; }

    public CellId Cell { get; }

    public StorageZoneDefinition MoveTo(CellId cell)
    {
        return new StorageZoneDefinition(
            Id,
            Name,
            Priority,
            Capacity,
            Filter,
            cell);
    }
}
}
