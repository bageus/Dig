using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.Notifications;
using UnityEngine.EventSystems;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private const int NotificationReadBatchSize = 256;
    private readonly GameNotificationTicker _notificationTicker =
        new GameNotificationTicker();
    private long _notificationEventSequence;
    private string? _contextStatusSuffix;

    private void RefreshNotifications()
    {
        IReadOnlyList<JournalEventEntry> entries = _terrainSession!.ReadEventsAfter(
            _notificationEventSequence,
            NotificationReadBatchSize);
        if (entries.Count == 0)
        {
            return;
        }

        IDomainEvent[] events = new IDomainEvent[entries.Count];
        for (int index = 0; index < entries.Count; index++)
        {
            events[index] = entries[index].DomainEvent;
        }

        IEnumerable<string> residentIds = _agentRenderer!
            .GetHudModels()
            .Select(resident => resident.Id);
        _notificationTicker.Ingest(events, residentIds);
        _notificationEventSequence = entries[entries.Count - 1].Sequence;
        RefreshNotificationText();
    }

    private void HandleNotificationClick(PointerEventData eventData)
    {
        bool navigate = eventData.button == PointerEventData.InputButton.Left;
        if (!navigate && eventData.button != PointerEventData.InputButton.Right)
        {
            return;
        }

        GameNotification? notification = _notificationTicker.Current;
        if (notification == null)
        {
            return;
        }

        _notificationTicker.DismissCurrent();
        if (navigate)
        {
            _interaction?.NavigateToNotification(notification.NavigationTarget);
        }

        RefreshNotificationText();
    }

    private void SetContextStatusSuffix(string? suffix)
    {
        _contextStatusSuffix = string.IsNullOrWhiteSpace(suffix)
            ? null
            : suffix.Trim();
        RefreshNotificationText();
    }

    private void RefreshNotificationText()
    {
        if (_statusText == null)
        {
            return;
        }

        GameNotification? notification = _notificationTicker.Current;
        if (notification != null)
        {
            string remaining = _notificationTicker.ActiveCount > 1
                ? $"  [{_notificationTicker.ActiveCount}]"
                : string.Empty;
            _statusText.text = FormatNotification(notification) + remaining;
            return;
        }

        _statusText.text = _contextStatusSuffix == null
            ? _status
            : $"{_status}  ·  {_contextStatusSuffix}";
    }

    private string FormatNotification(GameNotification notification)
    {
        string? sourceId = notification.LocalizationArguments
            .FirstOrDefault(argument => string.Equals(
                argument.Key,
                "source_id",
                StringComparison.Ordinal))
            .Value;
        sourceId ??= notification.NavigationTarget.EntityId ?? "unknown";
        string source = ResolveNotificationSourceName(notification, sourceId);
        return notification.Kind switch
        {
            GameNotificationKind.ResidentAttacked => $"⚔ {source} is under attack",
            GameNotificationKind.ResidentBorn => $"★ {source} was born",
            GameNotificationKind.ResidentHungry => $"! {source} is hungry",
            GameNotificationKind.ResidentOld => $"◷ {source} reached old age",
            GameNotificationKind.ResidentMoodCritical => $"! {source}'s mood is critical",
            GameNotificationKind.ResidentDied => $"† {source} died",
            GameNotificationKind.TechnologyUnlocked => $"◆ Technology unlocked: {source}",
            GameNotificationKind.JobCompleted => $"✓ Job completed: {source}",
            _ => notification.LocalizationKey,
        };
    }

    private string ResolveNotificationSourceName(
        GameNotification notification,
        string sourceId)
    {
        if (notification.NavigationTarget.Kind
                == GameNotificationNavigationKind.Resident
            || notification.Kind == GameNotificationKind.ResidentDied)
        {
            AgentViewModel? resident = _agentRenderer!
                .GetHudModels()
                .FirstOrDefault(model => string.Equals(
                    model.Id,
                    sourceId,
                    StringComparison.Ordinal));
            return resident?.Name ?? ShortIdentifier(sourceId);
        }

        if (notification.NavigationTarget.Kind == GameNotificationNavigationKind.Job)
        {
            return _terrainSession!.LoadJobs()
                .FirstOrDefault(job => string.Equals(
                    job.Id,
                    sourceId,
                    StringComparison.Ordinal))
                ?.Description
                ?? ShortIdentifier(sourceId);
        }

        return sourceId;
    }

    private static string ShortIdentifier(string value)
    {
        const int maximumLength = 12;
        return value.Length <= maximumLength
            ? value
            : value.Substring(0, maximumLength);
    }
}

}
