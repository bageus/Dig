using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Domain.Buildings
{

public readonly struct BuildingDefinitionId
    : IEquatable<BuildingDefinitionId>, IComparable<BuildingDefinitionId>
{
    private readonly string? _value;

    public BuildingDefinitionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Building definition id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public int CompareTo(BuildingDefinitionId other)
    {
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public bool Equals(BuildingDefinitionId other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is BuildingDefinitionId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);
    }

    public override string ToString()
    {
        return _value ?? string.Empty;
    }

    public static bool operator ==(BuildingDefinitionId left, BuildingDefinitionId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BuildingDefinitionId left, BuildingDefinitionId right)
    {
        return !left.Equals(right);
    }
}

public enum BuildingOrientation
{
    North = 0,
    East = 1,
    South = 2,
    West = 3,
}

public readonly struct CellOffset : IEquatable<CellOffset>, IComparable<CellOffset>
{
    public CellOffset(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }

    public CellOffset Rotate(BuildingOrientation orientation)
    {
        return orientation switch
        {
            BuildingOrientation.North => this,
            BuildingOrientation.East => new CellOffset(-Y, X),
            BuildingOrientation.South => new CellOffset(-X, -Y),
            BuildingOrientation.West => new CellOffset(Y, -X),
            _ => throw new ArgumentOutOfRangeException(nameof(orientation)),
        };
    }

    public CellId Apply(CellId origin)
    {
        return new CellId(checked(origin.X + X), checked(origin.Y + Y));
    }

    public int CompareTo(CellOffset other)
    {
        int yComparison = Y.CompareTo(other.Y);
        return yComparison != 0 ? yComparison : X.CompareTo(other.X);
    }

    public bool Equals(CellOffset other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        return obj is CellOffset other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}

public readonly struct BuildingMaterialRequirement
{
    public BuildingMaterialRequirement(ItemId itemId, int quantity)
    {
        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Material item id cannot be empty.", nameof(itemId));
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

public sealed class BuildingDefinition
{
    private readonly CellOffset[] _footprint;
    private readonly CellOffset[] _workPositions;
    private readonly BuildingMaterialRequirement[] _materials;

    public BuildingDefinition(
        BuildingDefinitionId id,
        string name,
        IEnumerable<CellOffset> footprint,
        IEnumerable<CellOffset> workPositions,
        IEnumerable<BuildingMaterialRequirement> materials,
        int requiredWork,
        int maximumDurability,
        BuildingBoxPolicy? boxPolicy = null)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Building definition id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Building name is required.", nameof(name));
        }

        _footprint = NormalizeOffsets(footprint, nameof(footprint));
        _workPositions = NormalizeOffsets(workPositions, nameof(workPositions));
        _materials = NormalizeMaterials(materials, requireMaterials: boxPolicy is null);
        if (boxPolicy is not null && _materials.Length != 0)
        {
            throw new ArgumentException(
                "A building cannot use box and legacy material construction together.",
                nameof(materials));
        }

        if (requiredWork <= 0 || maximumDurability <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredWork));
        }

        Id = id;
        Name = name.Trim();
        RequiredWork = requiredWork;
        MaximumDurability = maximumDurability;
        BoxPolicy = boxPolicy;
    }

    public BuildingDefinitionId Id { get; }

    public string Name { get; }

    public int RequiredWork { get; }

    public int MaximumDurability { get; }

    public BuildingBoxPolicy? BoxPolicy { get; }

    public BuildingConstructionPolicyKind ConstructionPolicy => BoxPolicy is null
        ? BuildingConstructionPolicyKind.LegacyMaterials
        : BuildingConstructionPolicyKind.BuildingBox;

    public IReadOnlyList<CellOffset> Footprint =>
        new ReadOnlyCollection<CellOffset>(_footprint);

    public IReadOnlyList<CellOffset> WorkPositions =>
        new ReadOnlyCollection<CellOffset>(_workPositions);

    public IReadOnlyList<BuildingMaterialRequirement> Materials =>
        new ReadOnlyCollection<BuildingMaterialRequirement>(_materials);

    public IReadOnlyList<CellId> ResolveFootprint(
        CellId origin,
        BuildingOrientation orientation)
    {
        return new ReadOnlyCollection<CellId>(_footprint
            .Select(offset => offset.Rotate(orientation).Apply(origin))
            .OrderBy(cell => cell)
            .ToArray());
    }

    public IReadOnlyList<CellId> ResolveWorkPositions(
        CellId origin,
        BuildingOrientation orientation)
    {
        return new ReadOnlyCollection<CellId>(_workPositions
            .Select(offset => offset.Rotate(orientation).Apply(origin))
            .OrderBy(cell => cell)
            .ToArray());
    }

    private static CellOffset[] NormalizeOffsets(
        IEnumerable<CellOffset> values,
        string parameterName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        CellOffset[] result = values.Distinct().OrderBy(value => value).ToArray();
        if (result.Length == 0)
        {
            throw new ArgumentException("At least one cell offset is required.", parameterName);
        }

        return result;
    }

    private static BuildingMaterialRequirement[] NormalizeMaterials(
        IEnumerable<BuildingMaterialRequirement> values,
        bool requireMaterials)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        BuildingMaterialRequirement[] result = values
            .GroupBy(value => value.ItemId)
            .Select(group => new BuildingMaterialRequirement(
                group.Key,
                checked(group.Sum(value => value.Quantity))))
            .OrderBy(value => value.ItemId)
            .ToArray();
        if (requireMaterials && result.Length == 0)
        {
            throw new ArgumentException("A legacy building needs at least one material.", nameof(values));
        }

        return result;
    }
}

public sealed class BuildingCatalog
{
    private readonly Dictionary<BuildingDefinitionId, BuildingDefinition> _definitions;

    public BuildingCatalog(IEnumerable<BuildingDefinition> definitions)
    {
        if (definitions is null)
        {
            throw new ArgumentNullException(nameof(definitions));
        }

        BuildingDefinition[] values = definitions.OrderBy(value => value.Id).ToArray();
        if (values.Length == 0
            || values.Select(value => value.Id).Distinct().Count() != values.Length)
        {
            throw new ArgumentException("Building definitions must be non-empty and unique.");
        }

        _definitions = values.ToDictionary(value => value.Id);
    }

    public BuildingDefinition Get(BuildingDefinitionId id)
    {
        return _definitions.TryGetValue(id, out BuildingDefinition? value)
            ? value
            : throw new KeyNotFoundException($"Unknown building definition '{id}'.");
    }
}
}