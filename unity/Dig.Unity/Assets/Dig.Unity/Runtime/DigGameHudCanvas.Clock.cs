using Dig.Domain.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private Text? _clockDigitalText;
    private Text? _clockWorkText;
    private Text? _clockModeText;
    private RectTransform? _clockHand;
    private Button? _workStartMinus;
    private Button? _workStartPlus;
    private Button? _workEndMinus;
    private Button? _workEndPlus;
    private string _clockSignature = string.Empty;

    private void CreateClockShell()
    {
        _clockPanel = CreatePanel(
            "Game Clock Panel",
            transform,
            new Color(0.025f, 0.04f, 0.06f, 0.96f));
        _clockDigitalText = CreateText(
            "Game Time",
            _clockPanel,
            "DAY 1 · 00:00",
            15,
            TextAnchor.MiddleCenter);
        Anchor(_clockDigitalText.rectTransform, 0f, 1f, 1f, 1f, 5f, -28f, -5f, -5f);

        RectTransform face = CreatePanel(
            "Clock Face",
            _clockPanel,
            new Color(0.08f, 0.10f, 0.14f, 1f));
        face.anchorMin = new Vector2(0f, 0.5f);
        face.anchorMax = new Vector2(0f, 0.5f);
        face.pivot = new Vector2(0f, 0.5f);
        face.sizeDelta = new Vector2(84f, 84f);
        face.anchoredPosition = new Vector2(8f, -5f);
        CreateClockTicks(face);
        _clockHand = CreatePanel(
            "Clock Hand",
            face,
            new Color(0.95f, 0.62f, 0.18f, 1f));
        _clockHand.anchorMin = new Vector2(0.5f, 0.5f);
        _clockHand.anchorMax = new Vector2(0.5f, 0.5f);
        _clockHand.pivot = new Vector2(0.5f, 0f);
        _clockHand.sizeDelta = new Vector2(3f, 31f);
        _clockHand.anchoredPosition = Vector2.zero;
        RectTransform hub = CreatePanel(
            "Clock Hub",
            face,
            new Color(0.75f, 0.80f, 0.88f, 1f));
        hub.anchorMin = new Vector2(0.5f, 0.5f);
        hub.anchorMax = new Vector2(0.5f, 0.5f);
        hub.pivot = new Vector2(0.5f, 0.5f);
        hub.sizeDelta = new Vector2(8f, 8f);
        hub.anchoredPosition = Vector2.zero;

        RectTransform schedule = CreateRect("Schedule Controls", _clockPanel);
        Anchor(schedule, 0f, 0f, 1f, 1f, 98f, 31f, -7f, -31f);
        VerticalLayoutGroup layout = schedule.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        _workStartMinus = CreateScheduleRow(
            schedule,
            "START",
            () => AdjustWorkWindow(adjustStart: true, -1),
            () => AdjustWorkWindow(adjustStart: true, 1),
            out _workStartPlus);
        _workEndMinus = CreateScheduleRow(
            schedule,
            "END",
            () => AdjustWorkWindow(adjustStart: false, -1),
            () => AdjustWorkWindow(adjustStart: false, 1),
            out _workEndPlus);
        _clockWorkText = CreateText(
            "Work Window",
            schedule,
            "SELECT A DWARF",
            12,
            TextAnchor.MiddleCenter);
        _clockWorkText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _clockWorkText.resizeTextForBestFit = true;
        _clockWorkText.resizeTextMinSize = 9;
        _clockWorkText.resizeTextMaxSize = 12;
        _clockWorkText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        _clockModeText = CreateText(
            "Clock Mode",
            _clockPanel,
            "WORLD TIME",
            12,
            TextAnchor.MiddleCenter);
        Anchor(_clockModeText.rectTransform, 0f, 0f, 1f, 0f, 6f, 5f, -6f, 26f);
        SetScheduleButtonsInteractable(false);
    }

    private static void CreateClockTicks(RectTransform face)
    {
        const float radius = 34f;
        for (int index = 0; index < 12; index++)
        {
            float angle = index * Mathf.PI * 2f / 12f;
            RectTransform tick = CreatePanel(
                $"Clock Tick {index}",
                face,
                new Color(0.70f, 0.75f, 0.82f, 0.9f));
            tick.anchorMin = new Vector2(0.5f, 0.5f);
            tick.anchorMax = new Vector2(0.5f, 0.5f);
            tick.pivot = new Vector2(0.5f, 0.5f);
            tick.sizeDelta = new Vector2(2f, index % 3 == 0 ? 8f : 5f);
            tick.anchoredPosition = new Vector2(
                Mathf.Sin(angle) * radius,
                Mathf.Cos(angle) * radius);
            tick.localRotation = Quaternion.Euler(0f, 0f, index * -30f);
            tick.GetComponent<Image>().raycastTarget = false;
        }
    }

    private static Button CreateScheduleRow(
        Transform parent,
        string label,
        UnityEngine.Events.UnityAction minus,
        UnityEngine.Events.UnityAction plus,
        out Button plusButton)
    {
        RectTransform row = CreateRect($"{label} Row", parent);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 23f;
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 3f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = false;
        Button minusButton = CreateButton(
            $"{label} Earlier",
            row,
            "−",
            () => minus(),
            preferredHeight: 22f);
        minusButton.GetComponent<LayoutElement>().preferredWidth = 24f;
        Text text = CreateText(label, row, label, 11, TextAnchor.MiddleCenter);
        LayoutElement textLayout = text.gameObject.AddComponent<LayoutElement>();
        textLayout.flexibleWidth = 1f;
        textLayout.preferredHeight = 22f;
        plusButton = CreateButton(
            $"{label} Later",
            row,
            "+",
            () => plus(),
            preferredHeight: 22f);
        plusButton.GetComponent<LayoutElement>().preferredWidth = 24f;
        return minusButton;
    }

    private void RefreshClock()
    {
        long tick = _simulation!.CurrentTick;
        string? selectedId = _agentRenderer!.SelectedCount == 1
            ? _agentRenderer.SelectedAgentId
            : null;
        int ticksPerDay = 24;
        int start = 0;
        int end = 12;
        bool editable = selectedId != null
            && _simulation.TryGetResidentWorkWindow(
                selectedId,
                out ticksPerDay,
                out start,
                out end);
        int tickOfDay = (int)(tick % ticksPerDay);
        long day = (tick / ticksPerDay) + 1;
        string signature = $"{tick}:{selectedId}:{ticksPerDay}:{start}:{end}:{editable}";
        if (string.Equals(signature, _clockSignature, System.StringComparison.Ordinal))
        {
            return;
        }

        _clockSignature = signature;
        _clockDigitalText!.text = $"DAY {day} · {tickOfDay:00}:00";
        _clockHand!.localRotation = Quaternion.Euler(
            0f,
            0f,
            -(360f * tickOfDay / ticksPerDay));
        SetScheduleButtonsInteractable(editable);
        if (!editable)
        {
            _clockWorkText!.text = "SELECT A DWARF";
            _clockModeText!.text = "WORLD TIME";
            return;
        }

        bool working = IsInsideWorkWindow(tickOfDay, start, end);
        _clockWorkText!.text = $"WORK {start:00}–{end:00}";
        _clockModeText!.text = working ? "WORK TIME" : "REST TIME";
        _clockModeText.color = working
            ? new Color(0.95f, 0.62f, 0.18f, 1f)
            : new Color(0.48f, 0.72f, 1f, 1f);
    }

    private void AdjustWorkWindow(bool adjustStart, int delta)
    {
        string? selectedId = _agentRenderer!.SelectedAgentId;
        if (selectedId == null
            || !_simulation!.TryGetResidentWorkWindow(
                selectedId,
                out int ticksPerDay,
                out int start,
                out int end))
        {
            return;
        }

        int nextStart = adjustStart ? WrapHour(start + delta, ticksPerDay) : start;
        int nextEnd = adjustStart ? end : WrapHour(end + delta, ticksPerDay);
        if (nextStart == nextEnd)
        {
            if (adjustStart)
            {
                nextStart = WrapHour(nextStart + delta, ticksPerDay);
            }
            else
            {
                nextEnd = WrapHour(nextEnd + delta, ticksPerDay);
            }
        }

        Result result = _simulation.SetResidentWorkWindow(
            selectedId,
            nextStart,
            nextEnd);
        SetStatus(result.IsSuccess
            ? $"Work schedule set to {nextStart:00}:00–{nextEnd:00}:00; all other time is rest."
            : result.Error!.Message);
        InvalidateAll();
    }

    private void SetScheduleButtonsInteractable(bool value)
    {
        _workStartMinus!.interactable = value;
        _workStartPlus!.interactable = value;
        _workEndMinus!.interactable = value;
        _workEndPlus!.interactable = value;
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