using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Domain.WorldObjects
{

public readonly struct BarrelDefinitionId : IEquatable<BarrelDefinitionId>, IComparable<BarrelDefinitionId>
{
    private readonly string? _value;

    public BarrelDefinitionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Barrel definition id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public int CompareTo(BarrelDefinitionId other) =>
        string.Compare(_value, other._value, StringComparison.Ordinal);

    public bool Equals(BarrelDefinitionId other) =>
        string.Equals(_value, other._value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is BarrelDefinitionId other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);

    public override string ToString() => _value ?? string.Empty;

    public static bool operator ==(BarrelDefinitionId left, BarrelDefinitionId right) => left.Equals(right);
    public static bool operator !=(BarrelDefinitionId left, BarrelDefinitionId right) => !left.Equals(right);
}

public enum BarrelLifecycle
{
    Supported = 0,
    Falling = 1,
    Destroyed = 2,
}

public sealed class BarrelDefinition
{
    private readonly HashSet<ItemId> _contentsPool;

    public BarrelDefinition(BarrelDefinitionId id, IEnumerable<ItemId> contentsPool)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Barrel definition id cannot be empty.", nameof(id));
        }

        if (contentsPool is null)
        {
            throw new ArgumentNullException(nameof(contentsPool));
        }

        ItemId[] items = contentsPool.Distinct().OrderBy(value => value.ToString(), StringComparer.Ordinal).ToArray();
        if (items.Length == 0 || items.Any(value => value.IsEmpty))
        {
            throw new ArgumentException("Barrel contents pool must contain valid item ids.", nameof(contentsPool));
        }

        Id = id;
        ContentsPool = new ReadOnlyCollection<ItemId>(items);
        _contentsPool = new HashSet<ItemId>(items);
    }

    public BarrelDefinitionId Id { get; }
    public IReadOnlyList<ItemId> ContentsPool { get; }

    public bool Supports(ItemId itemId) => _contentsPool.Contains(itemId);
}

public sealed class BarrelCatalog
{
    private readonly Dictionary<BarrelDefinitionId, BarrelDefinition> _definitions;

    public BarrelCatalog(IEnumerable<BarrelDefinition> definitions)
    {
        if (definitions is null)
        {
            throw new ArgumentNullException(nameof(definitions));
        }

        BarrelDefinition[] ordered = definitions.OrderBy(value => value.Id).ToArray();
        if (ordered.Select(value => value.Id).Distinct().Count() != ordered.Length)
        {
            throw new ArgumentException("Barrel definition ids must be unique.", nameof(definitions));
        }

        _definitions = ordered.ToDictionary(value => value.Id);
        Definitions = new ReadOnlyCollection<BarrelDefinition>(ordered);
    }

    public IReadOnlyList<BarrelDefinition> Definitions { get; }

    public bool Contains(BarrelDefinitionId id) => _definitions.ContainsKey(id);

    public BarrelDefinition Get(BarrelDefinitionId id)
    {
        return _definitions.TryGetValue(id, out BarrelDefinition? definition)
            ? definition
            : throw new KeyNotFoundException($"Barrel definition '{id}' was not found.");
    }
}

public sealed class BarrelSnapshot
{
    public BarrelSnapshot(
        EntityId barrelId,
        BarrelDefinitionId definitionId,
        CellId cell,
        BarrelLifecycle lifecycle,
        ItemId contentsItemId,
        long contentsGeneration,
        bool contentsMaterialized,
        CellId? fallSourceCell,
        CellId? fallLandingCell,
        long version)
    {
        BarrelId = barrelId;
        DefinitionId = definitionId;
        Cell = cell;
        Lifecycle = lifecycle;
        ContentsItemId = contentsItemId;
        ContentsGeneration = contentsGeneration;
        ContentsMaterialized = contentsMaterialized;
        FallSourceCell = fallSourceCell;
        FallLandingCell = fallLandingCell;
        Version = version;
    }

    public EntityId BarrelId { get; }
    public BarrelDefinitionId DefinitionId { get; }
    public CellId Cell { get; }
    public BarrelLifecycle Lifecycle { get; }
    public ItemId ContentsItemId { get; }
    public long ContentsGeneration { get; }
    public bool ContentsMaterialized { get; }
    public CellId? FallSourceCell { get; }
    public CellId? FallLandingCell { get; }
    public long Version { get; }
    public bool IsAttackable => Lifecycle == BarrelLifecycle.Supported && !ContentsMaterialized;
    public bool BlocksBuildingPlacement => Lifecycle == BarrelLifecycle.Supported;
}

public sealed class BarrelDestructionCommit
{
    public BarrelDestructionCommit(
        EntityId barrelId,
        EntityId jobId,
        EntityId workerId,
        CellId cell,
        ItemId contentsItemId,
        long contentsGeneration,
        long version)
    {
        BarrelId = barrelId;
        JobId = jobId;
        WorkerId = workerId;
        Cell = cell;
        ContentsItemId = contentsItemId;
        ContentsGeneration = contentsGeneration;
        Version = version;
    }

    public EntityId BarrelId { get; }
    public EntityId JobId { get; }
    public EntityId WorkerId { get; }
    public CellId Cell { get; }
    public ItemId ContentsItemId { get; }
    public long ContentsGeneration { get; }
    public long Version { get; }
}

}