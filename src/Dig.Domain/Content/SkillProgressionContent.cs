using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Agents;

namespace Dig.Domain.Content
{

public readonly struct SkillGrantProfileId
    : IEquatable<SkillGrantProfileId>, IComparable<SkillGrantProfileId>
{
    private readonly string? _value;

    public SkillGrantProfileId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Skill grant profile id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public int CompareTo(SkillGrantProfileId other)
    {
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public bool Equals(SkillGrantProfileId other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is SkillGrantProfileId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);
    }

    public override string ToString()
    {
        return _value ?? string.Empty;
    }

    public static bool operator ==(SkillGrantProfileId left, SkillGrantProfileId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SkillGrantProfileId left, SkillGrantProfileId right)
    {
        return !left.Equals(right);
    }
}

public sealed class SkillGrantProfileDefinition
{
    public SkillGrantProfileDefinition(
        SkillGrantProfileId id,
        IEnumerable<SkillGrant> grants)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Skill grant profile id cannot be empty.", nameof(id));
        }

        Id = id;
        Profile = new SkillGrantProfile(grants);
    }

    public SkillGrantProfileId Id { get; }
    public SkillGrantProfile Profile { get; }
}

public sealed class SkillProgressionContentCatalog
{
    private readonly IReadOnlyDictionary<SkillGrantProfileId, SkillGrantProfileDefinition>
        _profiles;

    private SkillProgressionContentCatalog(
        IReadOnlyCollection<SkillGrantProfileDefinition> profiles)
    {
        SkillGrantProfileDefinition[] ordered = profiles
            .OrderBy(value => value.Id)
            .ToArray();
        _profiles = new ReadOnlyDictionary<SkillGrantProfileId, SkillGrantProfileDefinition>(
            ordered.ToDictionary(value => value.Id));
        Profiles = new ReadOnlyCollection<SkillGrantProfileDefinition>(ordered);
    }

    public IReadOnlyList<SkillGrantProfileDefinition> Profiles { get; }

    public SkillGrantProfile GetProfile(SkillGrantProfileId id)
    {
        return _profiles.TryGetValue(id, out SkillGrantProfileDefinition? definition)
            ? definition.Profile
            : throw new KeyNotFoundException($"Unknown skill grant profile '{id}'.");
    }

    public static SkillProgressionContentValidationResult ValidateAndCreate(
        IEnumerable<SkillGrantProfileDefinition> profiles)
    {
        if (profiles is null)
        {
            throw new ArgumentNullException(nameof(profiles));
        }

        SkillGrantProfileDefinition[] supplied = profiles.ToArray();
        SkillGrantProfileDefinition[] values = supplied
            .Where(value => value is not null)
            .ToArray();
        List<ContentValidationIssue> issues = values
            .GroupBy(value => value.Id)
            .Where(group => group.Count() > 1)
            .Select(group => new ContentValidationIssue(
                "content.duplicate_skill_profile",
                $"skill-profiles/{group.Key}",
                "Skill grant profile ids must be unique."))
            .ToList();
        if (supplied.Any(value => value is null))
        {
            issues.Add(new ContentValidationIssue(
                "content.missing_skill_profile",
                "skill-profiles",
                "Skill grant profile definitions cannot be null."));
        }

        return issues.Count == 0
            ? new SkillProgressionContentValidationResult(
                new SkillProgressionContentCatalog(values),
                Array.Empty<ContentValidationIssue>())
            : new SkillProgressionContentValidationResult(null, issues);
    }
}

public sealed class SkillProgressionContentValidationResult
{
    public SkillProgressionContentValidationResult(
        SkillProgressionContentCatalog? catalog,
        IEnumerable<ContentValidationIssue> issues)
    {
        Catalog = catalog;
        Issues = new ReadOnlyCollection<ContentValidationIssue>(
            (issues ?? throw new ArgumentNullException(nameof(issues))).ToArray());
    }

    public SkillProgressionContentCatalog? Catalog { get; }
    public IReadOnlyList<ContentValidationIssue> Issues { get; }
    public bool IsValid => Catalog is not null && Issues.Count == 0;
}

}
