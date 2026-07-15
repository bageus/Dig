using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Factions
{

public readonly struct FactionId : IEquatable<FactionId>, IComparable<FactionId>
{
    private readonly string? _value;

    public FactionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Faction id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public int CompareTo(FactionId other)
    {
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public bool Equals(FactionId other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is FactionId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);
    }

    public override string ToString()
    {
        return _value ?? string.Empty;
    }

    public static bool operator ==(FactionId left, FactionId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FactionId left, FactionId right)
    {
        return !left.Equals(right);
    }
}

public enum FactionRelationKind
{
    Hostile = 0,
    Neutral = 1,
    Friendly = 2,
    Allied = 3,
}

public sealed class FactionDefinition
{
    public FactionDefinition(FactionId id, string displayName, int initialRelationScore)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Faction id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Faction display name is required.", nameof(displayName));
        }

        ValidateRelationScore(initialRelationScore, nameof(initialRelationScore));
        Id = id;
        DisplayName = displayName.Trim();
        InitialRelationScore = initialRelationScore;
    }

    public FactionId Id { get; }
    public string DisplayName { get; }
    public int InitialRelationScore { get; }

    private static void ValidateRelationScore(int score, string parameterName)
    {
        if (score < -10_000 || score > 10_000)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

public sealed class FactionCatalog
{
    private readonly Dictionary<FactionId, FactionDefinition> _definitions;

    public FactionCatalog(IEnumerable<FactionDefinition> definitions)
    {
        if (definitions is null)
        {
            throw new ArgumentNullException(nameof(definitions));
        }

        FactionDefinition[] values = definitions.OrderBy(item => item.Id).ToArray();
        if (values.Length == 0)
        {
            throw new ArgumentException("Faction catalog cannot be empty.", nameof(definitions));
        }

        if (values.Select(item => item.Id).Distinct().Count() != values.Length)
        {
            throw new ArgumentException("Faction ids must be unique.", nameof(definitions));
        }

        _definitions = values.ToDictionary(item => item.Id);
        Definitions = new ReadOnlyCollection<FactionDefinition>(values);
    }

    public IReadOnlyList<FactionDefinition> Definitions { get; }

    public bool Contains(FactionId factionId)
    {
        return _definitions.ContainsKey(factionId);
    }

    public FactionDefinition Get(FactionId factionId)
    {
        return _definitions.TryGetValue(factionId, out FactionDefinition? definition)
            ? definition
            : throw new KeyNotFoundException($"Faction '{factionId}' is not registered.");
    }
}

public sealed class FactionDiplomacyPolicy
{
    public FactionDiplomacyPolicy(
        int hostileThreshold,
        int friendlyThreshold,
        int alliedThreshold,
        int territoryViolationPenalty)
    {
        ValidateScore(hostileThreshold, nameof(hostileThreshold));
        ValidateScore(friendlyThreshold, nameof(friendlyThreshold));
        ValidateScore(alliedThreshold, nameof(alliedThreshold));
        if (hostileThreshold >= friendlyThreshold || friendlyThreshold >= alliedThreshold)
        {
            throw new ArgumentException("Diplomatic thresholds must increase strictly.");
        }

        if (territoryViolationPenalty <= 0 || territoryViolationPenalty > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(territoryViolationPenalty));
        }

        HostileThreshold = hostileThreshold;
        FriendlyThreshold = friendlyThreshold;
        AlliedThreshold = alliedThreshold;
        TerritoryViolationPenalty = territoryViolationPenalty;
    }

    public int HostileThreshold { get; }
    public int FriendlyThreshold { get; }
    public int AlliedThreshold { get; }
    public int TerritoryViolationPenalty { get; }

    public FactionRelationKind Resolve(int score)
    {
        ValidateScore(score, nameof(score));
        if (score <= HostileThreshold)
        {
            return FactionRelationKind.Hostile;
        }

        if (score >= AlliedThreshold)
        {
            return FactionRelationKind.Allied;
        }

        return score >= FriendlyThreshold
            ? FactionRelationKind.Friendly
            : FactionRelationKind.Neutral;
    }

    private static void ValidateScore(int score, string parameterName)
    {
        if (score < -10_000 || score > 10_000)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

public readonly struct FactionRelationSnapshot
{
    public FactionRelationSnapshot(
        FactionId first,
        FactionId second,
        int score,
        FactionRelationKind kind)
    {
        First = first;
        Second = second;
        Score = score;
        Kind = kind;
    }

    public FactionId First { get; }
    public FactionId Second { get; }
    public int Score { get; }
    public FactionRelationKind Kind { get; }
}

public readonly struct TerritorySnapshot
{
    public TerritorySnapshot(CellId cellId, FactionId owner)
    {
        CellId = cellId;
        Owner = owner;
    }

    public CellId CellId { get; }
    public FactionId Owner { get; }
}

internal readonly struct FactionPair : IEquatable<FactionPair>, IComparable<FactionPair>
{
    public FactionPair(FactionId left, FactionId right)
    {
        if (left.IsEmpty || right.IsEmpty || left == right)
        {
            throw new ArgumentException("Faction pair requires two distinct factions.");
        }

        bool leftFirst = left.CompareTo(right) < 0;
        First = leftFirst ? left : right;
        Second = leftFirst ? right : left;
    }

    public FactionId First { get; }
    public FactionId Second { get; }

    public int CompareTo(FactionPair other)
    {
        int first = First.CompareTo(other.First);
        return first != 0 ? first : Second.CompareTo(other.Second);
    }

    public bool Equals(FactionPair other)
    {
        return First == other.First && Second == other.Second;
    }

    public override bool Equals(object? obj)
    {
        return obj is FactionPair other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(First, Second);
    }
}
}
