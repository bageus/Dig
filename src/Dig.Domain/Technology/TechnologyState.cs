using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Domain.Content;
using Dig.Domain.Core;

namespace Dig.Domain.Technology
{

public static class TechnologyErrors
{
    public static readonly DomainError AlreadyUnlocked = new DomainError(
        "technology.already_unlocked",
        "The technology is already unlocked.");

    public static readonly DomainError PrerequisitesMissing = new DomainError(
        "technology.prerequisites_missing",
        "One or more prerequisite technologies are not unlocked.");
}

public sealed class TechnologyUnlocked : IDomainEvent
{
    public TechnologyUnlocked(long tick, TechnologyId technologyId)
    {
        Tick = tick;
        TechnologyId = technologyId;
    }

    public long Tick { get; }

    public TechnologyId TechnologyId { get; }
}

public sealed class TechnologySnapshot
{
    public TechnologySnapshot(
        long version,
        IReadOnlyCollection<TechnologyId> unlockedTechnologies)
    {
        Version = version;
        UnlockedTechnologies = new ReadOnlyCollection<TechnologyId>(
            unlockedTechnologies.OrderBy(value => value).ToArray());
    }

    public long Version { get; }

    public IReadOnlyList<TechnologyId> UnlockedTechnologies { get; }
}

public sealed class TechnologyState : AggregateRoot
{
    private readonly HashSet<TechnologyId> _unlocked = new HashSet<TechnologyId>();

    public long Version { get; private set; }

    public bool IsUnlocked(TechnologyId technologyId)
    {
        return _unlocked.Contains(technologyId);
    }

    public bool IsRecipeUnlocked(RecipeDefinition recipe)
    {
        if (recipe is null)
        {
            throw new ArgumentNullException(nameof(recipe));
        }

        return !recipe.RequiredTechnologyId.HasValue
            || IsUnlocked(recipe.RequiredTechnologyId.Value);
    }

    public Result CanUnlock(TechnologyDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (IsUnlocked(definition.Id))
        {
            return Result.Failure(TechnologyErrors.AlreadyUnlocked);
        }

        return definition.Prerequisites.All(IsUnlocked)
            ? Result.Success()
            : Result.Failure(TechnologyErrors.PrerequisitesMissing);
    }

    public Result Unlock(TechnologyDefinition definition, long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        Result allowed = CanUnlock(definition);
        if (allowed.IsFailure)
        {
            return allowed;
        }

        _unlocked.Add(definition.Id);
        Version = checked(Version + 1);
        Raise(new TechnologyUnlocked(tick, definition.Id));
        return Result.Success();
    }

    public TechnologySnapshot CreateSnapshot()
    {
        return new TechnologySnapshot(Version, _unlocked);
    }
}
}
