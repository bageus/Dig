using System.Collections.Generic;
using Dig.Domain.Inventory;

namespace Dig.Domain.Storage
{

public sealed partial class StorageFilter
{
    private readonly HashSet<ItemId> _allowedItems;
    private readonly HashSet<ItemCategoryId> _allowedCategories;

    public bool AcceptsAll { get; }

    public IReadOnlyList<ItemId> AllowedItems { get; }

    public IReadOnlyList<ItemCategoryId> AllowedCategories { get; }
}
}