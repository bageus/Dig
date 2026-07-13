using System.Collections.ObjectModel;

namespace Dig.Domain.World;

public readonly struct MaterialId : IEquatable<MaterialId>, IComparable<MaterialId>
{
    private readonly string? _value;

    public MaterialId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Material id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public int CompareTo(MaterialId other)
    {
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public bool Equals(MaterialId other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is MaterialId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);
    }

    public override string ToString()
    {
        return _value ?? string.Empty;
    }

    public static bool operator ==(MaterialId left, MaterialId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MaterialId left, MaterialId right)
    {
        return !left.Equals(right);
    }
}

public sealed class MaterialDefinition
{
    public MaterialDefinition(
        MaterialId id,
        bool isSolid,
        int hardness)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Material id cannot be empty.", nameof(id));
        }

        if (hardness < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hardness));
        }

        if (!isSolid && hardness != 0)
        {
            throw new ArgumentException(
                "Non-solid materials must have zero hardness.",
                nameof(hardness));
        }

        Id = id;
        IsSolid = isSolid;
        Hardness = hardness;
    }

    public MaterialId Id { get; }

    public bool IsSolid { get; }

    public int Hardness { get; }
}

public sealed class MaterialCatalog
{
    private readonly Dictionary<MaterialId, MaterialDefinition> _definitions;
    private readonly IReadOnlyList<MaterialDefinition> _orderedDefinitions;

    public MaterialCatalog(IEnumerable<MaterialDefinition> definitions)
    {
        if (definitions is null)
        {
            throw new ArgumentNullException(nameof(definitions));
        }

        _definitions = new Dictionary<MaterialId, MaterialDefinition>();
        foreach (MaterialDefinition definition in definitions)
        {
            if (definition is null)
            {
                throw new ArgumentException(
                    "Material definitions cannot contain null values.",
                    nameof(definitions));
            }

            if (!_definitions.TryAdd(definition.Id, definition))
            {
                throw new ArgumentException(
                    $"Duplicate material id '{definition.Id}'.",
                    nameof(definitions));
            }
        }

        if (_definitions.Count == 0)
        {
            throw new ArgumentException(
                "At least one material definition is required.",
                nameof(definitions));
        }

        MaterialDefinition[] ordered = _definitions.Values
            .OrderBy(definition => definition.Id)
            .ToArray();
        _orderedDefinitions = new ReadOnlyCollection<MaterialDefinition>(ordered);
    }

    public IReadOnlyList<MaterialDefinition> Definitions => _orderedDefinitions;

    public bool Contains(MaterialId materialId)
    {
        return _definitions.ContainsKey(materialId);
    }

    public MaterialDefinition? Get(MaterialId materialId)
    {
        return _definitions.TryGetValue(materialId, out MaterialDefinition? definition)
            ? definition
            : null;
    }
}
