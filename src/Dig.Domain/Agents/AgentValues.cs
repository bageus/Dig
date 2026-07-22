using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Domain.Core;

namespace Dig.Domain.Agents
{

public readonly struct AgentSkillId : IEquatable<AgentSkillId>, IComparable<AgentSkillId>
{
    private readonly string? _value;

    public AgentSkillId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Skill id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public int CompareTo(AgentSkillId other)
    {
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public bool Equals(AgentSkillId other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is AgentSkillId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);
    }

    public override string ToString()
    {
        return _value ?? string.Empty;
    }

    public static bool operator ==(AgentSkillId left, AgentSkillId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AgentSkillId left, AgentSkillId right)
    {
        return !left.Equals(right);
    }
}

public readonly struct AgentTraitId : IEquatable<AgentTraitId>, IComparable<AgentTraitId>
{
    private readonly string? _value;

    public AgentTraitId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Trait id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public int CompareTo(AgentTraitId other)
    {
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public bool Equals(AgentTraitId other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is AgentTraitId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);
    }

    public override string ToString()
    {
        return _value ?? string.Empty;
    }

    public static bool operator ==(AgentTraitId left, AgentTraitId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AgentTraitId left, AgentTraitId right)
    {
        return !left.Equals(right);
    }
}

public readonly struct AgentSkillValue
{
    public AgentSkillValue(AgentSkillId id, int level)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Skill id cannot be empty.", nameof(id));
        }

        if (level < 0 || level > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        Id = id;
        Level = level;
    }

    public AgentSkillId Id { get; }

    public int Level { get; }
}

internal sealed class AgentTraitSet
{
    private readonly HashSet<AgentTraitId> _traits;
    private readonly IReadOnlyList<AgentTraitId> _snapshot;

    public AgentTraitSet(IEnumerable<AgentTraitId> traits)
    {
        if (traits is null)
        {
            throw new ArgumentNullException(nameof(traits));
        }

        _traits = new HashSet<AgentTraitId>();
        foreach (AgentTraitId trait in traits)
        {
            if (trait.IsEmpty)
            {
                throw new ArgumentException("Trait ids cannot be empty.", nameof(traits));
            }

            if (!_traits.Add(trait))
            {
                throw new ArgumentException(
                    $"Duplicate trait id '{trait}'.",
                    nameof(traits));
            }
        }

        AgentTraitId[] ordered = _traits.OrderBy(trait => trait).ToArray();
        _snapshot = new ReadOnlyCollection<AgentTraitId>(ordered);
    }

    public bool Contains(AgentTraitId id)
    {
        return _traits.Contains(id);
    }

    public IReadOnlyList<AgentTraitId> CreateSnapshot()
    {
        return _snapshot;
    }
}
}
