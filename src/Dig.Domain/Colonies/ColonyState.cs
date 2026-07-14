using System;
using Dig.Domain.Core;

namespace Dig.Domain.Colonies
{

public sealed class ColonyState : AggregateRoot
{
    private const int MaximumNameLength = 80;

    private ColonyState(EntityId id, string name)
    {
        Id = id;
        Name = name;
    }

    public EntityId Id { get; }

    public string Name { get; private set; }

    public long Version { get; private set; }

    public static Result<ColonyState> Create(EntityId id, string name)
    {
        if (id.IsEmpty)
        {
            return Result<ColonyState>.Failure(ColonyErrors.EmptyId);
        }

        Result<string> validation = ValidateName(name);
        if (validation.IsFailure)
        {
            return Result<ColonyState>.Failure(validation.Error!);
        }

        return Result<ColonyState>.Success(new ColonyState(id, validation.Value));
    }

    public Result Rename(string newName, long tick)
    {
        if (tick < 0)
        {
            return Result.Failure(ColonyErrors.NegativeTick);
        }

        Result<string> validation = ValidateName(newName);
        if (validation.IsFailure)
        {
            return Result.Failure(validation.Error!);
        }

        string normalizedName = validation.Value;
        if (string.Equals(Name, normalizedName, StringComparison.Ordinal))
        {
            return Result.Success();
        }

        string previousName = Name;
        Name = normalizedName;
        Version++;

        Raise(new ColonyRenamed(
            Id,
            previousName,
            Name,
            Version,
            tick));

        return Result.Success();
    }

    private static Result<string> ValidateName(string? name)
    {
        string normalizedName = name?.Trim() ?? string.Empty;

        if (normalizedName.Length == 0)
        {
            return Result<string>.Failure(ColonyErrors.EmptyName);
        }

        if (normalizedName.Length > MaximumNameLength)
        {
            return Result<string>.Failure(ColonyErrors.NameTooLong);
        }

        return Result<string>.Success(normalizedName);
    }
}

public sealed class ColonyRenamed : IDomainEvent
{
    public ColonyRenamed(
        EntityId colonyId,
        string previousName,
        string currentName,
        long colonyVersion,
        long tick)
    {
        ColonyId = colonyId;
        PreviousName = previousName;
        CurrentName = currentName;
        ColonyVersion = colonyVersion;
        Tick = tick;
    }

    public EntityId ColonyId { get; }

    public string PreviousName { get; }

    public string CurrentName { get; }

    public long ColonyVersion { get; }

    public long Tick { get; }
}

public static class ColonyErrors
{
    public static readonly DomainError EmptyId = new DomainError(
        "colony.empty_id",
        "Colony id cannot be empty.");

    public static readonly DomainError EmptyName = new DomainError(
        "colony.empty_name",
        "Colony name cannot be empty.");

    public static readonly DomainError NameTooLong = new DomainError(
        "colony.name_too_long",
        "Colony name cannot exceed 80 characters.");

    public static readonly DomainError NegativeTick = new DomainError(
        "simulation.negative_tick",
        "Simulation tick cannot be negative.");

    public static readonly DomainError NotFound = new DomainError(
        "colony.not_found",
        "Colony was not found.");
}
}
