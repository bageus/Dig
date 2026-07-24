using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Inventory;

namespace Dig.Domain.Content
{

public sealed class BuildingBoxWorkProfile
{
    public BuildingBoxWorkProfile(
        int assemblyIterations,
        int packingIterations,
        decimal baseMinutesPerIteration,
        decimal logisticsPerCompletedIteration)
    {
        if (assemblyIterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(assemblyIterations));
        }

        if (packingIterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(packingIterations));
        }

        if (baseMinutesPerIteration <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(baseMinutesPerIteration));
        }

        if (logisticsPerCompletedIteration < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(logisticsPerCompletedIteration));
        }

        AssemblyIterations = assemblyIterations;
        PackingIterations = packingIterations;
        BaseMinutesPerIteration = baseMinutesPerIteration;
        LogisticsPerCompletedIteration = logisticsPerCompletedIteration;
    }

    public int AssemblyIterations { get; }

    public int PackingIterations { get; }

    public decimal BaseMinutesPerIteration { get; }

    public decimal LogisticsPerCompletedIteration { get; }

    public decimal TotalAssemblyLogistics =>
        AssemblyIterations * LogisticsPerCompletedIteration;

    public decimal TotalPackingLogistics =>
        PackingIterations * LogisticsPerCompletedIteration;
}

public sealed class PackableBuildingPlacementProfile
{
    public PackableBuildingPlacementProfile(
        decimal widthCells,
        decimal depthCells,
        bool requiresFlatSurface,
        bool outdoorOnly,
        bool allowsTunnel)
    {
        if (widthCells <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(widthCells));
        }

        if (depthCells <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(depthCells));
        }

        if (outdoorOnly && allowsTunnel)
        {
            throw new ArgumentException(
                "An outdoor-only building cannot allow tunnel placement.");
        }

        WidthCells = widthCells;
        DepthCells = depthCells;
        RequiresFlatSurface = requiresFlatSurface;
        OutdoorOnly = outdoorOnly;
        AllowsTunnel = allowsTunnel;
    }

    public decimal WidthCells { get; }

    public decimal DepthCells { get; }

    public bool RequiresFlatSurface { get; }

    public bool OutdoorOnly { get; }

    public bool AllowsTunnel { get; }
}

public sealed class PackableBuildingContentDefinition
{
    public PackableBuildingContentDefinition(
        BuildingDefinition building,
        ItemDefinition boxItem,
        BuildingBoxWorkProfile work,
        PackableBuildingPlacementProfile placement)
    {
        Building = building ?? throw new ArgumentNullException(nameof(building));
        BoxItem = boxItem ?? throw new ArgumentNullException(nameof(boxItem));
        Work = work ?? throw new ArgumentNullException(nameof(work));
        Placement = placement ?? throw new ArgumentNullException(nameof(placement));

        BuildingBoxPolicy? boxPolicy = building.BoxPolicy;
        if (boxPolicy is null || boxPolicy.BoxItemId != boxItem.Id)
        {
            throw new ArgumentException(
                "The building box policy must reference the supplied box item.");
        }

        if (building.RequiredWork != work.AssemblyIterations
            || boxPolicy.PackingWork != work.PackingIterations)
        {
            throw new ArgumentException(
                "Building work values must match the discrete iteration profile.");
        }

        if (boxItem.MaximumStackSize != 1)
        {
            throw new ArgumentException("Building boxes must be non-stackable.");
        }
    }

    public BuildingDefinition Building { get; }

    public ItemDefinition BoxItem { get; }

    public BuildingBoxWorkProfile Work { get; }

    public PackableBuildingPlacementProfile Placement { get; }
}

public sealed class PackableBuildingContentCatalog
{
    private readonly Dictionary<BuildingDefinitionId, PackableBuildingContentDefinition>
        _byBuilding;
    private readonly Dictionary<ItemId, PackableBuildingContentDefinition> _byBoxItem;

    public PackableBuildingContentCatalog(
        IEnumerable<PackableBuildingContentDefinition> definitions)
    {
        if (definitions is null)
        {
            throw new ArgumentNullException(nameof(definitions));
        }

        PackableBuildingContentDefinition[] values = definitions
            .OrderBy(value => value.Building.Id)
            .ToArray();
        if (values.Length == 0)
        {
            throw new ArgumentException("At least one packable building is required.");
        }

        if (values.Select(value => value.Building.Id).Distinct().Count() != values.Length
            || values.Select(value => value.BoxItem.Id).Distinct().Count() != values.Length)
        {
            throw new ArgumentException(
                "Packable building and box item ids must be unique.");
        }

        _byBuilding = values.ToDictionary(value => value.Building.Id);
        _byBoxItem = values.ToDictionary(value => value.BoxItem.Id);
        Definitions = new ReadOnlyCollection<PackableBuildingContentDefinition>(values);
    }

    public IReadOnlyList<PackableBuildingContentDefinition> Definitions { get; }

    public bool TryGet(
        BuildingDefinitionId id,
        out PackableBuildingContentDefinition? definition)
    {
        return _byBuilding.TryGetValue(id, out definition);
    }

    public PackableBuildingContentDefinition Get(BuildingDefinitionId id)
    {
        return TryGet(id, out PackableBuildingContentDefinition? value)
            ? value!
            : throw new KeyNotFoundException($"Unknown packable building '{id}'.");
    }

    public PackableBuildingContentDefinition GetByBoxItem(ItemId id)
    {
        return _byBoxItem.TryGetValue(id, out PackableBuildingContentDefinition? value)
            ? value
            : throw new KeyNotFoundException($"Unknown building box item '{id}'.");
    }
}

}