using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Presentation.Notifications
{

public enum GameNotificationKind
{
    ResidentAttacked = 0,
    ResidentBorn = 1,
    ResidentHungry = 2,
    ResidentOld = 3,
    ResidentMoodCritical = 4,
    ResidentDied = 5,
    TechnologyUnlocked = 6,
    JobCompleted = 7,
}

public enum GameNotificationNavigationKind
{
    None = 0,
    Resident = 1,
    Job = 2,
    Building = 3,
    Cell = 4,
    Technology = 5,
}

public readonly struct GameNotificationArgument
{
    public GameNotificationArgument(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Argument key is required.", nameof(key));
        }

        Key = key.Trim();
        Value = value ?? string.Empty;
    }

    public string Key { get; }
    public string Value { get; }
}

public readonly struct GameNotificationNavigationTarget
{
    public GameNotificationNavigationTarget(
        GameNotificationNavigationKind kind,
        string? entityId = null,
        CellId? cell = null)
    {
        if (!Enum.IsDefined(typeof(GameNotificationNavigationKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (kind == GameNotificationNavigationKind.None
            && (!string.IsNullOrWhiteSpace(entityId) || cell.HasValue))
        {
            throw new ArgumentException("An empty navigation target cannot contain a source.");
        }

        if (kind != GameNotificationNavigationKind.None
            && kind != GameNotificationNavigationKind.Cell
            && string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Navigation entity id is required.", nameof(entityId));
        }

        if (kind == GameNotificationNavigationKind.Cell && !cell.HasValue)
        {
            throw new ArgumentException("Navigation cell is required.", nameof(cell));
        }

        Kind = kind;
        EntityId = string.IsNullOrWhiteSpace(entityId) ? null : entityId.Trim();
        Cell = cell;
    }

    public GameNotificationNavigationKind Kind { get; }
    public string? EntityId { get; }
    public CellId? Cell { get; }

    public static GameNotificationNavigationTarget None =>
        new GameNotificationNavigationTarget(GameNotificationNavigationKind.None);
}

public sealed class GameNotification
{
    public GameNotification(
        string id,
        GameNotificationKind kind,
        string sourceEventKey,
        long tick,
        int priority,
        string localizationKey,
        IEnumerable<GameNotificationArgument> localizationArguments,
        GameNotificationNavigationTarget navigationTarget)
    {
        if (string.IsNullOrWhiteSpace(id)
            || string.IsNullOrWhiteSpace(sourceEventKey)
            || string.IsNullOrWhiteSpace(localizationKey))
        {
            throw new ArgumentException("Notification identity and localization are required.");
        }

        if (!Enum.IsDefined(typeof(GameNotificationKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (tick < 0 || priority < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        Id = id.Trim();
        Kind = kind;
        SourceEventKey = sourceEventKey.Trim();
        Tick = tick;
        Priority = priority;
        LocalizationKey = localizationKey.Trim();
        LocalizationArguments = new ReadOnlyCollection<GameNotificationArgument>(
            (localizationArguments ?? throw new ArgumentNullException(
                nameof(localizationArguments))).ToArray());
        NavigationTarget = navigationTarget;
        IsActive = true;
    }

    public string Id { get; }
    public GameNotificationKind Kind { get; }
    public string SourceEventKey { get; }
    public long Tick { get; }
    public int Priority { get; }
    public string LocalizationKey { get; }
    public IReadOnlyList<GameNotificationArgument> LocalizationArguments { get; }
    public GameNotificationNavigationTarget NavigationTarget { get; }
    public bool IsActive { get; private set; }

    public void Dismiss()
    {
        IsActive = false;
    }
}

}
