using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Inventory;
using Dig.Presentation.Buildings;

namespace Dig.Presentation.Management
{

public sealed class SettlementItemSummaryPresenter
{
    private static readonly MaterialRowDefinition[] Materials =
    {
        Material("material.stone", "Stones"),
        Material("material.mushroom_stem", "Stems", "mushroom.stem", "material.stem"),
        Material("material.mushroom_cap", "Hats", "mushroom.cap", "material.hat"),
        Material("ore.gold", "Gold ores"),
        Material("ore.iron", "Iron ores"),
        Material("material.coal", "Coal"),
        Material("ore.crystal", "Crystal ores"),
        Material("material.gold", "Gold"),
        Material("material.iron", "Iron"),
        Material("material.crystal", "Crystals"),
        Material("creature.hamster", "Hamsters"),
        Material("creature.grub", "Grubs", "creature.larva"),
    };

    public SettlementItemSummaryViewModel Present(
        InventorySnapshot inventory,
        ItemCatalog catalog,
        IReadOnlyList<BuildingWorldViewModel> buildings)
    {
        if (inventory is null || catalog is null || buildings is null)
        {
            throw new ArgumentNullException(
                inventory is null ? nameof(inventory) : catalog is null
                    ? nameof(catalog) : nameof(buildings));
        }

        Dictionary<string, string> buildingNames = buildings.ToDictionary(
            value => value.Id,
            value => value.Name,
            StringComparer.Ordinal);
        SettlementItemLocationViewModel[] locations = buildings
            .Select(value => new SettlementItemLocationViewModel(value.Id, value.Name))
            .Concat(inventory.Stacks
                .Where(value => IsContainer(value.Location))
                .Select(value => CreateLocation(value.Location, buildingNames)))
            .GroupBy(value => value.Id, StringComparer.Ordinal)
            .Select(group => group.OrderBy(value => value.Label, StringComparer.Ordinal).First())
            .OrderBy(value => value.Label, StringComparer.Ordinal)
            .ThenBy(value => value.Id, StringComparer.Ordinal)
            .ToArray();
        List<SettlementItemSummaryRowViewModel> rows = new List<SettlementItemSummaryRowViewModel>();
        HashSet<string> materialAliases = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < Materials.Length; index++)
        {
            MaterialRowDefinition material = Materials[index];
            rows.Add(CreateRow(
                material.Id,
                material.Label,
                SettlementItemGroup.Materials,
                material.Ids,
                inventory,
                locations));
            materialAliases.UnionWith(material.Ids);
        }

        foreach (ItemDefinition definition in catalog.Definitions)
        {
            string id = definition.Id.ToString();
            if (materialAliases.Contains(id))
            {
                continue;
            }

            SettlementItemGroup? group = Classify(definition);
            if (!group.HasValue)
            {
                continue;
            }

            rows.Add(CreateRow(
                id,
                definition.DisplayName,
                group.Value,
                new[] { id },
                inventory,
                locations));
        }

        SettlementItemSummaryRowViewModel[] ordered = rows
            .OrderBy(value => value.Group)
            .ThenBy(value => value.Label, StringComparer.Ordinal)
            .ThenBy(value => value.Id, StringComparer.Ordinal)
            .ToArray();
        return new SettlementItemSummaryViewModel(
            inventory.Version,
            new ReadOnlyCollection<SettlementItemLocationViewModel>(locations),
            new ReadOnlyCollection<SettlementItemSummaryRowViewModel>(ordered));
    }

    private static SettlementItemSummaryRowViewModel CreateRow(
        string id,
        string label,
        SettlementItemGroup group,
        IReadOnlyCollection<string> itemIds,
        InventorySnapshot inventory,
        IReadOnlyList<SettlementItemLocationViewModel> locations)
    {
        HashSet<ItemId> ids = new HashSet<ItemId>(
            itemIds.Select(value => new ItemId(value)));
        ItemStackSnapshot[] stacks = inventory.Stacks
            .Where(value => ids.Contains(value.ItemId))
            .ToArray();
        Dictionary<string, int> quantities = locations.ToDictionary(
            value => value.Id,
            value => stacks.Where(stack => LocationId(stack.Location) == value.Id)
                .Sum(stack => stack.Quantity),
            StringComparer.Ordinal);
        return new SettlementItemSummaryRowViewModel(
            id,
            label,
            group,
            stacks.Sum(value => value.Quantity),
            quantities);
    }

    private static SettlementItemGroup? Classify(ItemDefinition definition)
    {
        string id = definition.Id.ToString();
        string[] categories = definition.Categories
            .Select(value => value.ToString())
            .ToArray();
        if (Matches(id, categories, "potion", "elixir"))
        {
            return SettlementItemGroup.Potions;
        }

        if (Matches(id, categories, "food", "meal", "drink", "beverage"))
        {
            return SettlementItemGroup.Food;
        }

        if (Matches(id, categories, "weapon", "shield"))
        {
            return SettlementItemGroup.Weapons;
        }

        if (definition.IsTool
            || definition.IsInventoryExpansion
            || Matches(id, categories, "tool", "mount", "inventory"))
        {
            return SettlementItemGroup.Tools;
        }

        return Matches(id, categories, "material", "ore", "raw", "creature")
            ? SettlementItemGroup.Materials
            : null;
    }

    private static bool Matches(
        string id,
        IReadOnlyList<string> categories,
        params string[] tokens)
    {
        for (int index = 0; index < tokens.Length; index++)
        {
            string token = tokens[index];
            if (id.StartsWith(token + ".", StringComparison.Ordinal)
                || id.IndexOf("." + token + ".", StringComparison.Ordinal) >= 0
                || categories.Any(value => value.IndexOf(token, StringComparison.Ordinal) >= 0))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsContainer(ItemLocation location)
    {
        return location.Kind == ItemLocationKind.BuildingInventory
            || location.Kind == ItemLocationKind.Storage;
    }

    private static SettlementItemLocationViewModel CreateLocation(
        ItemLocation location,
        IReadOnlyDictionary<string, string> buildingNames)
    {
        string owner = location.OwnerId.ToString();
        string label = buildingNames.TryGetValue(owner, out string name)
            ? name
            : location.Kind == ItemLocationKind.Storage
                ? "Storage " + ShortId(owner)
                : "Building " + ShortId(owner);
        return new SettlementItemLocationViewModel(LocationId(location), label);
    }

    private static string LocationId(ItemLocation location)
    {
        return IsContainer(location)
            ? location.OwnerId.ToString()
            : string.Empty;
    }

    private static string ShortId(string value)
    {
        return value.Length <= 6 ? value : value.Substring(0, 6);
    }

    private static MaterialRowDefinition Material(
        string id,
        string label,
        params string[] aliases)
    {
        return new MaterialRowDefinition(
            id,
            label,
            new[] { id }.Concat(aliases).ToArray());
    }

    private sealed class MaterialRowDefinition
    {
        public MaterialRowDefinition(string id, string label, IReadOnlyList<string> ids)
        {
            Id = id;
            Label = label;
            Ids = ids;
        }

        public string Id { get; }
        public string Label { get; }
        public IReadOnlyList<string> Ids { get; }
    }
}

}
