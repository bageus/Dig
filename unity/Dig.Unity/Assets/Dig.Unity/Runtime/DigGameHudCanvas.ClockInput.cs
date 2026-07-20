using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private readonly List<RaycastResult> _clockUiHits =
        new List<RaycastResult>();

    internal void RefreshClockInteractionFrame()
    {
        RefreshClockHover();
        RefreshClock();
    }

    private void RefreshClockHover()
    {
        string? roster = ResolveRosterHoveredResident(out bool pointerOverUi);
        string? world = null;
        if (roster == null
            && !pointerOverUi
            && _mainCamera != null
            && _agentRenderer!.TryResolveHoveredAgent(
                _mainCamera,
                Input.mousePosition,
                out string residentId))
        {
            world = residentId;
        }

        if (string.Equals(
                roster,
                _rosterHoveredResidentId,
                StringComparison.Ordinal)
            && string.Equals(
                world,
                _worldHoveredResidentId,
                StringComparison.Ordinal))
        {
            return;
        }

        _rosterHoveredResidentId = roster;
        _worldHoveredResidentId = world;
        InvalidateClock();
    }

    private string? ResolveRosterHoveredResident(out bool pointerOverUi)
    {
        pointerOverUi = false;
        EventSystem? eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return null;
        }

        PointerEventData pointer = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition,
        };
        _clockUiHits.Clear();
        eventSystem.RaycastAll(pointer, _clockUiHits);
        pointerOverUi = _clockUiHits.Count > 0;
        for (int index = 0; index < _clockUiHits.Count; index++)
        {
            Transform? current = _clockUiHits[index].gameObject.transform;
            while (current != null && current != transform)
            {
                const string prefix = "Resident ";
                if (current.name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return current.name.Substring(prefix.Length);
                }

                current = current.parent;
            }
        }

        return null;
    }

    internal void DragScheduleHandle(
        bool adjustStart,
        RectTransform face,
        Vector2 screenPoint,
        Camera? eventCamera,
        bool announce)
    {
        string? selectedId = _agentRenderer!.SelectedCount == 1
            ? _agentRenderer.SelectedAgentId
            : null;
        if (selectedId == null
            || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                face,
                screenPoint,
                eventCamera,
                out Vector2 local)
            || !_simulation!.TryGetResidentWorkWindow(
                selectedId,
                out int ticksPerDay,
                out int start,
                out int end))
        {
            return;
        }

        int hour = ResolveHour(local, ticksPerDay);
        int nextStart = adjustStart ? hour : start;
        int nextEnd = adjustStart ? end : hour;
        if (nextStart == nextEnd)
        {
            if (adjustStart)
            {
                nextStart = WrapHour(nextEnd - 1, ticksPerDay);
            }
            else
            {
                nextEnd = WrapHour(nextStart + 1, ticksPerDay);
            }
        }

        if (nextStart == start && nextEnd == end)
        {
            if (announce)
            {
                SetStatus(
                    $"Work schedule remains {start:00}:00–{end:00}:00; all other time is rest.");
            }

            return;
        }

        Result result = _simulation.SetResidentWorkWindow(
            selectedId,
            nextStart,
            nextEnd);
        if (result.IsFailure)
        {
            SetStatus(result.Error!.Message);
            return;
        }

        InvalidateClock();
        _lastRosterSignature = string.Empty;
        if (announce)
        {
            SetStatus(
                $"Work schedule set to {nextStart:00}:00–{nextEnd:00}:00; all other time is rest.");
        }
    }

    private static int ResolveHour(Vector2 local, int ticksPerDay)
    {
        float angle = Mathf.Atan2(local.x, local.y) * Mathf.Rad2Deg;
        if (angle < 0f)
        {
            angle += 360f;
        }

        return WrapHour(
            Mathf.RoundToInt(angle * ticksPerDay / 360f),
            ticksPerDay);
    }
}

}
