using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Domain.Ecology
{

public readonly struct MushroomDefinitionId : IEquatable<MushroomDefinitionId>, IComparable<MushroomDefinitionId>
{
    private readonly string? _value;

    public MushroomDefinitionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Mushroom definition id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public int CompareTo(MushroomDefinitionId other) =>
        string.Compare(_value, other._value, StringComparison.Ordinal);

    public bool Equals(MushroomDefinitionId other) =>
        string.Equals(_value, other._value, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
        obj is MushroomDefinitionId other && Equals(other);

    public override int GetHashCode() =>
        StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);

    public override string ToString() => _value ?? string.Empty;

    public static bool operator ==(MushroomDefinitionId left, MushroomDefinitionId right) => left.Equals(right);
    public static bool operator !=(MushroomDefinitionId left, MushroomDefinitionId right) => !left.Equals(right);
}

public enum MushroomStage
{
    AbsentRegrowing = 0,
    Tiny = 1,
    Small = 2,
    Medium = 3,
    Large = 4,
}

public readonly struct MushroomDropProfile
{
    public MushroomDropProfile(int capCount, int legCount)
    {
        if (capCount < 0 || legCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capCount));
        }

        CapCount = capCount;
        LegCount = legCount;
    }

    public int CapCount { get; }
    public int LegCount { get; }
    public int TotalCount => checked(CapCount + LegCount);
}

public sealed class MushroomDefinition
{
    public const int WoodworkingGrantUnits = 80;

    public MushroomDefinition(
        MushroomDefinitionId id,
        long stageDurationTicks,
        ItemId capItemId,
        ItemId legItemId)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Mushroom definition id cannot be empty.", nameof(id));
        }

        if (stageDurationTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stageDurationTicks));
        }

        if (capItemId.IsEmpty || legItemId.IsEmpty)
        {
            throw new ArgumentException("Mushroom material item ids are required.");
        }

        Id = id;
        StageDurationTicks = stageDurationTicks;
        CapItemId = capItemId;
        LegItemId = legItemId;
    }

    public MushroomDefinitionId Id { get; }
    public long StageDurationTicks { get; }
    public ItemId CapItemId { get; }
    public ItemId LegItemId { get; }

    public MushroomDropProfile GetDrops(MushroomStage stage)
    {
        return stage switch
        {
            MushroomStage.Tiny => new MushroomDropProfile(1, 0),
            MushroomStage.Small => new MushroomDropProfile(1, 0),
            MushroomStage.Medium => new MushroomDropProfile(2, 0),
            MushroomStage.Large => new MushroomDropProfile(2, 1),
            MushroomStage.AbsentRegrowing => new MushroomDropProfile(0, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(stage)),
        };
    }

    public static (int Minimum, int Maximum) GetRequiredSwingBand(int woodworkingUnits)
    {
        if (woodworkingUnits < 0 || woodworkingUnits > AgentSkillCatalog.IndividualMaximumUnits)
        {
            throw new ArgumentOutOfRangeException(nameof(woodworkingUnits));
        }

        int points = woodworkingUnits / AgentSkillCatalog.UnitsPerPoint;
        if (points <= 10)
        {
            return (6, 8);
        }

        if (points <= 20)
        {
            return (5, 6);
        }

        if (points <= 40)
        {
            return (3, 5);
        }

        if (points <= 60)
        {
            return (2, 3);
        }

        if (points <= 80)
        {
            return (1, 2);
        }

        return (1, 1);
    }
}

public sealed class MushroomCatalog
{
    private readonly Dictionary<MushroomDefinitionId, MushroomDefinition> _definitions;

    public MushroomCatalog(IEnumerable<MushroomDefinition> definitions)
    {
        if (definitions is null)
        {
            throw new ArgumentNullException(nameof(definitions));
        }

        MushroomDefinition[] ordered = definitions.OrderBy(value => value.Id).ToArray();
        if (ordered.Select(value => value.Id).Distinct().Count() != ordered.Length)
        {
            throw new ArgumentException("Mushroom definition ids must be unique.", nameof(definitions));
        }

        _definitions = ordered.ToDictionary(value => value.Id);
        Definitions = new ReadOnlyCollection<MushroomDefinition>(ordered);
    }

    public IReadOnlyList<MushroomDefinition> Definitions { get; }

    public MushroomDefinition Get(MushroomDefinitionId id)
    {
        return _definitions.TryGetValue(id, out MushroomDefinition? definition)
            ? definition
            : throw new KeyNotFoundException($"Mushroom definition '{id}' was not found.");
    }

    public bool Contains(MushroomDefinitionId id) => _definitions.ContainsKey(id);
}

public sealed class MushroomSiteSnapshot
{
    public MushroomSiteSnapshot(
        EntityId siteId,
        MushroomDefinitionId definitionId,
        CellId cell,
        MushroomStage stage,
        long stageStartedTick,
        long? nextStageTick,
        long growthGeneration,
        EntityId? activeChopJobId,
        EntityId? activeWorkerId,
        int requiredSwings,
        int completedSwings,
        long? growthPausedAtTick,
        long version)
    {
        SiteId = siteId;
        DefinitionId = definitionId;
        Cell = cell;
        Stage = stage;
        StageStartedTick = stageStartedTick;
        NextStageTick = nextStageTick;
        GrowthGeneration = growthGeneration;
        ActiveChopJobId = activeChopJobId;
        ActiveWorkerId = activeWorkerId;
        RequiredSwings = requiredSwings;
        CompletedSwings = completedSwings;
        GrowthPausedAtTick = growthPausedAtTick;
        Version = version;
    }

    public EntityId SiteId { get; }
    public MushroomDefinitionId DefinitionId { get; }
    public CellId Cell { get; }
    public MushroomStage Stage { get; }
    public long StageStartedTick { get; }
    public long? NextStageTick { get; }
    public long GrowthGeneration { get; }
    public EntityId? ActiveChopJobId { get; }
    public EntityId? ActiveWorkerId { get; }
    public int RequiredSwings { get; }
    public int CompletedSwings { get; }
    public long? GrowthPausedAtTick { get; }
    public long Version { get; }
    public bool IsVisible => Stage != MushroomStage.AbsentRegrowing;
    public bool IsChopActive => ActiveChopJobId.HasValue;
}

public sealed class MushroomChopCommit
{
    public MushroomChopCommit(
        EntityId siteId,
        EntityId jobId,
        EntityId workerId,
        CellId cell,
        MushroomStage choppedStage,
        long growthGeneration,
        ItemId capItemId,
        ItemId legItemId,
        MushroomDropProfile drops,
        string skillSourceId)
    {
        SiteId = siteId;
        JobId = jobId;
        WorkerId = workerId;
        Cell = cell;
        ChoppedStage = choppedStage;
        GrowthGeneration = growthGeneration;
        CapItemId = capItemId;
        LegItemId = legItemId;
        Drops = drops;
        SkillSourceId = skillSourceId;
    }

    public EntityId SiteId { get; }
    public EntityId JobId { get; }
    public EntityId WorkerId { get; }
    public CellId Cell { get; }
    public MushroomStage ChoppedStage { get; }
    public long GrowthGeneration { get; }
    public ItemId CapItemId { get; }
    public ItemId LegItemId { get; }
    public MushroomDropProfile Drops { get; }
    public string SkillSourceId { get; }
}

}
