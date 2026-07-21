using System;
using System.Collections.Generic;
using Dig.Domain.Core;

namespace Dig.Presentation.Notifications
{

public sealed class GameNotificationTicker
{
    private readonly GameNotificationProjector _projector;
    private readonly HashSet<string> _seenSourceKeys =
        new HashSet<string>(StringComparer.Ordinal);
    private readonly List<GameNotification> _active = new List<GameNotification>();

    public GameNotificationTicker(GameNotificationProjector? projector = null)
    {
        _projector = projector ?? new GameNotificationProjector();
    }

    public GameNotification? Current => _active.Count == 0 ? null : _active[0];
    public int ActiveCount => _active.Count;

    public int Ingest(
        IEnumerable<IDomainEvent> events,
        IEnumerable<string>? residentIds = null)
    {
        if (events is null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        HashSet<string>? residents = residentIds == null
            ? null
            : new HashSet<string>(residentIds, StringComparer.Ordinal);
        int added = 0;
        foreach (IDomainEvent domainEvent in events)
        {
            GameNotification? notification = _projector.Project(
                domainEvent,
                residents);
            if (notification == null
                || !_seenSourceKeys.Add(notification.SourceEventKey))
            {
                continue;
            }

            Insert(notification);
            added++;
        }

        return added;
    }

    public GameNotification? DismissCurrent()
    {
        if (_active.Count == 0)
        {
            return null;
        }

        GameNotification dismissed = _active[0];
        dismissed.Dismiss();
        _active.RemoveAt(0);
        return dismissed;
    }

    private void Insert(GameNotification notification)
    {
        int index = _active.FindIndex(existing => Compare(notification, existing) < 0);
        if (index < 0)
        {
            _active.Add(notification);
        }
        else
        {
            _active.Insert(index, notification);
        }
    }

    private static int Compare(GameNotification left, GameNotification right)
    {
        int priority = right.Priority.CompareTo(left.Priority);
        if (priority != 0)
        {
            return priority;
        }

        int tick = left.Tick.CompareTo(right.Tick);
        return tick != 0
            ? tick
            : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
    }
}

}
