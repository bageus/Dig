using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dig.Presentation.Management
{

public enum SettlementItemGroup
{
    Materials = 0,
    Weapons = 1,
    Food = 2,
    Potions = 3,
    Tools = 4,
}

public sealed class SettlementItemLocationViewModel
{
    public SettlementItemLocationViewModel(string id, string label)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Item location id and label are required.");
        }

        Id = id.Trim();
        Label = label.Trim();
    }

    public string Id { get; }
    public string Label { get; }
}

public sealed class SettlementItemSummaryRowViewModel
{
    private readonly IReadOnlyDictionary<string, int> _locations;

    public SettlementItemSummaryRowViewModel(
        string id,
        string label,
        SettlementItemGroup group,
        int total,
        IReadOnlyDictionary<string, int> locations)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Item row id and label are required.");
        }

        if (total < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(total));
        }

        Id = id.Trim();
        Label = label.Trim();
        Group = group;
        Total = total;
        _locations = new ReadOnlyDictionary<string, int>(
            (locations ?? throw new ArgumentNullException(nameof(locations)))
                .ToDictionary(
                    value => value.Key,
                    value => value.Value,
                    StringComparer.Ordinal));
    }

    public string Id { get; }
    public string Label { get; }
    public SettlementItemGroup Group { get; }
    public int Total { get; }

    public int GetQuantity(string locationId)
    {
        return _locations.TryGetValue(locationId, out int quantity) ? quantity : 0;
    }
}

public sealed class SettlementItemSummaryViewModel
{
    public SettlementItemSummaryViewModel(
        long inventoryVersion,
        IEnumerable<SettlementItemLocationViewModel> locations,
        IEnumerable<SettlementItemSummaryRowViewModel> rows)
    {
        if (inventoryVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inventoryVersion));
        }

        InventoryVersion = inventoryVersion;
        Locations = new ReadOnlyCollection<SettlementItemLocationViewModel>(
            (locations ?? throw new ArgumentNullException(nameof(locations))).ToArray());
        Rows = new ReadOnlyCollection<SettlementItemSummaryRowViewModel>(
            (rows ?? throw new ArgumentNullException(nameof(rows))).ToArray());
    }

    public long InventoryVersion { get; }
    public IReadOnlyList<SettlementItemLocationViewModel> Locations { get; }
    public IReadOnlyList<SettlementItemSummaryRowViewModel> Rows { get; }

    public IReadOnlyList<SettlementItemSummaryRowViewModel> GetRows(
        SettlementItemGroup group)
    {
        return new ReadOnlyCollection<SettlementItemSummaryRowViewModel>(
            Rows.Where(value => value.Group == group).ToArray());
    }
}

}
