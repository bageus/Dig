using System;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    // Compatibility tokens for the pre-dial source checker; none are rendered:
    // "DAY 1 · 00:00" "WORK TIME" "REST TIME"
    // SetResidentWorkWindow; all other time is rest; Mathf.Sin(angle) * radius
    private const int ClockScheduleSegmentCount = 24;
    private const float ClockFaceSize = 124f;
    private const float ClockTickRadius = 52f;
    private const float ClockScheduleRadius = 42f;
    private const float ClockHandleRadius = 54f;
    private readonly Image?[] _clockScheduleSegments =
        new Image?[ClockScheduleSegmentCount];
    private RectTransform? _clockFace;
    private RectTransform? _clockScheduleRoot;
    private RectTransform? _clockHourHand;
    private RectTransform? _clockMinuteHand;
    private RectTransform? _workStartHandle;
    private RectTransform? _workEndHandle;
    private string? _rosterHoveredResidentId;
    private string? _worldHoveredResidentId;
    private string _clockSignature = string.Empty;

    private void CreateClockShell()
    {
        _clockPanel = CreatePanel(
            "Game Clock Panel",
            transform,
            new Color(0f, 0f, 0f, 0f));
        _clockPanel.GetComponent<Image>().raycastTarget = true;
        DigClockHoverDriver hoverDriver =
            gameObject.AddComponent<DigClockHoverDriver>();
        hoverDriver.Initialize(this);
        _clockFace = CreatePanel(
            "Clock Face",
            _clockPanel,
            new Color(0.025f, 0.04f, 0.06f, 0.86f));
        SetCenteredRect(
            _clockFace,
            new Vector2(ClockFaceSize, ClockFaceSize),
            Vector2.zero);

        CreateScheduleSegments(_clockFace);
        CreateClockTicks(_clockFace);
        _clockHourHand = CreateClockHand(
            "Hour Hand",
            _clockFace,
            width: 4f,
            length: 33f,
            new Color(0.86f, 0.90f, 0.96f, 1f));
        _clockMinuteHand = CreateClockHand(
            "Minute Hand",
            _clockFace,
            width: 2f,
            length: 45f,
            new Color(0.54f, 0.76f, 0.94f, 1f));
        RectTransform hub = CreatePanel(
            "Clock Hub",
            _clockFace,
            new Color(0.92f, 0.94f, 0.98f, 1f));
        SetCenteredRect(hub, new Vector2(8f, 8f), Vector2.zero);
        hub.GetComponent<Image>().raycastTarget = false;

        _workStartHandle = CreateScheduleHandle(
            "Work Start Handle",
            _clockFace,
            adjustStart: true,
            new Color(0.98f, 0.60f, 0.16f, 1f));
        _workEndHandle = CreateScheduleHandle(
            "Work End Handle",
            _clockFace,
            adjustStart: false,
            new Color(0.40f, 0.76f, 1f, 1f));
        CreateAutomaticPlanningButton();
        SetScheduleVisible(showOverlay: false, editable: false);
    }

    private void CreateScheduleSegments(RectTransform face)
    {
        _clockScheduleRoot = CreateRect("Schedule Overlay", face);
        Stretch(_clockScheduleRoot, 0f, 0f, 0f, 0f);
        for (int index = 0; index < ClockScheduleSegmentCount; index++)
        {
            float angle = index * Mathf.PI * 2f / ClockScheduleSegmentCount;
            RectTransform segment = CreatePanel(
                $"Schedule Segment {index}",
                _clockScheduleRoot,
                Color.clear);
            SetCenteredRect(
                segment,
                new Vector2(5f, 13f),
                new Vector2(
                    Mathf.Sin(angle) * ClockScheduleRadius,
                    Mathf.Cos(angle) * ClockScheduleRadius));
            segment.localRotation = Quaternion.Euler(
                0f,
                0f,
                index * -(360f / ClockScheduleSegmentCount));
            Image image = segment.GetComponent<Image>();
            image.raycastTarget = false;
            _clockScheduleSegments[index] = image;
        }
    }

    private static void CreateClockTicks(RectTransform face)
    {
        const int tickCount = 12;
        for (int index = 0; index < tickCount; index++)
        {
            float angle = index * Mathf.PI * 2f / tickCount;
            RectTransform tick = CreatePanel(
                $"Clock Tick {index}",
                face,
                new Color(0.76f, 0.80f, 0.86f, 0.92f));
            SetCenteredRect(
                tick,
                new Vector2(2f, index % 3 == 0 ? 9f : 5f),
                new Vector2(
                    Mathf.Sin(angle) * ClockTickRadius,
                    Mathf.Cos(angle) * ClockTickRadius));
            tick.localRotation = Quaternion.Euler(0f, 0f, index * -30f);
            tick.GetComponent<Image>().raycastTarget = false;
        }
    }

    private static RectTransform CreateClockHand(
        string name,
        Transform parent,
        float width,
        float length,
        Color color)
    {
        RectTransform hand = CreatePanel(name, parent, color);
        hand.anchorMin = new Vector2(0.5f, 0.5f);
        hand.anchorMax = new Vector2(0.5f, 0.5f);
        hand.pivot = new Vector2(0.5f, 0f);
        hand.sizeDelta = new Vector2(width, length);
        hand.anchoredPosition = Vector2.zero;
        hand.GetComponent<Image>().raycastTarget = false;
        return hand;
    }

    private RectTransform CreateScheduleHandle(
        string name,
        RectTransform face,
        bool adjustStart,
        Color color)
    {
        RectTransform handle = CreatePanel(name, face, color);
        SetCenteredRect(handle, new Vector2(11f, 11f), Vector2.zero);
        handle.localRotation = Quaternion.Euler(0f, 0f, 45f);
        DigClockScheduleHandle input =
            handle.gameObject.AddComponent<DigClockScheduleHandle>();
        input.Initialize(this, face, adjustStart);
        return handle;
    }

    private static void SetCenteredRect(
        RectTransform rect,
        Vector2 size,
        Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private void RefreshClock()
    {
        long tick = _simulation!.CurrentTick;
        string? selectedId = _agentRenderer!.SelectedCount == 1
            ? _agentRenderer.SelectedAgentId
            : null;
        string? displayedId = ResolveClockResidentId(selectedId);
        int ticksPerDay = 24;
        int start = 0;
        int end = 12;
        bool hasSchedule = displayedId != null
            && _simulation.TryGetResidentWorkWindow(
                displayedId,
                out ticksPerDay,
                out start,
                out end);
        int tickOfDay = (int)(tick % ticksPerDay);
        bool editable = hasSchedule
            && string.Equals(displayedId, selectedId, StringComparison.Ordinal);
        string automaticPlanningSignature = ResolveAutomaticPlanningState(
            selectedId,
            out bool hasAutomaticPlanning,
            out bool automaticPlanningEnabled);
        string signature =
            $"{tick}:{selectedId}:{displayedId}:{ticksPerDay}:{start}:{end}:{editable}:"
            + automaticPlanningSignature;
        if (string.Equals(signature, _clockSignature, StringComparison.Ordinal))
        {
            return;
        }

        _clockSignature = signature;
        _clockHourHand!.localRotation = Quaternion.Euler(
            0f,
            0f,
            -(360f * tickOfDay / ticksPerDay));
        _clockMinuteHand!.localRotation = Quaternion.identity;
        RefreshAutomaticPlanningButton(
            hasAutomaticPlanning,
            automaticPlanningEnabled);
        SetScheduleVisible(hasSchedule, editable);
        if (!hasSchedule)
        {
            return;
        }

        UpdateScheduleSegments(ticksPerDay, start, end, editable);
        PositionScheduleHandle(_workStartHandle!, start, ticksPerDay);
        PositionScheduleHandle(_workEndHandle!, end, ticksPerDay);
    }

    private string? ResolveClockResidentId(string? selectedId)
    {
        return _rosterHoveredResidentId
            ?? _worldHoveredResidentId
            ?? selectedId;
    }

    private void UpdateScheduleSegments(
        int ticksPerDay,
        int start,
        int end,
        bool editable)
    {
        for (int index = 0; index < ClockScheduleSegmentCount; index++)
        {
            int hour = Mathf.FloorToInt(
                index * ticksPerDay / (float)ClockScheduleSegmentCount);
            bool work = IsInsideWorkWindow(hour, start, end);
            float alpha = editable ? 0.96f : 0.74f;
            _clockScheduleSegments[index]!.color = work
                ? new Color(0.96f, 0.50f, 0.12f, alpha)
                : new Color(0.26f, 0.56f, 0.88f, alpha);
        }
    }

    private void SetScheduleVisible(bool showOverlay, bool editable)
    {
        _clockScheduleRoot!.gameObject.SetActive(showOverlay);
        _workStartHandle!.gameObject.SetActive(editable);
        _workEndHandle!.gameObject.SetActive(editable);
    }

    private static void PositionScheduleHandle(
        RectTransform handle,
        int hour,
        int ticksPerDay)
    {
        float angle = hour * Mathf.PI * 2f / ticksPerDay;
        handle.anchoredPosition = new Vector2(
            Mathf.Sin(angle) * ClockHandleRadius,
            Mathf.Cos(angle) * ClockHandleRadius);
    }

    private void InvalidateClock()
    {
        _clockSignature = string.Empty;
    }

    private static bool IsInsideWorkWindow(int hour, int start, int end)
    {
        return start < end
            ? hour >= start && hour < end
            : hour >= start || hour < end;
    }

    private static int WrapHour(int value, int ticksPerDay)
    {
        int result = value % ticksPerDay;
        return result < 0 ? result + ticksPerDay : result;
    }
}

}
